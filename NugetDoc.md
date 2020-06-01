# How to install
1. Open your mod project in visual studio.
2. Right click on References>Manage NuGet Packages
3. Inside the package manager window, click Browse and search for 'Lib.InGameWiki'.
4. Select the package (author: Epicguru) and select Install.
5. Package should now be installed. Under references, click on InGameWiki and find the properties tab.
6. Change Copy Local from True to False.

# How to add basic auto-generated wiki
1. Create a new C# file in your project called `Wiki.cs`
2. Put this code in the file:
```
using InGameWiki;
using Verse;

namespace YourModNamespace
{
    [StaticConstructorOnStartup]
    internal static class Wiki
    {
        static Wiki()
        {
            // Get a reference to your mod instance.
            Mod myMod = MyModClass.Instance;
            
            // Create and register a new wiki.
            var wiki = ModWiki.Create(myMod);
            
            // Change some wiki properties.
            wiki.WikiTitle = "MyMod: My wiki";
        }
    }
}
```
3. Edit the code to work with your mod; change namespace, get a reference to your mod class, change wiki title etc.

# How to add custom pages
In your mod folder, next to the Textures, Assemblies, About folders, create a new folder called `Wiki`.
Inside this folder, you can add custom pages.

[Click here to see how to create custom pages.](https://github.com/Epicguru/InGameWiki/blob/master/ExternalPages.md)

You can also add custom content to auto-generated item pages. For example, add images and text to the page that was generated for a gun added by your mod.

[Click here to see how to add content to existing auto-generated pages.](https://github.com/Epicguru/InGameWiki/blob/master/EditingAutogenPages.md)

# Example mod
This wiki mod was initially created for my mod _Antimatter Annihilation_:
[Link to Antimatter Annihilation repository](https://github.com/Epicguru/AntimatterAnnihilation)
There you will find all the wiki files, folder structures and every source file required to make a fully funcional wiki.

WIP documentation.
