using OsuLightBeatmapParser;
using OsuLightBeatmapParser.Enums;
using OsuPracticeTools.Enums;
using OsuPracticeTools.Helpers;
using OsuPracticeTools.Helpers.BeatmapHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OsuPracticeTools.Objects
{
    public class Script
    {
        public static Beatmap ParsedBeatmap { get; set; }
        public static ScriptOptions GlobalOptions { get; set; } = new();
        private static readonly List<string> SortedBeatmapFiles = new();
        private static readonly Regex Regex = new(@"-([a-z]+) *([^-']*)");

        private ScriptOptions _options;
        private bool _delayedParse;
        public ScriptType ScriptType { get; set; } = ScriptType.CreateMap;
        public string ScriptString { get; }

        public Script(string script)
        {
            ScriptString = script;
            ParseOptions(script);
        }

        private void ParseOptions(string script, bool delayedParse = false)
        {
            var parts = script.Split(' ', 2, StringSplitOptions.TrimEntries);

            switch (parts[0])
            {
                case "adddiff":
                    ScriptType = ScriptType.AddDiff;
                    return;
                case "enddiff":
                    ScriptType = ScriptType.EndDiff;
                    return;
                case "deldiff":
                    ScriptType = ScriptType.DeleteDiff;
                    return;
                case "cleardiffs":
                    ScriptType = ScriptType.ClearDiffs;
                    return;
                case "add":
                    ScriptType = ScriptType.AddMap;
                    break;
                case "del":
                    ScriptType = ScriptType.DeleteMap;
                    return;
                case "clearmaps":
                    ScriptType = ScriptType.ClearMaps;
                    return;
                case "set":
                    ScriptType = ScriptType.Set;
                    _options = new ScriptOptions();
                    break;
                case "creatediffs":
                    ScriptType = ScriptType.CreateDiffs;
                    _options = new ScriptOptions();
                    break;
                case "create":
                    ScriptType = ScriptType.CreateMap;
                    _options = new ScriptOptions();
                    break;
                case "createmaps":
                    ScriptType = ScriptType.CreateMaps;
                    _options = new ScriptOptions();
                    break;
            }

            if (parts.Length < 2) return;

            var matches = Regex.Matches(parts[1]);
            string overrideNameFormat = null;

            if (matches.Any() && ScriptType == ScriptType.AddMap)
                _options = new ScriptOptions();

            foreach (Match match in matches)
            {
                var arg = match.Groups[1].Value.ToLower();
                var param = match.Groups[2].Value;

                switch (arg)
                {
                    case "g":
                        _delayedParse = true;
                        if (delayedParse)
                        {
                            ScriptHelper.CopyOptions(_options, GlobalOptions);
                            break;
                        }
                        return;
                        
                    case "r":
                        _options.SpeedRate = string.IsNullOrEmpty(param) ? 1 : double.Parse(param);
                        if (Math.Abs(_options.SpeedRate - 1) < 0.001 || _options.SpeedRate is < 0.1d or > 5d)
                            _options.SpeedRate = 1;
                        break;
                    case "bpm":
                        _options.BPM = string.IsNullOrEmpty(param) ? null : double.Parse(param);
                        break;
                    case "pitch":
                        _options.Pitch = true;
                        break;
                    case "hr":
                        _options.HardRock = true;
                        break;
                    case "flip":
                        _options.FlipDirection = string.IsNullOrEmpty(param) ? FlipDirection.Horizontal : FlipDirection.Vertical;
                        break;
                    case "rs":
                        _options.RemoveSpinners = true;
                        break;
                    case "cs":
                        _options.CS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "ar":
                        _options.AR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "od":
                        _options.OD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "hp":
                        _options.HP = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "maxcs":
                        _options.MaxCS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "mincs":
                        _options.MinCS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "maxar":
                        _options.MaxAR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "minar":
                        _options.MinAR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "maxod":
                        _options.MaxOD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "minod":
                        _options.MinOD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _options.DifficultyModified = true;
                        break;
                    case "f":
                        // too hard to get name format in same regex
                        var formatMatch = new Regex(@"-f +'(.+)'").Match(parts[1].Replace('"', '\''));
                        if (formatMatch.Success)
                        {
                            overrideNameFormat = formatMatch.Groups[1].Value;
                        }
                        break;

                }

                if (ScriptType != ScriptType.CreateDiffs && ScriptType != ScriptType.Set) continue;

                switch (arg)
                {
                    case "i":
                        _options.ScriptDiffsType = ScriptDiffsType.Interval;
                        _options.Interval = string.IsNullOrEmpty(param) ? 20 : int.Parse(param);
                        break;
                    case "order":
                        if (param == "time")
                            _options.PracticeDiffOptions.IndexType = IndexFormatType.Time;
                        else if (param == "reverse")
                            _options.PracticeDiffOptions.IndexType = IndexFormatType.TimeReverse;
                        break;
                    case "next":
                        _options.PracticeDiffOptions.EndTimeType = EndTimeType.NextDiff;
                        break;
                    case "spinner":
                        _options.PracticeDiffOptions.ComboType = ComboType.Spinner;
                        break;
                    case "slider":
                        _options.PracticeDiffOptions.ComboType = ComboType.Slider;
                        _options.PracticeDiffOptions.SliderDuration = string.IsNullOrEmpty(param) ? 830 : int.Parse(param);
                        break;
                    case "gap":
                        _options.PracticeDiffOptions.GapDuration = string.IsNullOrEmpty(param) ? 1500 : int.Parse(param);
                        break;
                }
            }

            if (_options != null)
                ApplyDefaultNameFormat(overrideNameFormat);
        }

        private void ApplyDefaultNameFormat(string overrideNameFormat = null)
        {
            var nameFormat = "{v}";
            if (_options.HardRock)
                nameFormat += _options.NameFormat[^1] == ' ' ? "HR" : " HR";

            if (_options.FlipDirection != null)
                nameFormat += _options.NameFormat[^1] == ' ' ? $"Flip{_options.FlipDirection.ToString()[0]}" : $" Flip{_options.FlipDirection.ToString()[0]}";

            if (_options.SpeedRate != 1 || _options.BPM != null)
                nameFormat += "{R}{BPM}";

            if (_options.DifficultyModified)
                nameFormat += "{CS}{AR}{OD}{HP}";

            if (_options.RemoveSpinners)
                nameFormat += _options.NameFormat[^1] == ' ' ? "No Spinners" : " No Spinners";

            if (ScriptType is ScriptType.CreateMap or ScriptType.CreateMaps or ScriptType.AddMap)
                _options.NameFormat = overrideNameFormat ?? nameFormat;
            else if (ScriptType == ScriptType.CreateDiffs)
            {
                nameFormat += " ({i}/{n})";
                _options.PracticeDiffOptions.NameFormat = overrideNameFormat ?? nameFormat;
            }
        }

        // to avoid changing the original beatmap
        private FileSection[] GetRequiredCloneSections()
        {
            var requiredSections = new HashSet<FileSection>
            {
                FileSection.Metadata
            };

            if (ScriptType == ScriptType.CreateDiffs)
            {
                requiredSections.Add(FileSection.Events);
                requiredSections.Add(FileSection.TimingPoints);
                requiredSections.Add(FileSection.HitObjects);
            }

            if (_options.HardRock || _options.SpeedRate != 1 || _options.FlipDirection != null || _options.RemoveSpinners)
            {
                requiredSections.Add(FileSection.HitObjects);

                if (_options.HardRock)
                    requiredSections.Add(FileSection.Difficulty);

                if (_options.SpeedRate != 1)
                {
                    requiredSections.Add(FileSection.General);
                    requiredSections.Add(FileSection.Editor);
                    requiredSections.Add(FileSection.Events);
                    requiredSections.Add(FileSection.TimingPoints);
                }
            }

            if (_options.DifficultyModified)
                requiredSections.Add(FileSection.Difficulty);

            return requiredSections.ToArray();
        }

        public int Run(string beatmapFile, string beatmapFolder, List<int[]> diffTimes, Dictionary<string, HashSet<ScriptOptions>> beatmapFiles, int currentPlayTime)
        {
            if (_delayedParse)
                ParseOptions(ScriptString, _delayedParse);
            
            
            var sections = Array.Empty<FileSection>();

            if (ScriptType is ScriptType.CreateDiffs or ScriptType.CreateMap or ScriptType.CreateMaps)
            {
                sections = GetRequiredCloneSections();

                if (ParsedBeatmap is null && ScriptType != ScriptType.CreateMaps)
                {
                    ParsedBeatmap = BeatmapDecoder.Decode(beatmapFile);
                    ParsedBeatmap.Metadata.Tags.Add("prTools");
                }
            }

            if (ScriptType is ScriptType.CreateMap or ScriptType.CreateDiffs)
            {
                if (_options.BPM is not null)
                    _options.SpeedRate = BPMToSpeedRate(ParsedBeatmap, (double)_options.BPM);

                if (_options.SpeedRate != 1 || _options.DifficultyModified)
                    ParsedBeatmap.Metadata.Tags.Add("osutrainer");
            }

            switch (ScriptType)
            {
                case ScriptType.Set:
                    ScriptHelper.CopyOptions(GlobalOptions, _options);
                    return (int)ScriptType.Set;

                case ScriptType.AddDiff:
                    diffTimes.Add(new[]{currentPlayTime, -1});
                    return (int)ScriptType.AddDiff;

                case ScriptType.EndDiff:
                    if (diffTimes.Any())
                        diffTimes[^1][1] = currentPlayTime;
                    else
                        return -1;
                    return (int)ScriptType.EndDiff;

                case ScriptType.DeleteDiff:
                    if (diffTimes.Any())
                        diffTimes.RemoveAt(diffTimes.Count - 1);
                    else
                        return -1;
                    return (int)ScriptType.DeleteDiff;

                case ScriptType.ClearDiffs:
                    diffTimes.Clear();
                    return (int)ScriptType.ClearDiffs;

                case ScriptType.AddMap:
                    if (!beatmapFiles.ContainsKey(beatmapFile))
                        beatmapFiles[beatmapFile] = new HashSet<ScriptOptions>();
                    beatmapFiles[beatmapFile].Add(_options);
                    if (!SortedBeatmapFiles.Contains(beatmapFile))
                        SortedBeatmapFiles.Add(beatmapFile);
                    return (int)ScriptType.AddMap;

                case ScriptType.DeleteMap:
                    if (beatmapFiles.Any() && SortedBeatmapFiles.Any())
                    {
                        // try to remove current beatmap, else remove last added
                        if (!beatmapFiles.Remove(beatmapFile))
                            SortedBeatmapFiles.RemoveAt(SortedBeatmapFiles.Count - 1);
                    }
                    else
                        return -1;
                    return (int)ScriptType.DeleteMap;

                case ScriptType.ClearMaps:
                    beatmapFiles.Clear();
                    return (int)ScriptType.ClearMaps;

                case ScriptType.CreateDiffs:
                    var endtime = ParsedBeatmap.HitObjects[^1].EndTime + 1;
                    foreach (var time in diffTimes)
                    {
                        if (time[1] < 0)
                            time[1] = endtime;
                    }

                    var times = diffTimes;

                    if (_options.ScriptDiffsType == ScriptDiffsType.Interval)
                        times = PracticeDiffExtensions.GetTimesFromInterval(_options.Interval, ParsedBeatmap);

                    if (!times.Any())
                        return -1;

                    var newBeatmap = ParsedBeatmap;

                    if (_options.HardRock || _options.SpeedRate != 1 || _options.FlipDirection != null || _options.RemoveSpinners)
                    {
                        newBeatmap = ParsedBeatmap.Clone(sections);

                        if (_options.RemoveSpinners)
                            newBeatmap.RemoveSpinners();

                        if (_options.HardRock)
                            newBeatmap.ApplyHR();

                        if (_options.FlipDirection != null)
                            newBeatmap.ApplyFlip((FlipDirection)_options.FlipDirection);

                        if (_options.SpeedRate != 1)
                        {
                            var adjustTiming = newBeatmap.ChangeSpeedRate(GlobalConstants.BEATMAP_TEMP, beatmapFolder, _options.SpeedRate, _options.Pitch, _options.AudioProcessor);
                            foreach (var time in times)
                            {
                                time[0] = (int) (time[0] / _options.SpeedRate) + adjustTiming;
                                time[1] = (int) Math.Ceiling(time[1] / _options.SpeedRate) + adjustTiming;
                            }
                        }
                    }

                    var diffs = PracticeDiffExtensions.GetDiffsFromTimes(times, newBeatmap);

                    diffs.CreateDiffs(_options, GlobalConstants.BEATMAP_TEMP, beatmapFolder);
                    return (int)ScriptType.CreateDiffs;

                case ScriptType.CreateMap:
                    Create(_options, ParsedBeatmap, GlobalConstants.BEATMAP_TEMP, beatmapFolder, sections);

                    return (int)ScriptType.CreateMap;

                case ScriptType.CreateMaps:
                    if (!beatmapFiles.Any())
                        return -1;
                    Parallel.ForEach(beatmapFiles, bmapFileSet =>
                    {
                        var beatmap = BeatmapDecoder.Decode(bmapFileSet.Key);
                        beatmap.Metadata.Tags.Add("prTools");

                        Parallel.ForEach(bmapFileSet.Value, options =>
                        {
                            options ??= _options;
                            if (options.BPM is not null)
                                options.SpeedRate = BPMToSpeedRate(beatmap, (double)options.BPM);

                            if (options.SpeedRate != 1 || _options.DifficultyModified)
                                beatmap.Metadata.Tags.Add("osutrainer");

                            var bmapFolder = Path.GetDirectoryName(bmapFileSet.Key);
                            var tempFolder = Path.Combine(GlobalConstants.BEATMAPS_TEMP, new DirectoryInfo(bmapFolder).Name);
                            Directory.CreateDirectory(tempFolder);

                            Create(options, beatmap, tempFolder, bmapFolder, sections);
                        });

                    });

                    return (int)ScriptType.CreateMaps;
            }

            return -1;
        }

        private double BPMToSpeedRate(Beatmap beatmap, double bpm)
        {
            var speedRate = bpm / beatmap.General.MainBPM;
            if (Math.Abs(speedRate - 1) < 0.001 || speedRate is < 0.1d or > 5d)
                speedRate = 1;

            return speedRate;
        }

        private void Create(ScriptOptions options, Beatmap originalBeatmap, string tempFolder, string beatmapFolder, FileSection[] sections)
        {
            var beatmap = originalBeatmap.Clone(sections);

            if (options.RemoveSpinners)
                beatmap.RemoveSpinners();

            if (options.HardRock)
                beatmap.ApplyHR();

            if (options.FlipDirection != null)
                beatmap.ApplyFlip((FlipDirection)options.FlipDirection);

            if (options.SpeedRate != 1)
                beatmap.ChangeSpeedRate(tempFolder, beatmapFolder, options.SpeedRate, options.Pitch, options.AudioProcessor);

            var newBeatmap = beatmap;
            if (options.DifficultyModified)
            {
                newBeatmap = beatmap.Clone(new[] { FileSection.Difficulty });
                newBeatmap.ModifyDifficulty(options.CS, options.AR, options.OD, options.HP,
                    options.MinCS, options.MaxCS, options.MinAR, options.MaxAR, options.MinOD, options.MaxOD);
            }

            newBeatmap.FormatName(beatmap, options.NameFormat, options.SpeedRate);

            // save to temp folder to be zipped to osz
            newBeatmap.Save(tempFolder, beatmapFolder, false, true);
        }
    }
}
