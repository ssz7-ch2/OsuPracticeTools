using OsuLightBeatmapParser;
using OsuLightBeatmapParser.Enums;
using OsuPracticeTools.Core.BeatmapHelpers;
using OsuPracticeTools.Core.PracticeDiffs;
using OsuPracticeTools.Core.Scripts.Helpers;
using OsuPracticeTools.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OsuPracticeTools.Core.Scripts.PracticeDiffScripts
{
    public class CreateDiffsScript : ScriptWithSettings
    {
        protected PracticeDiffSettings DiffSettings { get; set; }

        public CreateDiffsScript(string script) : base(script)
        {
        }

        public override void ParseSettings()
        {
            Settings = new ScriptSettings { ScriptString = ScriptString };
            DiffSettings = new PracticeDiffSettings();

            var settingsString = ScriptString.Split(' ', 2, StringSplitOptions.TrimEntries)[1];

            var matches = Regex.Matches(settingsString);

            foreach (Match match in matches)
            {
                var arg = match.Groups[1].Value.ToLower();
                var param = match.Groups[2].Value.Trim();

                ScriptHelper.ScriptSettingsMatch(Settings, arg, param, settingsString);

                switch (arg)
                {
                    case "i":
                        DiffSettings.PracticeDiffsType = PracticeDiffsType.Interval;
                        if (string.IsNullOrEmpty(param))
                        {
                            DiffSettings.Interval = 30;
                            DiffSettings.IntervalType = IntervalType.HitObjects;
                        }
                        else
                        {
                            var splitParam = param.Split('m');
                            if (splitParam.Length == 2)
                            {
                                DiffSettings.IntervalType = IntervalType.Measures;
                                DiffSettings.IntervalQuota = string.IsNullOrEmpty(splitParam[1]) ? 1 : int.Parse(splitParam[1]);
                            }

                            var splitInterval = splitParam[0].Split('x');
                            if (splitInterval.Length == 2)
                                DiffSettings.Interval = string.IsNullOrEmpty(splitInterval[1]) ? -1 : -int.Parse(splitInterval[1]);
                            else
                                DiffSettings.Interval = string.IsNullOrEmpty(splitParam[0]) ? -1 : int.Parse(splitParam[0]);
                        }
                        break;
                    case "b":
                        DiffSettings.PracticeDiffsType = PracticeDiffsType.Bookmarks;
                        var nameMatch = new Regex(@"-b +'([^']+)'").Match(settingsString.Replace('"', '\''));
                        if (nameMatch.Success)
                            DiffSettings.BookmarksDiffLoad = nameMatch.Groups[1].Value;
                        break;
                    case "c":
                        DiffSettings.ComboAmount = string.IsNullOrEmpty(param) ? null : int.Parse(param);
                        break;
                    case "order":
                        if (param == "time")
                            DiffSettings.IndexType = IndexFormatType.Time;
                        else if (param == "rev")
                            DiffSettings.IndexType = IndexFormatType.TimeReverse;
                        break;
                    case "cc":
                        DiffSettings.CirclesComboColor = true;
                        DiffSettings.SkinComboColors = string.IsNullOrEmpty(param) ? 4 : int.Parse(param);
                        break;
                    case "next":
                        DiffSettings.EndTimeType = EndTimeType.NextDiff;
                        DiffSettings.ExtendAmount = string.IsNullOrEmpty(param) ? 0 : int.Parse(param);
                        break;
                    case "spinner":
                        DiffSettings.ComboType = ComboType.Spinner;
                        break;
                    case "slider":
                        DiffSettings.ComboType = ComboType.Slider;
                        DiffSettings.SliderDuration = string.IsNullOrEmpty(param) ? 830 : int.Parse(param);
                        break;
                    case "gap":
                        DiffSettings.GapDuration = string.IsNullOrEmpty(param) ? 1500 : int.Parse(param);
                        break;
                    case "save":
                        nameMatch = new Regex(@"-save +'([^']+)'").Match(settingsString.Replace('"', '\''));
                        if (nameMatch.Success)
                            DiffSettings.BookmarksDiffSave = nameMatch.Groups[1].Value;
                        break;
                }
            }

            if (Settings.NameFormat is null)
                ApplyDefaultNameFormat();
        }

        protected override void ApplyDefaultNameFormat()
        {
            base.ApplyDefaultNameFormat();
            Settings.NameFormat += " ({i}/{n})";
        }

        protected override FileSection[] GetRequiredCloneSections(ScriptSettings settings = null)
        {
            var sections = base.GetRequiredCloneSections(settings).ToHashSet();
            sections.Add(FileSection.Events);
            sections.Add(FileSection.TimingPoints);
            sections.Add(FileSection.HitObjects);

            return sections.ToArray();
        }

        public Beatmap GetModifiedMap(Beatmap beatmap) => beatmap.ModifyMap(Settings, null, null, GetRequiredCloneSections());

        private class TimesEqualityComparer : IEqualityComparer<int[]>
        {
            public bool Equals(int[] x, int[] y)
            {
                if (x is null && y is null)
                    return true;
                if (x is null || y is null)
                    return false;
                return x.SequenceEqual(y);
            }

            public int GetHashCode(int[] obj)
            {
                return obj.GetHashCode();
            }
        }

        public override Type Run()
        {
            base.Run();

            Info.SameMapDuration = 0;

            var sections = GetRequiredCloneSections();

            Info.ParsedBeatmap ??= BeatmapDecoder.Decode(Info.CurrentBeatmapFile);
            Info.ParsedBeatmap.Metadata.Tags.Add(GlobalConstants.PROGRAM_TAG);

            var endTime = Info.ParsedBeatmap.HitObjects[^1].EndTime + 1;

            var times = DiffSettings.PracticeDiffsType switch
            {
                PracticeDiffsType.Interval => PracticeDiffExtensions.GetTimesFromInterval(
                    DiffSettings.Interval,
                    Info.ParsedBeatmap,
                    DiffSettings.IntervalType,
                    Info.CurrentOsuStatus == 1 ? Info.CurrentPlayTime : null,
                    DiffSettings.IntervalQuota),

                PracticeDiffsType.Bookmarks => PracticeDiffExtensions.GetTimesFromBookmarksDiff(
                    Info.CurrentBeatmapFile.Replace(Info.ParsedBeatmap.Metadata.Version, $"{Info.ParsedBeatmap.Metadata.Version} {DiffSettings.BookmarksDiffLoad}")),

                _ => Info.DiffTimes
            };

            if (!times.Any())
                return null;

            times = times.Distinct(new TimesEqualityComparer()).ToList();

            foreach (var time in times)
            {
                if (time[1] < 0)
                    time[1] = endTime;
            }

            var modifiedBeatmap = Info.ParsedBeatmap.ModifyMap(Settings, GlobalConstants.BEATMAP_TEMP, Info.BeatmapFolder, sections);

            if (Settings.SpeedRate != 1)
            {
                foreach (var time in times)
                {
                    time[0] = (int)(time[0] / Settings.SpeedRate);
                    time[1] = (int)(time[1] / Settings.SpeedRate) + 1;
                }
            }

            var diffs = CreateDiffs(times, modifiedBeatmap, GlobalConstants.BEATMAP_TEMP, Info.BeatmapFolder, Settings.Overwrite);

            if (!string.IsNullOrEmpty(DiffSettings.BookmarksDiffSave))
                PracticeDiffExtensions.CreateBookmarksDiffFromTimes(
                    diffs.ConvertAll(p => p.StartTime).ToArray(), Info.ParsedBeatmap, GlobalConstants.BEATMAP_TEMP, DiffSettings.BookmarksDiffSave, DiffSettings.BookmarksAdd);

            return typeof(CreateDiffsScript);
        }

        public List<PracticeDiff> CreateDiffs(List<int[]> times, Beatmap beatmap, string tempFolder, string beatmapFolder, bool overwrite)
        {
            var diffs = PracticeDiffExtensions.GetDiffsFromTimes(times, beatmap);
            diffs.CreateDiffs(DiffSettings, tempFolder, beatmapFolder, overwrite);

            return diffs;
        }
    }
}
