using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using InGameWiki.Internal;
using Verse;

[assembly: InternalsVisibleTo("InGameWikiMod")]

namespace InGameWiki
{
    public class ModWiki
    {
        /// <summary>
        /// The current loaded version of the wiki mod.
        /// This is the version of the mod API that is loaded. Ideally, it should match the API version downloaded from NuGet.
        /// </summary>
        public static string Version
        {
            get
            {
                return "1.1.0";
            }
        }

        public static bool NoSpoilerMode { get; set; } = true;
        public static IReadOnlyList<ModWiki> AllWikis
        {
            get
            {
                return allWikis;
            }
        }
        private static List<ModWiki> allWikis = new List<ModWiki>();

        internal static void Patch(Harmony harmonyInstance)
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
                harmonyInstance.Patch(method, postfix: patch);
                Log.Message("<color=cyan>Patched game for in-game wiki.</color>");
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
        /// </summary>
        /// <param name="mod">Your mod instance.</param>
        /// <returns>The newly created ModWiki object, or null if creation failed.</returns>
        public static ModWiki Create(Mod mod)
        {
            if(mod == null)
            {
                Log.Error("Cannot pass in null mod to create wiki.");
                return null;
            }

            try
            {
                var wiki = new ModWiki();
                wiki.Mod = mod;
                wiki.GenerateFromMod(mod);

                allWikis.Add(wiki);

                Log.Message($"<color=cyan>A new wiki was registered for mod '{mod.Content.Name}'.</color>");

                return wiki;
            }
            catch(Exception e)
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
            foreach (var def in mod.Content.AllDefs)
            {
                if (!(def is ThingDef thingDef))
                    continue;

                bool shouldAdd = AutogenPageFilter(thingDef);
                if (!shouldAdd)
                    continue;

                var page = WikiPage.CreateFromThingDef(thingDef); 
                this.Pages.Add(page);
            }

            string dir = Path.Combine(mod.Content.RootDir, "Wiki");
            PageParser.AddAllFromDirectory(this, dir);
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
