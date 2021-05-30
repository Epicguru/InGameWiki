using HarmonyLib;
using InGameWiki.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Verse;

[assembly: InternalsVisibleTo("InGameWikiMod")]

namespace InGameWiki
{
    public class ModWiki
    {
        /// <summary>
        /// Checks for 
        /// </summary>
        public static bool IsWikiModInstalled
        {
            get
            {
                if (wikiModInstallStatus == 0)
                {
                    // Start by assuming not installed.
                    wikiModInstallStatus = 2;
                    foreach (var mod in LoadedModManager.ModHandles)
                    {
                        var c = mod.Content;
                        //Log.Message($"Name: {c.Name}, PackageID: {c.PackageId}, PackageIDPF: {c.PackageIdPlayerFacing}");
                        if (c.PackageId == "co.uk.epicguru.ingamewiki")
                        {
                            wikiModInstallStatus = 1;
                            Log.Message("<color=cyan>Wiki mod is installed correctly, full API enabled.</color>");
                            break;
                        }
                    }
                }

                return wikiModInstallStatus == 1;
            }
        }
        public static IReadOnlyList<ModWiki> AllWikis
        {
            get
            {
                return allWikis;
            }
        }

        private static List<ModWiki> allWikis = new List<ModWiki>();
        private static int wikiModInstallStatus; // 0: not checked, 1: installed, 2: not installed

        internal static void Patch(Harmony harmonyInstance, bool doInspectorButton)
        {
            if (harmonyInstance == null)
                return;

            var method = typeof(Dialog_InfoCard).GetMethod("DoWindowContents", BindingFlags.Public | BindingFlags.Instance);
            if (method == null)
            {
                Log.Error($"Failed to get method Dialog_InfoCard.DoWindowContents to patch. Did Rimworld update in a major way?");
                return;
            }

            var postfix = typeof(InspectorPatch).GetMethod("InGameWikiPostfix", BindingFlags.NonPublic | BindingFlags.Static);
            if (postfix == null)
            {
                Log.Error("Failed to get local patch method...");
                return;
            }
            var patch = new HarmonyMethod(postfix);

            bool canPatch = true;
            var patches = Harmony.GetPatchInfo(method);
            if(patches != null)
            {
                foreach (var pf in patches.Postfixes)
                {
                    if (pf.PatchMethod.Name == "InGameWikiPostfix")
                    {
                        canPatch = false;
                        break;
                    }
                    else
                    {
                        Log.Warning($"There is already a postfix on Dialog_InfoCard.DoWindowContents: {pf.PatchMethod.Name} by {pf.owner}. This could affect functionality of wiki patch.");
                    }
                }
            }

            if (canPatch)
            {
                if(doInspectorButton)
                    harmonyInstance.Patch(method, postfix: patch);

                Log.Message($"<color=cyan>Patched game for in-game wiki. Inspector button is {(doInspectorButton ? "enabled" : "<color=red>disabled</color>")}</color>");
            }
        }

        /// <summary>
        /// Opens an inspector window for the Def provided.
        /// If Def is null, nothing happens.
        /// </summary>
        /// <param name="def">The Def to inspect.</param>
        public static void OpenInspectWindow(Def def)
        {
            if (def != null)
                Find.WindowStack.Add(new Dialog_InfoCard(def));
        }

        /// <summary>
        /// Creates and registers a new mod wiki.
        /// You must pass in your mod instance. Do not call this method more than once per mod!
        /// Uses the default ModWiki class. See <see cref="Create(Mod, ModWiki)"/> to use a custom ModWiki class.
        /// </summary>
        /// <param name="mod">Your mod instance.</param>
        /// <returns>The newly created ModWiki object, or null if creation failed.</returns>
        public static ModWiki Create(Mod mod)
        {
            return Create(mod, new ModWiki());
        }

