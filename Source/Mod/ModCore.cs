using HarmonyLib;
using InGameWiki;
using Verse;

namespace InGameWikiMod
{
    public class ModCore : Mod
    {
        public ModCore(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony("co.uk.epicguru.ingamewiki");
            ModWiki.Patch(harmony);
            Log.Message($"<color=cyan>Finished loading in-game wiki mod: Version {ModWiki.Version}</color>");
        }
    }
}
