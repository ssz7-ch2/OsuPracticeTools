﻿using OsuLightBeatmapParser;
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
        public static int SetGlobalOptions(List<Keys> keys, string beatmapFile, Keys[] statKeys, Keys rateKey, List<Keys> resetKey)
        {
            if (keys.SequenceEqual(resetKey))
            {
                Script.GlobalOptions = new ScriptOptions();
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
                                Script.GlobalOptions.SpeedRate = CeilToNearest(Script.GlobalOptions.SpeedRate, 0.1, 1);
                            Script.GlobalOptions.SpeedRate = Math.Max(Script.GlobalOptions.SpeedRate - rateAmount, 0.5);
                            break;
                        case Keys.Oemplus:
                            if (rateAmount == 0.1)
                                Script.GlobalOptions.SpeedRate = FloorToNearest(Script.GlobalOptions.SpeedRate, 0.1, 1);
                            Script.GlobalOptions.SpeedRate = Math.Min(Script.GlobalOptions.SpeedRate + rateAmount, 2);
                            break;
                        case Keys.Back:
                            Script.GlobalOptions.SpeedRate = 1;
                            break;
                    }
                }
               
                var bpm = Script.ParsedBeatmap.General.MainBPM * Script.GlobalOptions.SpeedRate;
                MessageForm.ShowMessage($"Rate: {Script.GlobalOptions.SpeedRate:0.0#}x ({Convert.ToInt32(bpm)}bpm)");
            }
            else if (keys[0] == statKeys[0])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1f;
                    
                    Script.GlobalOptions.CS ??= Script.ParsedBeatmap.Difficulty.CircleSize;
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            Script.GlobalOptions.CS = (float)CeilToNearest((float)Script.GlobalOptions.CS, amount, 1);
                            Script.GlobalOptions.CS = Math.Max((float)Script.GlobalOptions.CS - amount, 0);
                            break;
                        case Keys.Oemplus:
                            Script.GlobalOptions.CS = (float)FloorToNearest((float)Script.GlobalOptions.CS, amount, 1);
                            Script.GlobalOptions.CS = Math.Min((float)Script.GlobalOptions.CS + amount, 10);
                            break;
                        case Keys.Back:
                            Script.GlobalOptions.CS = null;
                            break;
                    }
                    Script.GlobalOptions.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"CS: {Script.GlobalOptions.CS ?? Script.ParsedBeatmap.Difficulty.CircleSize:0.0#}");
            }
            else if (keys[0] == statKeys[1])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1f;

                    Script.GlobalOptions.AR ??= BeatmapDifficulty.ApplyRateChangeAR(Script.ParsedBeatmap.Difficulty.ApproachRate, Script.GlobalOptions.SpeedRate);
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            Script.GlobalOptions.AR = (float)CeilToNearest((float)Script.GlobalOptions.AR, amount, 1);
                            Script.GlobalOptions.AR = Math.Max((float)Script.GlobalOptions.AR - amount, 0);
                            break;
                        case Keys.Oemplus:
                            Script.GlobalOptions.AR = (float)FloorToNearest((float)Script.GlobalOptions.AR, amount, 1);
                            Script.GlobalOptions.AR = Math.Min((float)Script.GlobalOptions.AR + amount, 10);
                            break;
                        case Keys.Back:
                            Script.GlobalOptions.AR = null;
                            break;
                    }
                    Script.GlobalOptions.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"AR: {Script.GlobalOptions.AR ?? Math.Round(BeatmapDifficulty.ApplyRateChangeAR(Script.ParsedBeatmap.Difficulty.ApproachRate, Script.GlobalOptions.SpeedRate), 2, MidpointRounding.ToEven):0.0#}");
            }
            else if (keys[0] == statKeys[2])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1f;

                    Script.GlobalOptions.OD ??= BeatmapDifficulty.ApplyRateChangeOD(Script.ParsedBeatmap.Difficulty.OverallDifficulty, Script.GlobalOptions.SpeedRate);
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            Script.GlobalOptions.OD = (float)CeilToNearest((float)Script.GlobalOptions.OD, amount, 1);
                            Script.GlobalOptions.OD = Math.Max((float)Script.GlobalOptions.OD - amount, 0);
                            break;
                        case Keys.Oemplus:
                            Script.GlobalOptions.OD = (float)FloorToNearest((float)Script.GlobalOptions.OD, amount, 1);
                            Script.GlobalOptions.OD = Math.Min((float)Script.GlobalOptions.OD + amount, 10);
                            break;
                        case Keys.Back:
                            Script.GlobalOptions.OD = null;
                            break;
                    }
                    Script.GlobalOptions.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"OD: {Script.GlobalOptions.OD ?? Math.Round(BeatmapDifficulty.ApplyRateChangeOD(Script.ParsedBeatmap.Difficulty.OverallDifficulty, Script.GlobalOptions.SpeedRate), 2, MidpointRounding.ToEven):0.0#}");
            }
            else if (keys[0] == statKeys[3])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1f;

                    Script.GlobalOptions.HP ??= Script.ParsedBeatmap.Difficulty.HPDrainRate;
                    switch (keys[1])
                    {
                        case Keys.OemMinus:
                            Script.GlobalOptions.HP = (float)CeilToNearest((float)Script.GlobalOptions.HP, amount, 1);
                            Script.GlobalOptions.HP = Math.Max((float)Script.GlobalOptions.HP - amount, 0);
                            break;
                        case Keys.Oemplus:
                            Script.GlobalOptions.HP = (float)FloorToNearest((float)Script.GlobalOptions.HP, amount, 1);
                            Script.GlobalOptions.HP = Math.Min((float)Script.GlobalOptions.HP + amount, 10);
                            break;
                        case Keys.Back:
                            Script.GlobalOptions.HP = null;
                            break;
                    }
                    Script.GlobalOptions.DifficultyModified = true;
                }
                
                MessageForm.ShowMessage($"HP: {Script.GlobalOptions.HP ?? Script.ParsedBeatmap.Difficulty.HPDrainRate:0.0#}");
            }
            else
                return -1;

            return 0;
        }

        private static double CeilToNearest(double input, double value, int round) => Math.Ceiling(Math.Round(input * (1 / value), round)) / (1 / value);
        private static double FloorToNearest(double input, double value, int round) => Math.Floor(Math.Round(input * (1 / value), round)) / (1 / value);

        // copies script options from B to A
        public static void CopyOptions(ScriptOptions optionsA, ScriptOptions optionsB)
        {
            optionsA.SpeedRate = optionsB.SpeedRate;
            optionsA.CS = optionsB.CS ?? optionsA.CS;
            optionsA.AR = optionsB.AR ?? optionsA.AR;
            optionsA.OD = optionsB.OD ?? optionsA.OD;
            optionsA.HP = optionsB.HP ?? optionsA.HP;
            optionsA.DifficultyModified = optionsA.DifficultyModified || optionsB.DifficultyModified;
            /*var scriptOptions = typeof(ScriptOptions);
            foreach (var property in scriptOptions.GetProperties())
                scriptOptions.GetProperty(property.Name)?.SetValue(optionsA, property.GetValue(optionsB));*/

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
