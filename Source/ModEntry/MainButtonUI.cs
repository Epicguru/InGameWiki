using InGameWiki;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace InGameWikiMod
{
    [StaticConstructorOnStartup]
    public class MainButtonUI : MainTabWindow
    {
        public override void DoWindowContents(Rect inRect)
        {
            //base.DoWindowContents(inRect);
        }

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
                string LabelGetter(ModWiki w) => w.Mod.Content.Name;
                Action ActionGetter(ModWiki w) => w.Show;

                FloatMenuUtility.MakeMenu(wikis, LabelGetter, ActionGetter);
            }
        }
    }
}
