using InGameWiki;
using Verse;

namespace ExampleWiki
{
    [StaticConstructorOnStartup]
    internal static class WikiLoader
    {
        static WikiLoader()
        {
            // Create the wiki based on this mod.
            var wiki = ModWiki.Create(ExampleWikiMod.Instance);

            // Make sure it isn't null. It could be null if there was an error generating the wiki.
            if (wiki == null)
                return;

            // Set title, icon, etc...
            wiki.WikiTitle = "Example mod wiki";
        }
    }
}
