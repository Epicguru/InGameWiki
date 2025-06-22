/*
 * This is an example C# Mod class that can be used to create a Wiki with.
 * If you are familiar with C# modding, you probably already have a mod class such as this.
 * Otherwise, you can copy this code and modify the names to suit your mod.
 */

using InGameWiki;
using Verse;

namespace YourNamespace
{
    public class YourModName : Mod
    {
        // A public, static reference to our mod class instance. It is used when creating the wiki.
        public static YourModName Instance { get; private set; }

        public YourModName(ModContentPack content) : base(content)
        {
            // Set this instance reference. This will be used later when creating the wiki.
            Instance = this;

            // This message will show up in the Rimworld debug log.
            Log.Message("Hello world! I am a mod!");
        }
    }
}
