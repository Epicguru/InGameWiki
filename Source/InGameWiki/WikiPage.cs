using InGameWiki.Internal;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace InGameWiki
{
    public class WikiPage
    {
        public static bool DebugMode = false;

        public static WikiPage CreateFromThingDef(ThingDef thing)
        {
            if (thing == null)
                return null;

            WikiPage p = new WikiPage();

            try
            {
                p.Title = thing.LabelCap;
                p.ShortDescription = thing.DescriptionDetailed;
                p.Icon = thing.uiIcon;
            }
            catch (Exception e)
            {
                throw new Exception("Exception setting page basics.", e);
            }

            try
            {
                // Cost.
                if (thing.costList != null)
                {
                    var cost = new SectionWikiElement();
                    cost.Name = "Wiki.Cost".Translate();

                    foreach (var costThing in thing.costList)
                    {
                        cost.Elements.Add(new WikiElement() { DefForIconAndLabel = costThing.thingDef, Text = costThing.count <= 1 ? "" : $"x{costThing.count}" });
                    }

                    int outputCount = thing.recipeMaker?.productCount ?? 1;

                    cost.Elements.Add(WikiElement.Create("Wiki.OutputCount".Translate(outputCount)));

                    if (cost.Elements.Count > 0)
                    {
                        p.Elements.Add(cost);
                    }

                    var creates = new SectionWikiElement();
                    creates.Name = "Wiki.Creates".Translate();

                    // Show recipes added by this production thing.
                    foreach (var rec in thing.AllRecipes)
                    {
                        creates.Elements.Add(WikiElement.Create($" • {rec.LabelCap}"));
                    }

                    if (creates.Elements.Count > 0)
                        p.Elements.Add(creates);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Exception generating thing cost list.", e);
            }

            try
            {
                // Crafting (where is it crafted)
                if (thing.recipeMaker?.recipeUsers != null)
                {
                    var crafting = new SectionWikiElement();
                    crafting.Name = "Wiki.CraftedAt".Translate();

                    foreach (var user in thing.recipeMaker.recipeUsers)
                    {
                        crafting.Elements.Add(new WikiElement() { DefForIconAndLabel = user });
                    }

                    if (crafting.Elements.Count > 0)
                    {
                        p.Elements.Add(crafting);
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Exception generating thing crafting location list.", e);
            }

            try
            {
                // Research prerequisite.
                var research = new SectionWikiElement();
                research.Name = "Wiki.ResearchToUnlock".Translate();
                if (thing.researchPrerequisites != null && thing.researchPrerequisites.Count > 0) // Generally buildings.
                {
                    foreach (var r in thing.researchPrerequisites)
                    {
                        research.Elements.Add(new WikiElement() { Text = $" • {r.LabelCap}" });
                    }

                }
                if (thing.recipeMaker?.researchPrerequisites != null) // Generally craftable items.
                {
                    foreach (var r in thing.recipeMaker.researchPrerequisites)
                    {
                        research.Elements.Add(new WikiElement() { Text = $" • {r.LabelCap}" });
                    }
                }
                if (thing.recipeMaker?.researchPrerequisite != null) // Generally craftable items.
                {
                    var r = thing.recipeMaker.researchPrerequisite;
                    research.Elements.Add(new WikiElement() { Text = $" • {r.LabelCap}" });
                }

                if (research.Elements.Count > 0)
                    p.Elements.Add(research);

                if (DebugMode)
                {
                    if (thing.weaponTags != null)
                    {
                        foreach (var tag in thing.weaponTags)
                        {
                            p.Elements.Add(new WikiElement() { Text = $"WeaponTag: {tag}" });
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw new Exception("Exception generating thing research requirements.", e);
            }

            p.Def = thing;

            return p;
        }

        public virtual bool IsSpoiler
        {
            get
            {
                if (!ModWiki.NoSpoilerMode)
                    return false;

                if (IsAlwaysSpoiler)
                    return true;

                // Check custom research tag.
                if (RequiresResearchRaw != null)
                {
                    if (requiresResearch == null)
                    {
                        requiresResearch = ResearchProjectDef.Named(RequiresResearchRaw);
                        if (requiresResearch == null)
                        {
                            Log.Error($"Failed to find required research for page {ID} ({Title}): '{RequiresResearchRaw}'");
                            RequiresResearchRaw = null;
                        }
                    }
                    if (requiresResearch != null)
                    {
                        if (!requiresResearch.IsFinished)
                            return true;
                    }
                }

                // If the research hasn't been done, it's a spoiler.
                if (!IsResearchFinished)
                    return true;

                return false;
            }
        }

        public virtual bool IsResearchFinished
        {
            get
            {
                if (Def == null)
                    return true;

                if (!(Def is ThingDef td))
                    return true;

                // This only works for buildings. For craftable items, I also need to check recipe makers for research.
                if (td.researchPrerequisites != null)
                {
                    return td.IsResearchFinished;
                }

                // This works for craftable items.
                if (td.recipeMaker != null)
                {
                    bool singleDone = true;
                    if (td.recipeMaker.researchPrerequisite != null)
                    {
                        singleDone = td.recipeMaker.researchPrerequisite.IsFinished;
                    }
                    if (!singleDone)
                        return false;

                    bool allDone = true;
                    if (td.recipeMaker.researchPrerequisites != null)
                    {
                        foreach (var rec in td.recipeMaker.researchPrerequisites)
                        {
                            if (!rec.IsFinished)
                            {
                                allDone = false;
                                break;
                            }
                        }
                    }
                    return allDone;
                }

                // Looks like this Thing has no research requirements.
                return true;
            }
        }

        public virtual Color IconColor => Def is ThingDef td ? td.graphicData?.color ?? Color.white : Color.white;

        /// <summary>
        /// Only valid when the page is external (not generated from a ThingDef)
        /// </summary>
        public string RequiresResearchRaw;
        public string ID { get; internal set; }
        public string Title;
        public string ShortDescription;
        public Texture2D Icon;
        public Texture2D Background;
        public Def Def;
        public bool IsAlwaysSpoiler;

        public List<WikiElement> Elements = new List<WikiElement>();

        private ResearchProjectDef requiresResearch;
        private float lastHeight;
        private Vector2 scroll;
        private Vector2 descScroll;

        public virtual void Draw(Rect maxBounds)
        {
            const int PADDING = 5;

            float topHeight = 128 + PADDING;

            // Background
            if (Background != null)
            {
                GUI.color = Color.white * 0.45f;
                var coords = CalculateUVCoords(maxBounds, new Rect(0, 0, Background.width, Background.height));
                GUI.DrawTextureWithTexCoords(maxBounds, Background, coords, true);
                GUI.color = Color.white;
            }

            // Icon.
            bool drawnIcon = Icon != null;
            if (drawnIcon)
            {
                // Draws icon and opens zoom view window if clicked.
                if (Widgets.ButtonImageFitted(new Rect(maxBounds.x + PADDING, maxBounds.y + PADDING, 128, 128), Icon, IconColor, IconColor * 0.8f))
                {
                    Find.WindowStack?.Add(new UI_ImageInspector(Icon));
                }
                GUI.color = Color.white;
            }

            // Title.
            if (Title != null)
            {
                Text.Font = GameFont.Medium;
                float x = !drawnIcon ? maxBounds.x + PADDING : maxBounds.x + PADDING + 128 + PADDING;
                float w = !drawnIcon ? maxBounds.width - PADDING * 2 : maxBounds.width - PADDING * 2 - 128;
                string toDraw = Title;
                if (IsSpoiler)
                {
                    toDraw += $" <color=#FF6D71><i>[{"Wiki.Spoiler".Translate().CapitalizeFirst()}]</i></color>";
                }
                Widgets.Label(new Rect(x, maxBounds.y + PADDING, w, 34), toDraw);
            }

            // Short description.
            if(ShortDescription != null)
            {
                Text.Font = GameFont.Small;
                float x = !drawnIcon ? maxBounds.x + PADDING : maxBounds.x + PADDING + 128 + PADDING;
                float y = Title == null ? maxBounds.y + PADDING : maxBounds.y + PADDING * 2 + 34;
                float w = !drawnIcon ? maxBounds.width - PADDING * 2 : maxBounds.width - PADDING * 3 - 128;
                float h = (maxBounds.y + PADDING + 128) - y;
                Widgets.LabelScrollable(new Rect(x, y, w, h), ShortDescription, ref descScroll, false, true, true);
            }

            // Info card button.
            if (Def != null)
            {
                float infoCardX = maxBounds.xMax - Widgets.InfoCardButtonSize - 5;
                float infoCardY = maxBounds.y + PADDING;
                Widgets.InfoCardButton(infoCardX, infoCardY, Def);
            }

            // Move down to 'real wiki' part.
            Text.Font = GameFont.Small;
            maxBounds.y += topHeight;
            maxBounds.height -= topHeight;

            // Draw horizontal line separating wiki from description/icon.
            Widgets.DrawLineHorizontal(maxBounds.x, maxBounds.y, maxBounds.width);

            // Scroll view stuff.
            var whereToDraw = maxBounds;
            whereToDraw = whereToDraw.GetInner(PADDING);
            Widgets.BeginScrollView(whereToDraw, ref scroll, new Rect(maxBounds.x + PADDING, maxBounds.y + PADDING, maxBounds.width - 25 - PADDING * 2, lastHeight));
            lastHeight = 0;

            // Draw elements.
            Rect pos = whereToDraw;
            pos.width -= 18;
            foreach (var element in Elements)
            {
                if (element == null)
                    continue;

                try
                {
                    var size = element.Draw(pos);
                    pos.y += size.y + 10;
                    lastHeight += size.y + 10;
                }
                catch (Exception e)
                {
                    Log.Error($"In-game wiki exception when drawing element: {e}");
                }
            }

            Widgets.EndScrollView();
        }

        private Rect CalculateUVCoords(Rect boundsToFill, Rect imageSize)
        {
            var nr = new Rect();
            nr.size = imageSize.size;
            nr.center = boundsToFill.center;

            Vector2 topLeftOffset = boundsToFill.min - nr.min;
            Vector2 bottomRightOffset = boundsToFill.max - nr.min;
            Vector2 topLeftUV = new Vector2(topLeftOffset.x / imageSize.width, topLeftOffset.y / imageSize.height);
            Vector2 bottomRightUV = new Vector2(bottomRightOffset.x / imageSize.width, bottomRightOffset.y / imageSize.height);
            Rect uv = new Rect(topLeftUV, bottomRightUV - topLeftUV);
            return uv;
        }
    }
}
