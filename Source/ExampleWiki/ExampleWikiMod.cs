using Verse;

namespace ExampleWiki
{
    public class ExampleWikiMod : Mod
    {
        public static ExampleWikiMod Instance;

        public ExampleWikiMod(ModContentPack content) : base(content)
        {
            Log.Message("Loaded example wiki mod.");

            Instance = this;
        }
    }
}
