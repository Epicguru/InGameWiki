using InGameWiki;
using RimWorld;
using System;
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
                Log.Warning("There are no wikis loaded.");
                return;
            }

            if (wikis.Count == 1)
            {
                // There is only 1 wiki, just open that one.
                wikis[0].Show();
            }
            else
            {
                // Yeah this is some pretty slick C# right here.
                Func<ModWiki, string> labelGetter = (w) => w.Mod.Content.Name;
                Func<ModWiki, Action> actionGetter = (w) => w.Show;

                FloatMenuUtility.MakeMenu(wikis, labelGetter, actionGetter);
            }
        }
    }
}
