using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace InGameWiki
{
    public static class PageParser
    {
        private static Dictionary<string, string> pageTags = new Dictionary<string, string>();
        private static Dictionary<Type, (MethodInfo, bool)> classToParser = new Dictionary<Type, (MethodInfo, bool)>();

        /// <summary>
        /// Generates wiki pages from the .txt files supplied by the mod inside it's Wiki folder.
        /// Uses the currently active language where possible, or defaults to English.
        /// </summary>
        /// <param name="wiki">The mod wiki to generate the files for.</param>
        /// <param name="dir">The Wiki directory to find files in.</param>
        /// <returns>The name of the language used, or null if loading failed.</returns>
        public static string AddAllFromDirectory(ModWiki wiki, string dir)
        {
            if (wiki == null)
                return null;
            if (!Directory.Exists(dir))
                return null;

            var activeLang = LanguageDatabase.activeLanguage;
            string langName = activeLang.folderName;
            string defaultLangName = LanguageDatabase.DefaultLangFolderName;

            bool hasActiveLanguage = Directory.Exists(Path.Combine(dir, langName));
            if (!hasActiveLanguage)
            {
                string modName = wiki.Mod?.Content?.Name ?? "UKN";
                Log.Warning($"Mod {modName} has a wiki folder, but does not support language '{langName}'. {(langName == defaultLangName ? "Falling back to first found." : $"Falling back to '{defaultLangName}', or first found.")} ");
                // Look for default (english) language.
                hasActiveLanguage = Directory.Exists(Path.Combine(dir, defaultLangName));

                if (hasActiveLanguage)
                {
                    // English found. Use it.
                    dir = Path.Combine(dir, defaultLangName);
                    Log.Warning($"Using {defaultLangName}.");
                }
                else
                {
                    // Okay, mod doesn't have native language and also doesn't have english. Look for any language at all.
                    var folders = Directory.GetDirectories(dir, "", SearchOption.TopDirectoryOnly);
                    if (folders.Length == 0)
                    {
                        Log.Warning($"Mod {modName} has wiki folder, but no languages. The folder structure should be 'ModName/Wiki/LanguageName/'.");
                    }
                    else
                    {
                        // Just go with the first one.
                        dir = folders[0];
                        Log.Warning($"Failed to find wiki in '{defaultLangName}', using first found: '{new DirectoryInfo(dir).Name}'.");
                    }
                }
            }
            else
            {
                // This mod has support for the native language! Nice!
                dir = Path.Combine(dir, langName);
            }

            var files = Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories).ToList();

            files.Sort((a, b) =>
            {
                string nameA = new FileInfo(a).Name;
                string nameB = new FileInfo(b).Name;

                return -string.Compare(nameA, nameB, StringComparison.InvariantCultureIgnoreCase);
            });

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                string fileName = info.Name;
                fileName = fileName.Substring(0, fileName.Length - info.Extension.Length);
                if (fileName.StartsWith("All_"))
                {
                    // For example All_RimForge.WikiAlloyParser.Method.txt
                    string methodPath = fileName.Substring("All_".Length);
                    int last = methodPath.LastIndexOf('.');
                    if (last < 0)
                    {
                        Log.Error($"Wiki parse error: The All_ method path '{methodPath}' is invalid. Expected format: 'Namespace.Class.MethodName'");
                        continue;
                    }
                    string final = methodPath.Substring(0, last) + ':' + methodPath.Substring(last + 1);
                    var method = AccessTools.Method(final);
                    if (method == null)
                    {
                        Log.Error($"Wiki parse error: The All_ method '{methodPath}' does not correspond to any found class/method. Check spelling!");
                        continue;
                    }

                    if (!method.IsStatic || method.IsGenericMethod || method.ReturnType != typeof(bool) ||
                        method.ContainsGenericParameters || method.GetParameters().Length != 1 ||
                        method.GetParameters()[0].ParameterType != typeof(ThingDef))
                    {
                        Log.Error($"Wiki parse error: The All_ method '{methodPath}' was found but is invalid: it must be a static method that returns a boolean and takes a single parameter of type ThingDef");
                        continue;
                    }

                    var allDefs = wiki?.Mod?.Content?.AllDefs;
                    if (allDefs == null)
                    {
                        Log.Error("Unexpected wiki error when parsing All_ page: wiki does not belong to any mod, so cannot scan defs.");
                        continue;
                    }

                    string rawText = File.ReadAllText(file);
                    foreach (var def in wiki.Mod.Content.AllDefs)
                    {
                        if (!(def is ThingDef thing))
                            continue;
                        bool include;
                        try
                        {
                            include = (bool) method.Invoke(null, new object[] {thing});
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.ToString());
                            continue;
                        }

                        if (include)
                        {
                            var existing = wiki.FindPageFromDef(thing);
                            if (existing == null)
                                continue;

                            Parse(wiki, rawText, existing, fileName);
                        }
                    }

                    continue;
                }
                if (fileName.StartsWith("Thing_"))
                {
                    string thingDefName = fileName.Substring(6);
                    var existing = wiki.FindPageFromDef(thingDefName);
                    if (existing != null)
                    {
                        Parse(wiki, File.ReadAllText(file), existing, fileName);
                        //Log.Message("Added to existing " + file);
                    }
                    else
                    {
                        Log.Error("Failed to find Thing wiki entry for wiki page: Thing_" + thingDefName);
                    }
                    continue;
                }

                var page = Parse(wiki, File.ReadAllText(file), null, fileName);
                if (page == null)
                {
                    Log.Error($"Failed to load wiki page from {file}");
                    continue;
                }

                //Log.Message("Added " + file);
                wiki.Pages.Insert(0, page);
            }

            return new DirectoryInfo(dir).Name;
        }

        private static string TryGetTag(string tag, string ifNotFound = null)
        {
            return pageTags.TryGetValue(tag, out string found) ? found : ifNotFound;
        }

        public static WikiPage Parse(ModWiki wiki, string rawText, WikiPage existing, string fileName)
        {
            string[] lines = rawText.Split('\n');

            // Load up tags.
            pageTags.Clear();
            const string ENDTAGS = "ENDTAGS";
            int metaEndLine = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == ENDTAGS)
                {
                    metaEndLine = i;
                    break;
                }
            }
            if (metaEndLine == -1)
            {
                // This is only allowed if this is not an external page.
                if (existing == null)
                {
                    Log.Error($"External wiki page '{fileName}' does not have an ENDTAGS line.\nExternal pages need to have the following format:\n\nTAG:Data\nOTHERTAG:Other Data\nENDTAGS\n... Page data below ...\n\nRaw page:\n" + rawText);
                    return null;
                }
            }
            if (metaEndLine != -1)
            {
                for (int i = 0; i < metaEndLine; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrWhiteSpace(line))
                        continue;

                    if (!line.Contains(':'))
                    {
                        Log.Error($"External wiki page '{fileName}' tag error, line {i + 1}: incorrect format. Expected 'TAG:Data', got '{line.Trim()}'.\nRaw file:\n{rawText}");
                        continue;
                    }

                    var parts = line.Split(':');
                    string tag = parts[0];
                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        Log.Error($"External wiki page '{fileName}' tag error, line {i + 1}: blank tag.\nRaw file:\n{rawText}");
                        continue;
                    }
                    if (pageTags.ContainsKey(tag))
                    {
                        Log.Error($"External wiki page '{fileName}' tag error, line {i + 1}: duplicate tag '{tag}'.\nRaw file:\n{rawText}");
                        continue;
                    }
                    pageTags.Add(tag, parts[1].Trim());
                }
            }

            WikiPage p = existing ?? new WikiPage(wiki);
            if (existing == null)
            {
                const string INVALID_ID = "INVALID_ID_ERROR";
                string id = TryGetTag("ID", INVALID_ID);
                string title = TryGetTag("Title", "<No title specified>");
                Texture2D icon = ContentFinder<Texture2D>.Get(TryGetTag("Icon", ""), false);
                Texture2D bg = ContentFinder<Texture2D>.Get(TryGetTag("Background", ""), false);
                string requiredResearch = TryGetTag("RequiredResearch", "");
                string desc = TryGetTag("Description", "");
                bool alwaysSpoiler = TryGetTag("AlwaysSpoiler", "false") != "false";

                if (id == INVALID_ID)
                    Log.Warning($"External wiki page '{fileName}' with title {title} does not have an ID tag. It should specify 'ID: MyPageID'. It may break things.");

                p.ID = id;
                p.Title = title;
                p.Icon = icon;
                p.ShortDescription = desc;
                p.Background = bg;
                if (requiredResearch != string.Empty)
                    p.RequiresResearchRaw = requiredResearch;
                if (alwaysSpoiler)
                    p.IsAlwaysSpoiler = true;
            }
            else
            {
                Texture2D bg = ContentFinder<Texture2D>.Get(TryGetTag("Background", ""), false);
                Texture2D icon = ContentFinder<Texture2D>.Get(TryGetTag("Icon", "WIKI__ICON_NOT_SPECIFIED"), false);
                string requiredResearch = TryGetTag("RequiredResearch", "");
                string desc = TryGetTag("Description", null);
                bool alwaysSpoiler = TryGetTag("AlwaysSpoiler", "false") != "false";

                p.Background = bg;
                if (requiredResearch != string.Empty)
                    p.RequiresResearchRaw = requiredResearch;
                if (alwaysSpoiler)
                    p.IsAlwaysSpoiler = true;
                if (icon != null)
                    p.Icon = icon;
                if (desc != null)
                    p.ShortDescription = desc;
            }

            StringBuilder str = new StringBuilder();
            CurrentlyParsing parsing = CurrentlyParsing.None;
            for (int i = metaEndLine + 1; i < lines.Length; i++)
            {
                string line = lines[i];
                line += '\n';

                char last = char.MinValue;
                foreach (var c in line)
                {
                    int add = 0;

                    // Custom elements
                    string final = CheckParseChar('|', CurrentlyParsing.Custom, last, i, c, ref parsing, ref add);
                    if (final != null)
                    {
                        AddCustom(final);
                    }

                    // Text
                    final = CheckParseChar('#', CurrentlyParsing.Text, last, i, c, ref parsing, ref add);
                    if(final != null)
                    {
                        if (final.StartsWith("!"))
                            AddText(final.Substring(1), true);
                        else
                            AddText(final, false);
                        continue;
                    }

                    // Images
                    final = CheckParseChar('$', CurrentlyParsing.Image, last, i, c, ref parsing, ref add);
                    if (final != null)
                    {
                        Vector2? size = null;
                        if (final.Contains(':'))
                        {
                            var split = final.Split(':');
                            final = split[0];
                            if (split[1].Contains(","))
                            {
                                bool workedX = float.TryParse(split[1].Split(',')[0], out float x);
                                bool workedY = float.TryParse(split[1].Split(',')[1], out float y);

                                if (workedX && workedY)
                                {
                                    size = new Vector2(x, y);
                                }
                                else
                                {
                                    Log.Error($"Error in wiki parse: failed to parse Vector2 for image size, '{split[1]}', failed to parse {(workedX ? "y" : "x")} value as a float.");
                                }
                            }
                            else
                            {
                                Log.Error($"Error in wiki parse: failed to parse Vector2 for image size, '{split[1]}', expected format 'x, y'.");
                            }
                        }
                        Texture2D loaded = ContentFinder<Texture2D>.Get(final, false);
                        p.Elements.Add(new WikiElement()
                        {
                            Image = loaded,
                            AutoFitImage = size == null,
                            ImageSize = size ?? new Vector2(-1, -1)
                        });
                        continue;
                    }

                    // ThingDef links
                    final = CheckParseChar('@', CurrentlyParsing.ThingDefLink, last, i, c, ref parsing, ref add);
                    if (final != null)
                    {
                        string label = null;
                        if (final.Contains(':'))
                        {
                            var split = final.Split(':');
                            final = split[0];
                            label = split[1];
                        }

                        Def def = ThingDef.Named(final);
                        if (def != null)
                        {
                            p.Elements.Add(new WikiElement()
                            {
                                DefForIconAndLabel = def,
                                Text = label
                            });
                        }
                        else
                        {
                            AddText($"<i>MissingDefLink [{final}]</i>", false);
                        }
                        continue;
                    }

                    // Page links
                    final = CheckParseChar('~', CurrentlyParsing.PageLink, last, i, c, ref parsing, ref add);
                    if (final != null)
                    {
                        p.Elements.Add(new WikiElement()
                        {
                            PageLink = final,
                        });
                        continue;
                    }

                    if (add > 0)
                        str.Append(c);
                    last = c;
                }
            }
            str.Clear();

            string CheckParseChar(char tag, CurrentlyParsing newState, char last, int i, char c, ref CurrentlyParsing currentParsing, ref int shouldAppend)
            {
                if (c != tag)
                {
                    if (currentParsing != CurrentlyParsing.None)
                        shouldAppend++;
                    return null;
                }

                if (last == '\\')
                {
                    // Escape char. This must be added and the slash removed.
                    if (currentParsing != CurrentlyParsing.None)
                    {
                        str.Remove(str.Length - 1, 1);
                        shouldAppend++;
                    }
                    return null;
                }

                // This is either an opening or closing tag, either way it should definitely not be added.
                shouldAppend = -1000;

                if (currentParsing == CurrentlyParsing.None)
                {
                    // Start state.
                    currentParsing = newState;
                    str.Clear();
                    return null;
                }
                else if (currentParsing == newState)
                {
                    // End state.
                    currentParsing = CurrentlyParsing.None;
                    string s = str.ToString();
                    str.Clear();
                    return s;
                }
                else
                {
                    // Invalid.
                    Log.Error($"Error parsing wiki '{fileName}' on line {i + 1}: got '{c}' which is invalid since {currentParsing} is currently active. Raw page:\n{rawText}");
                    return null;
                }
            }

            void AddText(string txt, bool large)
            {
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    if (large)
                        txt = $"<color=cyan>{txt}</color>";

                    var text = WikiElement.Create(txt);
                    text.FontSize = large ? GameFont.Medium : GameFont.Small;
                    p.Elements.Add(text);
                }
            }

            void AddCustom(string txt)
            {
                if (string.IsNullOrWhiteSpace(txt))
                {
                    Log.Warning("Empty custom tag found when parsing wiki.");
                    return;
                }

                int index = txt.IndexOf(':');

                string klassPath = index < 0 ? txt : txt.Substring(0, index);
                string input = index < 0 ? null : txt.Substring(index + 1);

                Type foundType = GenTypes.GetTypeInAnyAssembly(klassPath);
                if (foundType == null)
                {
                    Log.Error($"Wiki: Failed to find class '{klassPath}' for custom element parsing.");
                    return;
                }

                var parser = GetParser(foundType, out bool isMultiReturn);
                if (parser == null)
                {
                    Log.Error($"Wiki: Failed to find parser method in class '{foundType.FullName}'. There should be a static method in the class that has a single input parameter of type InGameWiki.CustomElementArgs and a return value of type InGameWiki.WikiElement or an enumerable of WikiElements.");
                    return;
                }

                try
                {
                    var args = new CustomElementArgs(p, input);
                    if (isMultiReturn)
                    {
                        IEnumerable<WikiElement> result = parser.Invoke(null, new object[] { args }) as IEnumerable<WikiElement>;
                        if (result == null)
                            return;

                        foreach (var item in result)
                        {
                            if(item != null)
                                p.Elements.Add(item);
                        }
                    }
                    else
                    {
                        WikiElement result = parser.Invoke(null, new object[] { args }) as WikiElement;
                        if (result == null)
                            return;
                        
                        p.Elements.Add(result);
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Wiki: Exception executing custom element parser {foundType.FullName}.{parser.Name}():");
                    Log.Error(e.ToString());
                }
            }

            return p;
        }

        private static MethodInfo GetParser(Type type, out bool multi)
        {
            multi = false;
            if (type == null)
                return null;

            if (classToParser.TryGetValue(type, out var pair))
            {
                multi = pair.Item2;
                return pair.Item1;
            }

            var methods = AccessTools.GetDeclaredMethods(type);
            foreach(var method in methods)
            {
                if (method.IsStatic && !method.IsGenericMethod)
                {
                    var ps = method.GetParameters();
                    if (ps.Length != 1 || ps[0].ParameterType != typeof(CustomElementArgs))
                        continue;

                    bool isSimple = typeof(WikiElement).IsAssignableFrom(method.ReturnType);
                    bool isMulti = typeof(IEnumerable<WikiElement>).IsAssignableFrom(method.ReturnType);

                    if (!isSimple && !isMulti)
                        continue;

                    classToParser.Add(type, (method, isMulti));
                    multi = isMulti;
                    return method;
                }
            }

            classToParser.Add(type, (null, false));
            return null;
        }

        public enum CurrentlyParsing
        {
            None,
            Text,
            Image,
            ThingDefLink,
            PageLink,
            Custom
        }
    }
}
