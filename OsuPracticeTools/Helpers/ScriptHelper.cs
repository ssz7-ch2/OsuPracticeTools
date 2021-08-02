using OsuLightBeatmapParser;
using OsuPracticeTools.Forms;
using OsuPracticeTools.Helpers.BeatmapHelpers;
using OsuPracticeTools.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace OsuPracticeTools.Helpers
{
    public static class ScriptHelper
    {
        private static string _prevBeatmapFile;
        public static int SetGlobalSettings(List<Keys> keys, string beatmapFile, Keys[] statKeys, Keys rateKey, List<Keys> resetKey)
        {
            if (keys.SequenceEqual(resetKey))
            {
                Script.GlobalSettings = new ScriptSettings();
                return 0;
            }

            var rateAmount = 0.1;
            var amount = 0.5f;
            bool changeAmount = false;
            if (keys[0].HasFlag(Keys.Shift))
                changeAmount = true;

            keys = keys.ConvertAll(k => k & Keys.KeyCode);

            if (_prevBeatmapFile != beatmapFile)
                Script.ParsedBeatmap = null;
            if (Script.ParsedBeatmap is null)
                Script.ParsedBeatmap = BeatmapDecoder.Decode(beatmapFile);

            _prevBeatmapFile = beatmapFile;

            if (keys[0] == rateKey)
            {

                if (keys.Count == 2)
                {
                    if (changeAmount)
                        rateAmount = 0.01;
                    
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            if (rateAmount == 0.1)
                                Script.GlobalSettings.SpeedRate = CeilToNearest(Script.GlobalSettings.SpeedRate, 0.1, 1);
                            Script.GlobalSettings.SpeedRate = Math.Max(Script.GlobalSettings.SpeedRate - rateAmount, 0.5);
                            break;
                        case Keys.Oemplus:
                            if (rateAmount == 0.1)
                                Script.GlobalSettings.SpeedRate = FloorToNearest(Script.GlobalSettings.SpeedRate, 0.1, 1);
                            Script.GlobalSettings.SpeedRate = Math.Min(Script.GlobalSettings.SpeedRate + rateAmount, 2);
                            break;
                        case Keys.Back:
                            Script.GlobalSettings.SpeedRate = 1;
                            break;
                    }
                }
               
                var bpm = Script.ParsedBeatmap.General.MainBPM * Script.GlobalSettings.SpeedRate;
                MessageForm.ShowMessage($"Rate: {Script.GlobalSettings.SpeedRate:0.0#}x ({Convert.ToInt32(bpm)}bpm)");
            }
            else if (keys[0] == statKeys[0])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1f;
                    
                    Script.GlobalSettings.CS ??= Script.ParsedBeatmap.Difficulty.CircleSize;
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            Script.GlobalSettings.CS = (float)CeilToNearest((float)Script.GlobalSettings.CS, amount, 1);
                            Script.GlobalSettings.CS = Math.Max((float)Script.GlobalSettings.CS - amount, 0);
                            break;
                        case Keys.Oemplus:
                            Script.GlobalSettings.CS = (float)FloorToNearest((float)Script.GlobalSettings.CS, amount, 1);
                            Script.GlobalSettings.CS = Math.Min((float)Script.GlobalSettings.CS + amount, 10);
                            break;
                        case Keys.Back:
                            Script.GlobalSettings.CS = null;
                            break;
                    }
                    Script.GlobalSettings.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"CS: {Script.GlobalSettings.CS ?? Script.ParsedBeatmap.Difficulty.CircleSize:0.0#}");
            }
            else if (keys[0] == statKeys[1])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1f;

                    Script.GlobalSettings.AR ??= BeatmapDifficulty.ApplyRateChangeAR(Script.ParsedBeatmap.Difficulty.ApproachRate, Script.GlobalSettings.SpeedRate);
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            Script.GlobalSettings.AR = (float)CeilToNearest((float)Script.GlobalSettings.AR, amount, 1);
                            Script.GlobalSettings.AR = Math.Max((float)Script.GlobalSettings.AR - amount, 0);
                            break;
                        case Keys.Oemplus:
                            Script.GlobalSettings.AR = (float)FloorToNearest((float)Script.GlobalSettings.AR, amount, 1);
                            Script.GlobalSettings.AR = Math.Min((float)Script.GlobalSettings.AR + amount, 10);
                            break;
                        case Keys.Back:
                            Script.GlobalSettings.AR = null;
                            break;
                    }
                    Script.GlobalSettings.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"AR: {Script.GlobalSettings.AR ?? Math.Round(BeatmapDifficulty.ApplyRateChangeAR(Script.ParsedBeatmap.Difficulty.ApproachRate, Script.GlobalSettings.SpeedRate), 2, MidpointRounding.ToEven):0.0#}");
            }
            else if (keys[0] == statKeys[2])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1f;

                    Script.GlobalSettings.OD ??= BeatmapDifficulty.ApplyRateChangeOD(Script.ParsedBeatmap.Difficulty.OverallDifficulty, Script.GlobalSettings.SpeedRate);
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            Script.GlobalSettings.OD = (float)CeilToNearest((float)Script.GlobalSettings.OD, amount, 1);
                            Script.GlobalSettings.OD = Math.Max((float)Script.GlobalSettings.OD - amount, 0);
                            break;
                        case Keys.Oemplus:
                            Script.GlobalSettings.OD = (float)FloorToNearest((float)Script.GlobalSettings.OD, amount, 1);
                            Script.GlobalSettings.OD = Math.Min((float)Script.GlobalSettings.OD + amount, 10);
                            break;
                        case Keys.Back:
                            Script.GlobalSettings.OD = null;
                            break;
                    }
                    Script.GlobalSettings.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"OD: {Script.GlobalSettings.OD ?? Math.Round(BeatmapDifficulty.ApplyRateChangeOD(Script.ParsedBeatmap.Difficulty.OverallDifficulty, Script.GlobalSettings.SpeedRate), 2, MidpointRounding.ToEven):0.0#}");
            }
            else if (keys[0] == statKeys[3])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1f;

                    Script.GlobalSettings.HP ??= Script.ParsedBeatmap.Difficulty.HPDrainRate;
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            Script.GlobalSettings.HP = (float)CeilToNearest((float)Script.GlobalSettings.HP, amount, 1);
                            Script.GlobalSettings.HP = Math.Max((float)Script.GlobalSettings.HP - amount, 0);
                            break;
                        case Keys.Oemplus:
                            Script.GlobalSettings.HP = (float)FloorToNearest((float)Script.GlobalSettings.HP, amount, 1);
                            Script.GlobalSettings.HP = Math.Min((float)Script.GlobalSettings.HP + amount, 10);
                            break;
                        case Keys.Back:
                            Script.GlobalSettings.HP = null;
                            break;
                    }
                    Script.GlobalSettings.DifficultyModified = true;
                }
                
                MessageForm.ShowMessage($"HP: {Script.GlobalSettings.HP ?? Script.ParsedBeatmap.Difficulty.HPDrainRate:0.0#}");
            }
            else
                return -1;

            return 0;
        }

        private static double CeilToNearest(double input, double value, int round) => Math.Ceiling(Math.Round(input * (1 / value), round)) / (1 / value);
        private static double FloorToNearest(double input, double value, int round) => Math.Floor(Math.Round(input * (1 / value), round)) / (1 / value);

        // copies script settings from B to A
        public static void CopySettings(ScriptSettings settingsA, ScriptSettings settingsB)
        {
            settingsA.SpeedRate = settingsB.SpeedRate;
            settingsA.CS = settingsB.CS ?? settingsA.CS;
            settingsA.AR = settingsB.AR ?? settingsA.AR;
            settingsA.OD = settingsB.OD ?? settingsA.OD;
            settingsA.HP = settingsB.HP ?? settingsA.HP;
            settingsA.DifficultyModified = settingsA.DifficultyModified || settingsB.DifficultyModified;
            /*var scriptSettings = typeof(ScriptSettings);
            foreach (var property in scriptSettings.GetProperties())
                scriptSettings.GetProperty(property.Name)?.SetValue(settingsA, property.GetValue(settingsB));*/

        }

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

        private static List<Script> ParseScripts(string line) => line.Split('|', StringSplitOptions.TrimEntries).Select(script => new Script(script)).ToList();

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
