using Verse;

namespace InGameWikiMod
{
    public class ModCore : Mod
    {
        public ModCore(ModContentPack content) : base(content)
        {
            Log.Message("<color=cyan>Loaded in-game wiki mod.</color>");
            Log.Message($"<color=cyan>{typeof(MainButtonUI).FullName}</color>");
        }
    }
}