        /// <summary>
        /// Creates and registers a new mod wiki.
        /// You must pass in your mod instance. Do not call this method more than once per mod!
        /// You must also supply an instance of a ModWiki subclass. This version should only be used if you need to add specialized custom behaviour to the
        /// ModWiki. Otherwise, use <see cref="Create(Mod)"/>.
        /// </summary>
        /// <param name="mod">Your mod instance.</param>
        /// <param name="wiki">Your non-null ModWiki subclass instance.</param>
        /// <returns>The newly created ModWiki object, or null if creation failed.</returns>
        public static ModWiki Create(Mod mod, ModWiki wiki)
        {
            if (mod == null)
            {
                Log.Error("Cannot pass in null mod to create wiki.");
                return null;
            }

            if (wiki == null)
            {
                Log.Error("Cannot pass in null ModWiki instance to create wiki.");
                return null;
            }

            try
            {
                wiki.Mod = mod;

                // If the wiki mod is not installed, then don't generate wiki contents, they aren't needed.
                // This could cause issues if mod's try to the access those pages later, but normally wiki
                // creation will be fire-and-forget.
                if (!IsWikiModInstalled)
                {
                    Log.Warning($"A wiki was registered for mod '{mod.Content.Name}', but the InGameWiki mod is not installed. Dummy wiki has been created instead.");
                    return wiki;
                }

                wiki.GenerateFromMod(mod);

                allWikis.Add(wiki);

                Log.Message($"<color=cyan>A new wiki was registered for mod '{mod.Content.Name}'.</color>");

                return wiki;
            }
            catch (Exception e)
            {
                Log.Error($"Exception creating wiki for {mod.Content.Name}: {e}");
                return null;
            }
        }

        /// <summary>
        /// Tries to find a wiki page given a def name. This searches all ModWikis.
        /// Returns the wiki and the page within that wiki.
        /// See also <see cref="FindPageFromDef(string)"/>.
        /// </summary>
        /// <param name="defName">The ThingDef name to search for.</param>
        /// <returns>A tupple containing the wiki and the wiki page, or <c>(null, null)</c> if the page was not found.</returns>
        public static (ModWiki wiki, WikiPage page) GlobalFindPageFromDef(string defName)
        {
            if (defName == null)
                return (null, null);

            foreach (var wiki in AllWikis)
            {
                var found = wiki.FindPageFromDef(defName);
                if (found != null)
                    return (wiki, found);
            }

            return (null, null);
        }

        /// <summary>
        /// Tries to find a wiki page given a page ID. This searches all ModWikis.
        /// Returns the wiki and the page within that wiki.
        /// See also <see cref="FindPageFromID(string)"/>.
        /// </summary>
        /// <param name="pageID">The page ID, as specified in file, to search for.</param>
        /// <returns>A tupple containing the wiki and the wiki page, or <c>(null, null)</c> if the page was not found.</returns>
        public static (ModWiki wiki, WikiPage page) GlobalFindPageFromID(string pageID)
        {
            if (pageID == null)
                return (null, null);

            foreach (var wiki in AllWikis)
            {
                var found = wiki.FindPageFromID(pageID);
                if (found != null)
                    return (wiki, found);
            }

            return (null, null);
        }

        /// <summary>
        /// Opens the wiki window to a specific page from a specific wiki.
        /// </summary>
        public static void ShowPage(ModWiki wiki, WikiPage page)
        {
            if (wiki == null || page == null)
                return;

            if (WikiWindow.CurrentActive != null && WikiWindow.CurrentActive.Wiki == wiki)
            {
                WikiWindow.CurrentActive.CurrentPage = page;
            }
            else
            {
                WikiWindow.Open(wiki, page);
            }
        }

        public string WikiTitle = "Your Mod Name Here";
        public List<WikiPage> Pages = new List<WikiPage>();
        public Mod Mod { get; private set; }
        public bool NoSpoilerMode { get; set; } = true;

        private ModWiki()
        {

        }

        /// <summary>
        /// Opens this wiki to whatever page was last opened.
        /// </summary>
        public void Show()
        {
            WikiWindow.Open(this);
        }

