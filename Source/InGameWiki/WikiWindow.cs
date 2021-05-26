using UnityEngine;
using Verse;

namespace InGameWiki
{
    public class WikiWindow : Window
    {
        public static WikiWindow Open(ModWiki wiki, WikiPage page = null)
        {
            if (wiki == null)
                return null;
            if (CurrentActive != null && CurrentActive.Wiki != wiki)
            {
                //Log.Warn("There is already an open wiki page, closing old.");
                CurrentActive.Close(true);
            }

            var created = new WikiWindow(wiki);
            created.CurrentPage = page;
            CurrentActive = created;
            Find.WindowStack?.Add(created);

            return created;
        }
        public static WikiWindow CurrentActive { get; private set; }

        public ModWiki Wiki;
        public int TopHeight = 38;
        public int SearchHeight = 34;
        public int SideWidth = 330;
        public WikiPage CurrentPage { get; set; }
        public override Vector2 InitialSize => new Vector2(1100, 800);
        public string SearchText = "";

        private Vector2 scroll;
        private float lastHeight;

        protected WikiWindow(ModWiki wiki)
        {
            this.Wiki = wiki;
            resizeable = true;
            doCloseButton = true;
            draggable = true;
            drawShadow = true;
            onlyOneOfTypeAllowed = true;
            absorbInputAroundWindow = false;
            preventCameraMotion = false;
        }

        public override void DoWindowContents(Rect maxBounds)
        {
            var global = new Rect(maxBounds.x, maxBounds.y, maxBounds.width, maxBounds.height - 50);
            Rect titleArea = new Rect(global.x, global.y, global.width, TopHeight);
            Rect searchArea = new Rect(global.x, global.y + TopHeight + 5, SideWidth, SearchHeight);
            Rect pagesArea = new Rect(global.x, global.y + TopHeight + 10 + SearchHeight, SideWidth, global.height - 10 - TopHeight - SearchHeight);
            Rect contentArea = new Rect(global.x + SideWidth + 5, global.y + TopHeight + 5, global.width - SideWidth - 5, global.height - TopHeight - 5);

            Widgets.DrawBoxSolid(pagesArea, Color.white * 0.4f);
            Widgets.DrawBox(pagesArea);
            Widgets.DrawBox(titleArea);
            Widgets.DrawBox(contentArea);

            // Title
            Text.Font = GameFont.Medium;
            var titleSize = Text.CalcSize(Wiki.WikiTitle);
            Widgets.Label(new Rect(titleArea.x + (titleArea.width - titleSize.x) * 0.5f, titleArea.y + (titleArea.height - titleSize.y) * 0.5f, titleSize.x, titleSize.y), Wiki.WikiTitle);

            // Search box.
            SearchText = Widgets.TextField(searchArea, SearchText);

            // Draw all pages list.
            Widgets.BeginScrollView(pagesArea, ref scroll, new Rect(pagesArea.x, pagesArea.y, pagesArea.width - 32, lastHeight));
            lastHeight = 0;

            // Normalize search string.
            string searchString = SearchText?.Trim().ToLowerInvariant();
            bool isSearching = !string.IsNullOrEmpty(searchString);

            int hiddenCount = 0;
            foreach (var page in Wiki.Pages)
            {
                if (page == null)
                    continue;

                if (isSearching)
                {
                    string pageName = page.Title.Trim().ToLowerInvariant();
                    if (!pageName.Contains(searchString))
                        continue;
                }

                if (page.IsSpoiler)
                {
                    hiddenCount++;
                    continue;
                }

                if (page.Icon != null)
                {
                    GUI.color = page.IconColor;
                    Widgets.DrawTextureFitted(new Rect(pagesArea.x + 4, pagesArea.y + 4 + lastHeight + 5, 24, 24), page.Icon, 1f);
                    GUI.color = Color.white;
                }
                bool clicked = Widgets.ButtonText(new Rect(pagesArea.x + 28, pagesArea.y + 4 + lastHeight, pagesArea.width - 32, 40), page.Title);
                if (clicked)
                {
                    CurrentPage = page;
                }

                lastHeight += 32 + 5;
            }

            if (hiddenCount > 0)
            {
                string text = $"<color=#FF6D71><i>{"Wiki.HiddenWarning".Translate(hiddenCount)}</i></color>";
                Widgets.Label(new Rect(pagesArea.x + 4, pagesArea.y + 4 + lastHeight, pagesArea.width - 4, 50), text);
                lastHeight += 42 + 5;
            }

            Widgets.EndScrollView();

            // Current page.
            CurrentPage?.Draw(contentArea);

            // Spoiler mode toggle.
            bool spoilerMode = ModWiki.NoSpoilerMode;
            string txt = "Wiki.HideSpoilerMode".Translate();
            float width = Text.CalcSize(txt).x + 24;
            Widgets.CheckboxLabeled(new Rect(maxBounds.x + 5, maxBounds.yMax - 32, width, 32), txt, ref spoilerMode);
            ModWiki.NoSpoilerMode = spoilerMode;
        }

        public override void PreClose()
        {
            CurrentActive = null;
            base.PreClose();
        }

        public bool GoToPage(Def def, bool openInspectWindow = false)
        {
            if (def == null)
                return false;

            var page = Wiki.FindPageFromDef(def.defName);
            if (page == null)
            {
                if (openInspectWindow)
                {
                    ModWiki.OpenInspectWindow(def);
                    return true;
                }
                return false;
            }
            else
            {
                this.CurrentPage = page;
                return true;
            }
        }
    }
}
