using OsuPracticeTools.Core.Scripts.BeatmapScripts;
using OsuPracticeTools.Core.Scripts.PracticeDiffScripts;
using OsuPracticeTools.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OsuPracticeTools.Core.Scripts.Helpers
{
    public static class ScriptParser
    {
        private static void Parse(Dictionary<List<Keys>, List<Script>> keyScripts, string path)
        {
            foreach (var line in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                if (line.StartsWith(">>")) break;
                try
                {
                    string[] parts;

                    if (line[0] == '~')
                    {
                        parts = line[1..].Split(':', StringSplitOptions.TrimEntries);
                        switch (parts[0].ToLower())
                        {
                            case "cs":
                                Program.StatKeys[0] = KeysHelper.Parse(parts[1]);
                                break;
                            case "ar":
                                Program.StatKeys[1] = KeysHelper.Parse(parts[1]);
                                break;
                            case "od":
                                Program.StatKeys[2] = KeysHelper.Parse(parts[1]);
                                break;
                            case "hp":
                                Program.StatKeys[3] = KeysHelper.Parse(parts[1]);
                                break;
                            case "rate":
                                Program.RateKey = KeysHelper.Parse(parts[1]);
                                break;
                            case "reset":
                                Program.ResetGlobalKey = parts[1].Split('/', StringSplitOptions.TrimEntries)
                                    .Select(KeysHelper.Parse).ToList();
                                break;
                        }
                    }

                    parts = line.Split(':', StringSplitOptions.TrimEntries);

                    var keyParts = parts[0].Split('+', StringSplitOptions.TrimEntries);

                    var keyModifiers = Keys.None;

                    foreach (var keyModifier in keyParts[..^1])
                    {
                        if (keyModifier.ToLower() == "c")
                            keyModifiers |= Keys.Control;
                        else if (keyModifier.ToLower() == "s")
                            keyModifiers |= Keys.Shift;
                    }

                    var keys = keyParts[^1].Split('/');
                    var keyList = new List<Keys>();
                    foreach (var k in keys)
                    {
                        var key = KeysHelper.Parse(k);
                        // make sure key is not control, shift, or alt
                        if (key is >= Keys.ShiftKey and <= Keys.Menu || key is >= Keys.LShiftKey and <= Keys.RMenu ||
                            key is Keys.Control or Keys.Shift or Keys.Alt)
                            continue;

                        key |= keyModifiers;

                        if (!keyList.Contains(key))
                            keyList.Add(key);
                    }

                    if (!keyList.Any())
                        continue;

                    var script = parts[1];
                    var keyScript = keyScripts.Keys.FirstOrDefault(keyList.SequenceEqual);
                    if (keyScript != null)
                        keyScripts[keyScript].AddRange(ParseScripts(script));
                    else
                        keyScripts.Add(keyList, ParseScripts(script));
                }
                catch (Exception)
                {
                    Logger.LogMessage($"Error: failed to parse line >> {line} <<");
                }
            }
        }

        private static Script ParseScript(string script)
        {
            var type = script.Split(' ', 2, StringSplitOptions.TrimEntries)[0].ToLower();
            return type switch
            {
                "reload" => new ReloadScript(script),
                "set" => new SetSettingsScript(script),
                "adddiff" => new AddDiffScript(script),
                "enddiff" => new EndDiffScript(script),
                "updatediff" => new UpdateDiffScript(script),
                "updatediffend" => new UpdateDiffEndScript(script),
                "deldiff" => new DeleteDiffScript(script),
                "cleardiffs" => new ClearDiffsScript(script),
                "creatediffs" => new CreateDiffsScript(script),
                "add" => new AddMapScript(script),
                "del" => new DeleteMapScript(script),
                "create" => new CreateMapScript(script),
                "clearmaps" => new ClearMapsScript(script),
                "createmaps" => new CreateMapsScript(script),
                _ => throw new ArgumentOutOfRangeException(nameof(script), $"{type} is not a valid command")
            };
        }

        private static List<Script> ParseScripts(string line) => line.Split('|', StringSplitOptions.TrimEntries).Select(ParseScript).ToList();

        public static void ParseScripts(Dictionary<List<Keys>, List<Script>> keyScriptDictionary)
        {
            if (!File.Exists("scripts.txt") && !File.Exists("defaultscripts.txt"))
            {
                Application.Exit();
                return;
            }

            if (File.Exists("defaultscripts.txt"))
                Parse(keyScriptDictionary, "defaultscripts.txt");

            var customKeyScriptDictionary = new Dictionary<List<Keys>, List<Script>>();

            if (File.Exists("scripts.txt"))
                Parse(customKeyScriptDictionary, "scripts.txt");

            foreach (var (key, value) in customKeyScriptDictionary)
            {
                var oldKey = keyScriptDictionary.Keys.FirstOrDefault(key.SequenceEqual);
                if (oldKey is null)
                    keyScriptDictionary[key] = value;
                else
                    keyScriptDictionary[oldKey] = value;
            }
        }
    }
}
