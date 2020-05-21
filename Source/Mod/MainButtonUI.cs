using InGameWiki;
using RimWorld;
using Verse;

namespace InGameWikiMod
{
    [StaticConstructorOnStartup]
    public class MainButtonUI : MainTabWindow
    {
        public override void PreOpen()
        {
            base.PreOpen();
            Close(false);

            WikiButtonClicked();
        }

        public override void PostOpen()
        {
            base.PostOpen();
            Close(false);
        }

        public void WikiButtonClicked()
        {
            // Choose which wiki to open.
            var wikis = ModWiki.AllWikis;
            if (wikis.Count == 0)
            {
                Log.Warning($"There are no wikis loaded.");
                return;
            }

            if (wikis.Count == 1)
            {
                // There is only 1 wiki, just open that one.
            }
        }
    }
}
