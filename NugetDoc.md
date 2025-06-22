# How to install
1. Open your mod project in visual studio.
2. Right click on References>Manage NuGet Packages
3. Inside the package manager window, click Browse and search for 'Lib.InGameWiki'.
4. Select the package (author: Epicguru) and select Install.
5. Package should now be installed. Under references, click on InGameWiki and find the properties tab.

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
            
            // Check if wiki creation was successful. If not, exit from the method.
            if(wiki == null)
                return;
            
            // Change some wiki properties.
            wiki.WikiTitle = "MyMod: My wiki";
        }
    }
}
```
3. Edit the code to work with your mod; change namespace, get a reference to your mod class, change wiki title etc.

__Note:__ If you are confused as how to get the _reference to your mod class_, see [this example file](https://github.com/Epicguru/InGameWiki/blob/master/ExampleModClass.cs) for an example mod class. If you are familiar with C# modding, you probably already have one.

# How to add custom pages
In your mod folder, next to the Textures, Assemblies, About folders, create a new folder called `Wiki`.
Inside this folder, you can add custom pages.

[Click here to see how to create custom pages.](https://github.com/Epicguru/InGameWiki/blob/master/ExternalPages.md)

You can also add custom content to auto-generated item pages. For example, add images and text to the page that was generated for a gun added by your mod.

[Click here to see how to add content to existing auto-generated pages.](https://github.com/Epicguru/InGameWiki/blob/master/EditingAutogenPages.md)

# How to exclude certain defs
The wiki mod takes all of the defs added by your mod and discards certain ones, such as _Motes_, _Projectiles_, _Blueprints_ etc.
However, sometimes you want to tell the mod to ignore a specific def. This could be either to hide content from the player, or just because it gives no useful information.

Suppose that you want to remove the generated page for a def called `MyBoringDef`. The steps are as follows:
1. Go into your `Wiki` folder. See [here](https://github.com/Epicguru/InGameWiki/blob/master/ExternalPages.md) for more info on folder structure.
2. Create a new file called `Exclude.txt`. Spelling is imporant!
3. Open the txt file.
4. Type the name(s) of the def that you want to exclude, such as `MyBoringDef`. One def name per line.
5. Save the file.

Inside the `Excluded.txt` file, blank lines are ignored. You can also type comments by starting the line with _//_ such as `// I am a comment`.

# Example mod
This wiki mod was initially created for my mod _Antimatter Annihilation_:

[Link to Antimatter Annihilation repository](https://github.com/Epicguru/AntimatterAnnihilation)

There you will find all the wiki files, folder structures and every source file required to make a fully funcional wiki.

WIP documentation.
