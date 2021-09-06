using OsuLightBeatmapParser;
using OsuPracticeTools.Core.BeatmapHelpers;
using OsuPracticeTools.Core.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace OsuPracticeTools.Core.GlobalSettings
{
    public class GlobalSettingsHelper
    {
        //TODO: use dictionary to store mapping between hotkeys and property
        private static string _prevBeatmapFile;
        public static int SetGlobalSettings(List<Keys> keys, Keys[] statKeys, Keys rateKey, List<Keys> resetKey)
        {
            if (keys.SequenceEqual(resetKey))
            {
                Info.GlobalSettings = new ScriptSettings();
                return 0;
            }

            var rateAmount = 0.1;
            var amount = 0.5;
            var changeAmount = keys[0].HasFlag(Keys.Shift);

            keys = keys.ConvertAll(k => k & Keys.KeyCode);

            if (_prevBeatmapFile != Info.BeatmapFile)
                Info.ParsedBeatmap = null;
            Info.ParsedBeatmap ??= BeatmapDecoder.Decode(Info.BeatmapFile);

            _prevBeatmapFile = Info.BeatmapFile;

            if (keys[0] == rateKey)
            {

                if (keys.Count == 2)
                {
                    if (changeAmount)
                        rateAmount = 0.01;

                    Info.GlobalSettings.SpeedRate = (double)AdjustValue(Info.GlobalSettings, Info.GlobalSettings.SpeedRate, keys[1], rateAmount, 0.5, 2, 1);
                }

                var bpm = Info.ParsedBeatmap.General.MainBPM * Info.GlobalSettings.SpeedRate;
                MessageForm.ShowMessage($"Rate: {Info.GlobalSettings.SpeedRate:0.0#}x ({Convert.ToInt32(bpm)}bpm)");
            }
            else if (keys[0] == statKeys[0])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1;

                    Info.GlobalSettings.CS ??= Info.ParsedBeatmap.Difficulty.CircleSize;
                    var value = AdjustValue(Info.GlobalSettings, (float)Info.GlobalSettings.CS, keys[1], amount, 0, 10, null);
                    Info.GlobalSettings.CS = value != null ? Convert.ToSingle(value) : null;
                    Info.GlobalSettings.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"CS: {Info.GlobalSettings.CS ?? Info.ParsedBeatmap.Difficulty.CircleSize:0.0#}");
            }
            else if (keys[0] == statKeys[1])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1;

                    Info.GlobalSettings.AR ??= BeatmapDifficulty.ApplyRateChangeAR(Info.ParsedBeatmap.Difficulty.ApproachRate, Info.GlobalSettings.SpeedRate);
                    var value = AdjustValue(Info.GlobalSettings, (float)Info.GlobalSettings.AR, keys[1], amount, 0, 10, null);
                    Info.GlobalSettings.AR = value != null ? Convert.ToSingle(value) : null;
                    Info.GlobalSettings.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"AR: {Info.GlobalSettings.AR ?? Math.Round(BeatmapDifficulty.ApplyRateChangeAR(Info.ParsedBeatmap.Difficulty.ApproachRate, Info.GlobalSettings.SpeedRate), 2, MidpointRounding.ToEven):0.0#}");
            }
            else if (keys[0] == statKeys[2])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1;

                    Info.GlobalSettings.OD ??= BeatmapDifficulty.ApplyRateChangeOD(Info.ParsedBeatmap.Difficulty.OverallDifficulty, Info.GlobalSettings.SpeedRate);
                    var value = AdjustValue(Info.GlobalSettings, (float)Info.GlobalSettings.OD, keys[1], amount, 0, 10, null);
                    Info.GlobalSettings.OD = value != null ? Convert.ToSingle(value) : null;
                    Info.GlobalSettings.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"OD: {Info.GlobalSettings.OD ?? Math.Round(BeatmapDifficulty.ApplyRateChangeOD(Info.ParsedBeatmap.Difficulty.OverallDifficulty, Info.GlobalSettings.SpeedRate), 2, MidpointRounding.ToEven):0.0#}");
            }
            else if (keys[0] == statKeys[3])
            {
                if (keys.Count == 2)
                {
                    if (changeAmount)
                        amount = 0.1;

                    Info.GlobalSettings.HP ??= Info.ParsedBeatmap.Difficulty.HPDrainRate;
                    var value = AdjustValue(Info.GlobalSettings, (float)Info.GlobalSettings.HP, keys[1], amount, 0, 10, null);
                    Info.GlobalSettings.HP = value != null ? Convert.ToSingle(value) : null;
                    Info.GlobalSettings.DifficultyModified = true;
                }

                MessageForm.ShowMessage($"HP: {Info.GlobalSettings.HP ?? Info.ParsedBeatmap.Difficulty.HPDrainRate:0.0#}");
            }
            else
                return -1;

            return 0;
        }

        private static object AdjustValue(ScriptSettings globalSettings, double value, Keys key, double amount, double min, double max, object defaultValue)
        {
            return key switch
            {
                Keys.OemMinus => Math.Max(CeilToNearest(value, amount) - amount, min),
                Keys.Oemplus => Math.Min(FloorToNearest(value, amount) + amount, max),
                Keys.Back => defaultValue,
                _ => value,
            };
        }

        private static double CeilToNearest(double input, double value) => Math.Ceiling(Math.Round(input / value, 2)) * value;
        private static double FloorToNearest(double input, double value) => Math.Floor(Math.Round(input / value, 2)) * value;
    }
}
