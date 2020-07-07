using UnityEngine;
using Verse;

namespace InGameWiki
{
    public class WikiElement
    {
        public static WikiElement Create(string text)
        {
            return Create(text, null, null);
        }

        public static WikiElement Create(Texture2D image, Vector2? imageSize = null)
        {
            return Create(null, image, imageSize);
        }

        public static WikiElement Create(string text, Texture2D image, Vector2? imageSize = null)
        {
            return new WikiElement()
            {
                Text = text,
                Image = image,
                ImageSize = imageSize ?? new Vector2(-1, -1)
            };
        }

        public string Text;
        public Texture2D Image;
        public bool AutoFitImage = false;
        public Vector2 ImageSize = new Vector2(-1, -1);
        public float ImageScale = 1f;
        public GameFont FontSize = GameFont.Small;
        public Def DefForIconAndLabel;

        public string PageLink;
        public (ModWiki wiki, WikiPage page) PageLinkReal;
        public bool IsLinkBroken { get; private set; }

        public bool HasText
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Text) || PageLink != null;
            }
        }
        public bool HasImage
        {
            get
            {
                return Image != null;
            }
        }

        public virtual Vector2 Draw(Rect maxBounds)
        {
            Vector2 size = Vector2.zero;

            var old = Verse.Text.Font;
            Verse.Text.Font = FontSize;

            Vector2 imageOffset = Vector2.zero;
            if (HasImage)
            {
                if (!AutoFitImage)
                {
                    float width = ImageSize.x < 1 ? Image.width * ImageScale : ImageSize.x;
                    float height = ImageSize.y < 1 ? Image.height * ImageScale : ImageSize.y;
                    Widgets.DrawTextureFitted(new Rect(maxBounds.x, maxBounds.y, width, height), Image, 1f);
                    size += new Vector2(width, height);
                    imageOffset.x = width;
                }
                else
                {
                    float baseWith = Image.width;

                    if(baseWith <= maxBounds.width)
                    {
                        float width = Image.width;
                        float height = Image.height;
                        Widgets.DrawTextureFitted(new Rect(maxBounds.x, maxBounds.y, width, height), Image, 1f);
                        size += new Vector2(width, height);
                        imageOffset.x = width;
                    }
                    else
                    {
                        float width = maxBounds.width;
                        float height = Image.height * (width / Image.width);
                        Widgets.DrawTextureFitted(new Rect(maxBounds.x, maxBounds.y, width, height), Image, 1f);
                        size += new Vector2(width, height);
                        imageOffset.x = width;
                    }
                }
            }

            if (DefForIconAndLabel != null)
            {
                //TooltipHandler.TipRegion(rect, (TipSignal)def.description);
                var rect = new Rect(maxBounds.x, maxBounds.y, 200, 32);
                bool isSpoiler = false;
                var pair = ModWiki.GlobalFindPageFromDef(DefForIconAndLabel.defName);
                if (pair.page != null)
                {
                    isSpoiler = pair.page.IsSpoiler;
                }
                bool overrideSpoilers = Input.GetKey(KeyCode.LeftControl);

                if (!isSpoiler || overrideSpoilers)
                {
                    Widgets.DefLabelWithIcon(rect, DefForIconAndLabel);
                }
                else
                {
                    Widgets.DrawBoxSolid(rect, new Color(1f, 89f / 255f, 136f / 255f, 0.4f));
                    string spoilerText = "<i>[" + "Wiki.Spoiler".Translate().CapitalizeFirst() + "]</i>";
                    var spoilerTextSize = Verse.Text.CalcSize(spoilerText);
                    Widgets.Label(new Rect(rect.x + (rect.width - spoilerTextSize.x) * 0.5f, rect.y + (rect.height - spoilerTextSize.y) * 0.5f, spoilerTextSize.x, spoilerTextSize.y), spoilerText);
                }
                if (Widgets.ButtonInvisible(rect, true) && (!isSpoiler || overrideSpoilers))
                {
                    if (pair.page != null)
                    {
                        ModWiki.ShowPage(pair.wiki, pair.page);
                    }
                    else
                    {
                        ModWiki.OpenInspectWindow(DefForIconAndLabel);
                    }
                }
                size.y += 32;
                imageOffset.x = 200;
                imageOffset.y = 6;
            }

            if (HasText)
            {
                bool isLink = PageLink != null;

                float x = maxBounds.x + imageOffset.x;
                float width = maxBounds.xMax - x;

                float startY = maxBounds.y + imageOffset.y;
                float cacheStartY = startY;

                if (isLink && PageLinkReal.page == null && !IsLinkBroken)
                {
                    var found = ModWiki.GlobalFindPageFromID(PageLink);
                    if (found.page == null)
                    {
                        IsLinkBroken = true;
                    }
                    else
                    {
                        PageLinkReal = found;
                    }
                }

                string linkText = IsLinkBroken ? $"<color=#ff2b2b><b><i>{"Wiki.LinkBroken".Translate()}: [{PageLink}]</i></b></color>" : $"<color=#9c9c9c><b><i>{"Wiki.Link".Translate()}:</i></b></color>{PageLinkReal.page?.Title}";
                string txt = isLink ? linkText : Text;

                Widgets.LongLabel(x, width, txt, ref startY);
                float change = startY - cacheStartY;

                if (isLink)
                {
                    Rect bounds = new Rect(x, cacheStartY, width, change);
                    Widgets.DrawHighlightIfMouseover(bounds);
                    if (!IsLinkBroken && Widgets.ButtonInvisible(bounds))
                    {
                        // Go to link.
                        ModWiki.ShowPage(PageLinkReal.wiki, PageLinkReal.page);
                    }
                }

                size += new Vector2(width, 0);
                if (size.y < change)
                    size.y = change;
            }

            Verse.Text.Font = old;

            return size;
        }
    }
}
