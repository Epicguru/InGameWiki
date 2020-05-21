using InGameWiki;
using Verse;

namespace ExampleWiki
{
    [StaticConstructorOnStartup]
    internal static class WikiLoader
    {
        static WikiLoader()
        {
            var wiki = ModWiki.Create(ExampleWikiMod.Instance);
            wiki.WikiTitle = "Example mod wiki";
        }
    }
}
