using InGameWiki;
using Verse;

namespace InGameWikiMod
{
    public class ModCore : Mod
    {
        public ModCore(ModContentPack content) : base(content)
        {
            Log.Message($"<color=cyan>Loaded in-game wiki mod: Version {ModWiki.Version}</color>");
        }
    }
}
