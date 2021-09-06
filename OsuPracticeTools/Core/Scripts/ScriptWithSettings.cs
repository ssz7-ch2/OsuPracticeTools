using OsuLightBeatmapParser.Enums;
using OsuPracticeTools.Core.Scripts.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OsuPracticeTools.Core.Scripts
{
    public abstract class ScriptWithSettings : Script
    {
        protected ScriptSettings Settings;
        protected static readonly Regex Regex = new(@"-([a-z]+) *([^-']*)");

        protected ScriptWithSettings(string script) : base(script)
        {
        }

        public virtual void ParseSettings()
        {
            var parts = ScriptString.Split(' ', 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                return;

            var settingsString = parts[1];

            var matches = Regex.Matches(settingsString);

            if (!matches.Any())
                return;

            Settings = new ScriptSettings { ScriptString = ScriptString };

            foreach (Match match in matches)
            {
                var arg = match.Groups[1].Value.ToLower();
                var param = match.Groups[2].Value.Trim();

                ScriptHelper.ScriptSettingsMatch(Settings, arg, param, settingsString);
            }

            if (Settings.NameFormat is null)
                ApplyDefaultNameFormat();
        }

        protected virtual void ApplyDefaultNameFormat()
        {
            Settings.NameFormat = "{v}";
            if (Settings.HardRock)
                Settings.NameFormat += "{HR}";

            if (Settings.FlipDirection != null)
                Settings.NameFormat += "{FLIP}";

            if (Settings.SpeedRate != 1 || Settings.BPM != null)
                Settings.NameFormat += "{R}{BPM}";

            if (Settings.DifficultyModified)
                Settings.NameFormat += "{CS}{AR}{OD}{HP}";

            if (Settings.RemoveSpinners)
                Settings.NameFormat += "{RS}";
        }

        protected virtual FileSection[] GetRequiredCloneSections(ScriptSettings settings = null)
        {
            settings ??= Settings;
            var requiredSections = new HashSet<FileSection>
            {
                FileSection.Metadata,
                FileSection.General
            };

            if (settings.HardRock || settings.SpeedRate != 1 || settings.FlipDirection != null || settings.RemoveSpinners)
            {
                requiredSections.Add(FileSection.HitObjects);

                if (settings.HardRock)
                    requiredSections.Add(FileSection.Difficulty);

                if (settings.SpeedRate != 1)
                {
                    requiredSections.Add(FileSection.Editor);
                    requiredSections.Add(FileSection.Events);
                    requiredSections.Add(FileSection.TimingPoints);
                }
            }

            if (settings.DifficultyModified)
                requiredSections.Add(FileSection.Difficulty);

            return requiredSections.ToArray();
        }

        public override Type Run()
        {
            if (Settings is null)
                ParseSettings();

            if (Settings?.UseGlobalSettings ?? false)
            { 
                ScriptHelper.CopySettings(Settings, Info.GlobalSettings);
                ApplyDefaultNameFormat();
            }

            return typeof(ScriptWithSettings);
        }
    }
}
