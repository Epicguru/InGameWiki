using Verse;

namespace Mod
{
    public class ModCore : Verse.Mod
    {
        public ModCore(ModContentPack content) : base(content)
        {
            Log.Message("<color=cyan>Loaded in-game wiki mod.</color>");
        }
    }
}
