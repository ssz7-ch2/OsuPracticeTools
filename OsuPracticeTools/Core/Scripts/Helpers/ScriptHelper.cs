using OsuPracticeTools.Enums;
using System;
using System.Text.RegularExpressions;

namespace OsuPracticeTools.Core.Scripts.Helpers
{
    public static class ScriptHelper
    {
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

        public static void ScriptSettingsMatch(ScriptSettings settings, string arg, string param, string settingsString)
        {
            switch (arg)
            {
                case "g":
                    settings.UseGlobalSettings = true;
                    return;

                case "r":
                    settings.SpeedRate = string.IsNullOrEmpty(param) ? 1 : double.Parse(param);
                    if (Math.Abs(settings.SpeedRate - 1) < 0.001 || settings.SpeedRate is < 0.1d or > 5d)
                        settings.SpeedRate = 1;
                    break;
                case "overwrite":
                    settings.Overwrite = true;
                    break;
                case "bpm":
                    settings.BPM = string.IsNullOrEmpty(param) ? null : double.Parse(param);
                    break;
                case "pitch":
                    settings.Pitch = true;
                    break;
                case "hr":
                    settings.HardRock = true;
                    break;
                case "flip":
                    settings.FlipDirection = string.IsNullOrEmpty(param) ? FlipDirection.Horizontal : FlipDirection.Vertical;
                    break;
                case "rs":
                    settings.RemoveSpinners = true;
                    break;
                case "cs":
                    settings.CS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "ar":
                    settings.AR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "od":
                    settings.OD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "hp":
                    settings.HP = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "maxcs":
                    settings.MaxCS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "mincs":
                    settings.MinCS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "maxar":
                    settings.MaxAR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "minar":
                    settings.MinAR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "maxod":
                    settings.MaxOD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "minod":
                    settings.MinOD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                    settings.DifficultyModified = true;
                    break;
                case "f":
                    // too hard to get name format in same regex
                    var formatMatch = new Regex(@"-f +'(.+)'.+").Match(settingsString.Replace('"', '\''));
                    if (formatMatch.Success)
                    {
                        settings.NameFormat = formatMatch.Groups[1].Value;
                    }
                    break;
                case "fauto":
                    formatMatch = new Regex(@"-fauto +'(.+)'.+").Match(settingsString.Replace('"', '\''));
                    if (formatMatch.Success)
                    {
                        settings.NameFormat = "{v}{HR}{FLIP}{R}{BPM}{CS}{AR}{OD}{HP}{RS}" + " " + formatMatch.Groups[1].Value;
                    }
                    break;
            }
        }
    }
}
