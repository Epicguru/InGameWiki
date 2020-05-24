using UnityEngine;
using Verse;

namespace InGameWiki.Internal
{
    internal static class InspectorPatch
    {
        internal static void InGameWikiPostfix(Rect inRect, Dialog_InfoCard __instance, Def ___def, Thing ___thing)
        {
            if (___def == null && ___thing == null)
                return;

            var def = ___def ?? ___thing.def;

            if (def == null)
                return;

            (ModWiki wiki, WikiPage page) = ModWiki.GlobalFindPageFromDef(def.defName);

            if (page == null)
                return;

            if (Widgets.ButtonText(new Rect(inRect.x + inRect.width * 0.5f + 6, inRect.y + 24, 180, 36), "Wiki.OpenWikiPage".Translate()))
            {
                __instance.Close(true);
                ModWiki.ShowPage(wiki, page);
            }
        }
    }
}