        private void GenerateFromMod(Mod mod)
        {
            var toExclude = GetExcludedDefs(mod);

            foreach (var def in mod.Content.AllDefs)
            {
                if (!(def is ThingDef thingDef))
                    continue;

                if (toExclude.Contains(def.defName))
                {
                    toExclude.Remove(def.defName);
                    continue;
                }

                bool shouldAdd = AutogenPageFilter(thingDef);
                if (!shouldAdd)
                    continue;

                WikiPage page = null;
                try
                {
                    page = WikiPage.CreateFromThingDef(this, thingDef);
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to generate wiki page for {mod.Content?.Name ?? "<no-name-mod>"}'s ThingDef '{thingDef.LabelCap}': {e}");
                }
                this.Pages.Add(page);
            }

            string dir = Path.Combine(mod.Content.RootDir, "Wiki");
            PageParser.AddAllFromDirectory(this, dir);

            if(toExclude.Count != 0)
            {
                Log.Error($"{mod.Content?.Name ?? "<no-name-mod>"}'s Exclude.txt file includes names of defs that do not exist:");
                foreach (var name in toExclude)
                {
                    Log.Error($"  -{name}");
                }
            }
        }

        private List<string> GetExcludedDefs(Mod mod)
        {
            string file = Path.Combine(mod.Content.RootDir, "Wiki", "Exclude.txt");
            if (!File.Exists(file))
            {
                return new List<string>();
            }

            List<string> found = new List<string>();

            var lines = File.ReadAllLines(file);
            foreach (var l in lines)
            {
                string line = l.Trim();

                // Check that the names correspond to defs.
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("//"))
                    continue;

                found.Add(line);
            }

            return found;
        }

        /// <summary>
        /// A method that should be overriden to decide weather pages will be generated
        /// for the ThingDefs added by a mod. The default implementation filters out blueprints, projectiles, turret guns, motes, ethereal, etc.
        /// Return true to generate page from the ThingDef, false to ignore the ThingDef.
        /// </summary>
        /// <param name="def">The ThingDef that a page might be created for.</param>
        /// <returns>True to generate a page, false to not generate page.</returns>
        public virtual bool AutogenPageFilter(ThingDef def)
        {
            if (def == null)
                return false;

            if (def.IsBlueprint)
                return false;

            if (def.projectile != null)
                return false;

            if (def.entityDefToBuild != null)
                return false;

            if (def.weaponTags?.Contains("TurretGun") ?? false)
                return false;

            if (def.mote != null)
                return false;

            var cat = def.category;
            if (cat == ThingCategory.Ethereal || cat == ThingCategory.Filth)
                return false;

            return true;
        }

        /// <summary>
        /// Finds a page from this wiki given the def name of the Thing that the page is about.
        /// For example, if your mod adds Gun_MySuperGun, then you could access it's wiki page by calling <c>FindPageFromDef("Gun_MySuperGun")</c>.
        /// </summary>
        /// <param name="defName">The name of the definition.</param>
        /// <returns>The wiki page if found, or null.</returns>
        public WikiPage FindPageFromDef(string defName)
        {
            if (defName == null)
                return null;

            foreach (var page in Pages)
            {
                if (page != null && page.Def?.defName == defName)
                    return page;
            }
            return null;
        }

        /// <summary>
        /// Finds a page from this wiki given the def of the Thing that the page is about.
        /// For example, if your mod adds Gun_MySuperGun, then you could access it's wiki page by calling <c>FindPageFromDef(YourDefOf.Gun_MySuperGun)</c>.
        /// </summary>
        /// <param name="defName">The definition.</param>
        /// <returns>The wiki page if found, or null.</returns>
        public WikiPage FindPageFromDef(Def def)
        {
            if (def == null)
                return null;

            foreach (var page in Pages)
            {
                if (page != null && page.Def == def)
                    return page;
            }
            return null;
        }

        /// <summary>
        /// Finds a page from this wiki given the page ID. This will only return 'custom' pages, not auto-generated pages for ThingDefs.
        /// See <see cref="FindPageFromDef(string)"/>.
        /// </summary>
        /// <param name="pageID">The page ID as specified in file.</param>
        /// <returns>The wiki page if found, or null.</returns>
        public WikiPage FindPageFromID(string pageID)
        {
            if (pageID == null)
                return null;

            foreach (var page in Pages)
            {
                if (page != null && page.ID == pageID)
                    return page;
            }
            return null;
        }
    }
}
