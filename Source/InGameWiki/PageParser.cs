using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace InGameWiki
{
    public static class PageParser
    {
        private static Dictionary<string, string> pageTags = new Dictionary<string, string>();

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
                if (fileName.StartsWith("Thing_"))
                {
                    string thingDefName = fileName.Substring(6);
                    var existing = wiki.FindPageFromDef(thingDefName);
                    if (existing != null)
                    {
                        Parse(File.ReadAllText(file), existing);
                        //Log.Message("Added to existing " + file);
                    }
                    else
                    {
                        Log.Error("Failed to find Thing wiki entry for wiki page: Thing_" + thingDefName);
                    }
                    continue;
                }

                var page = Parse(File.ReadAllText(file), null);
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

        public static WikiPage Parse(string rawText, WikiPage existing)
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
                    Log.Error("External wiki page does not have an ENDTAGS line.\nExternal pages need to have the following format:\n\nTAG:Data\nOTHERTAG:Other Data\nENDTAGS\n... Page data below ...\n\nRaw page:\n" + rawText);
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
                        Log.Error($"External wiki page tag error, line {i + 1}: incorrect format. Expected 'TAG:Data', got '{line.Trim()}'.\nRaw file:\n{rawText}");
                        continue;
                    }

                    var parts = line.Split(':');
                    string tag = parts[0];
                    if (string.IsNullOrWhiteSpace(tag))
                    {
                        Log.Error($"External wiki page tag error, line {i + 1}: blank tag.\nRaw file:\n{rawText}");
                        continue;
                    }
                    if (pageTags.ContainsKey(tag))
                    {
                        Log.Error($"External wiki page tag error, line {i + 1}: duplicate tag '{tag}'.\nRaw file:\n{rawText}");
                        continue;
                    }
                    pageTags.Add(tag, parts[1].Trim());
                }
            }

            WikiPage p = existing ?? new WikiPage();
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
                    Log.Warning($"External wiki page with title {title} does not have an ID tag. It should specify 'ID: MyPageID'. It may break things.");

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
                string requiredResearch = TryGetTag("RequiredResearch", "");
                bool alwaysSpoiler = TryGetTag("AlwaysSpoiler", "false") != "false";

                p.Background = bg;
                if (requiredResearch != string.Empty)
                    p.RequiresResearchRaw = requiredResearch;
                if (alwaysSpoiler)
                    p.IsAlwaysSpoiler = true;
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

                    // Text
                    string final = CheckParseChar('#', CurrentlyParsing.Text, last, i, c, ref parsing, ref add);
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
                    Log.Error($"Error parsing wiki on line {i + 1}: got '{c}' which is invalid since {currentParsing} is currently active. Raw page:\n{rawText}");
                    return null;
                }
            }

            void AddText(string txt, bool large)
            {
                if (!string.IsNullOrWhiteSpace(txt))
                {
                    var text = WikiElement.Create(txt);
                    text.FontSize = large ? GameFont.Medium : GameFont.Small;
                    p.Elements.Add(text);
                }
            }

            return p;
        }

        public enum CurrentlyParsing
        {
            None,
            Text,
            Image,
            ThingDefLink,
            PageLink
        }
    }
}
