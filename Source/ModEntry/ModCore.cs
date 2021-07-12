using HarmonyLib;
using InGameWiki;
using RimWorld;
using UnityEngine;
using Verse;

namespace InGameWikiMod
{
    public class ModCore : Mod
    {
        public static ModCore Instance;

        public ModCore(ModContentPack content) : base(content)
        {
            Instance = this;
            GetSettings<WikiModSettings>();

            Harmony harmony = new Harmony("co.uk.epicguru.ingamewiki");
            ModWiki.Patch(harmony, WikiModSettings.InspectorButtonEnabled);
            try
            {
                Log.Message($"<color=cyan>Finished loading in-game wiki mod: Version {ModWiki.APIVersion}</color>");
            }
            catch
            {
                // Ignore.
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GetSettings<WikiModSettings>().Draw(inRect);
        }

        public override string SettingsCategory()
        {
            return "In-Game Wiki";
        }
    }

    public class WikiModSettings : ModSettings
    {
        public static bool TabButtonEnabled = true;
        public static bool InspectorButtonEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref TabButtonEnabled, "TabButtonEnabled", true);
            Scribe_Values.Look(ref InspectorButtonEnabled, "InspectorButtonEnabled", true);
        }

        public void Apply()
        {
            WikiDefOf.WikiButton.buttonVisible = TabButtonEnabled;
        }

        public void Draw(Rect rect)
        {
            Listing_Standard l = new Listing_Standard();
            l.Begin(rect);

            bool old = TabButtonEnabled;
            l.CheckboxLabeled("Wiki.ShowMenuBarButton".Translate(), ref TabButtonEnabled, "Wiki.ShowMenuBarButtonDesc".Translate());
            if (old != TabButtonEnabled)
                Apply();

            l.CheckboxLabeled("Wiki.ShowInspectorButton".Translate(), ref InspectorButtonEnabled, "Wiki.ShowInspectorButtonDesc".Translate());

            l.End();
        }
    }

    [DefOf]
    public static class WikiDefOf
    {
        static WikiDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(WikiDefOf));
        }

        public static MainButtonDef WikiButton;
    }

    [StaticConstructorOnStartup]
    public static class StaticStart
    {
        static StaticStart()
        {
            ModCore.Instance.GetSettings<WikiModSettings>().Apply(); // Show or hide button.
        }
    }
}
