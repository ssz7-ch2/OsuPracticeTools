using OsuLightBeatmapParser;
using OsuLightBeatmapParser.Enums;
using OsuLightBeatmapParser.Helpers;
using OsuLightBeatmapParser.Objects;
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
        public static ScriptSettings GlobalSettings { get; set; } = new();
        private static readonly List<string> SortedBeatmapFiles = new();
        private static readonly Regex Regex = new(@"-([a-z]+) *([^-']*)");

        private ScriptSettings _settings;
        private bool _delayedParse;
        public ScriptType ScriptType { get; set; } = ScriptType.CreateMap;
        public string ScriptString { get; }

        public Script(string script)
        {
            ScriptString = script;

            ParseSettings(script);
            if (_settings != null)
                _settings.ScriptString = script;
        }

        private void ParseSettings(string script, bool delayedParse = false)
        {
            var parts = script.Split(' ', 2, StringSplitOptions.TrimEntries);

            switch (parts[0].ToLower())
            {
                case "reload":
                    ScriptType = ScriptType.Reload;
                    return;
                case "adddiff":
                    ScriptType = ScriptType.AddDiff;
                    return;
                case "enddiff":
                    ScriptType = ScriptType.EndDiff;
                    return;
                case "updatediff":
                    ScriptType = ScriptType.UpdateDiff;
                    return;
                case "updateenddiff":
                    ScriptType = ScriptType.UpdateEndDiff;
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
                    _settings = new ScriptSettings();
                    break;
                case "creatediffs":
                    ScriptType = ScriptType.CreateDiffs;
                    _settings = new ScriptSettings();
                    break;
                case "create":
                    ScriptType = ScriptType.CreateMap;
                    _settings = new ScriptSettings();
                    break;
                case "createmaps":
                    ScriptType = ScriptType.CreateMaps;
                    _settings = new ScriptSettings();
                    break;
            }

            if (parts.Length < 2) return;

            var matches = Regex.Matches(parts[1]);
            string overrideNameFormat = null;

            if (matches.Any() && ScriptType == ScriptType.AddMap)
                _settings = new ScriptSettings();

            foreach (Match match in matches)
            {
                var arg = match.Groups[1].Value.ToLower();
                var param = match.Groups[2].Value.Trim();

                switch (arg)
                {
                    case "g":
                        _delayedParse = true;
                        if (delayedParse)
                        {
                            ScriptHelper.CopySettings(_settings, GlobalSettings);
                            break;
                        }
                        return;
                        
                    case "r":
                        _settings.SpeedRate = string.IsNullOrEmpty(param) ? 1 : double.Parse(param);
                        if (Math.Abs(_settings.SpeedRate - 1) < 0.001 || _settings.SpeedRate is < 0.1d or > 5d)
                            _settings.SpeedRate = 1;
                        break;
                    case "bpm":
                        _settings.BPM = string.IsNullOrEmpty(param) ? null : double.Parse(param);
                        break;
                    case "pitch":
                        _settings.Pitch = true;
                        break;
                    case "hr":
                        _settings.HardRock = true;
                        break;
                    case "flip":
                        _settings.FlipDirection = string.IsNullOrEmpty(param) ? FlipDirection.Horizontal : FlipDirection.Vertical;
                        break;
                    case "rs":
                        _settings.RemoveSpinners = true;
                        break;
                    case "cs":
                        _settings.CS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "ar":
                        _settings.AR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "od":
                        _settings.OD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "hp":
                        _settings.HP = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "maxcs":
                        _settings.MaxCS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "mincs":
                        _settings.MinCS = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "maxar":
                        _settings.MaxAR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "minar":
                        _settings.MinAR = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "maxod":
                        _settings.MaxOD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
                        break;
                    case "minod":
                        _settings.MinOD = string.IsNullOrEmpty(param) ? null : float.Parse(param);
                        _settings.DifficultyModified = true;
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
                        _settings.ScriptDiffsType = ScriptDiffsType.Interval;
                        if (string.IsNullOrEmpty(param))
                        {
                            _settings.Interval = 30;
                            _settings.IntervalType = IntervalType.HitObjects;
                        }
                        else
                        {
                            var splitParam = param.Split('m');
                            if (splitParam.Length == 2)
                            {
                                _settings.IntervalType = IntervalType.Measures;
                                _settings.IntervalQuota = string.IsNullOrEmpty(splitParam[1]) ? 1 : int.Parse(splitParam[1]);
                            }

                            var splitInterval = splitParam[0].Split('x');
                            if (splitInterval.Length == 2)
                                _settings.Interval = string.IsNullOrEmpty(splitInterval[1]) ? -1 : -int.Parse(splitInterval[1]);
                            else
                                _settings.Interval = string.IsNullOrEmpty(splitParam[0]) ? -1 : int.Parse(splitParam[0]);
                        }
                        break;
                    case "order":
                        if (param == "time")
                            _settings.PracticeDiffSettings.IndexType = IndexFormatType.Time;
                        else if (param == "rev")
                            _settings.PracticeDiffSettings.IndexType = IndexFormatType.TimeReverse;
                        break;
                    case "cc":
                        _settings.PracticeDiffSettings.CirclesComboColor = true;
                        _settings.PracticeDiffSettings.SkinComboColors = string.IsNullOrEmpty(param) ? 4 : int.Parse(param);
                        break;
                    case "next":
                        _settings.PracticeDiffSettings.EndTimeType = EndTimeType.NextDiff;
                        _settings.PracticeDiffSettings.ExtendAmount = string.IsNullOrEmpty(param) ? 0 : int.Parse(param);
                        break;
                    case "spinner":
                        _settings.PracticeDiffSettings.ComboType = ComboType.Spinner;
                        break;
                    case "slider":
                        _settings.PracticeDiffSettings.ComboType = ComboType.Slider;
                        _settings.PracticeDiffSettings.SliderDuration = string.IsNullOrEmpty(param) ? 830 : int.Parse(param);
                        break;
                    case "gap":
                        _settings.PracticeDiffSettings.GapDuration = string.IsNullOrEmpty(param) ? 1500 : int.Parse(param);
                        break;
                }
            }

            if (_settings != null)
                ApplyDefaultNameFormat(overrideNameFormat);
        }

        private void ApplyDefaultNameFormat(string overrideNameFormat = null)
        {
            var nameFormat = "{v}";
            if (_settings.HardRock)
                nameFormat += _settings.NameFormat[^1] == ' ' ? "HR" : " HR";

            if (_settings.FlipDirection != null)
                nameFormat += _settings.NameFormat[^1] == ' ' ? $"Flip{_settings.FlipDirection.ToString()[0]}" : $" Flip{_settings.FlipDirection.ToString()[0]}";

            if (_settings.SpeedRate != 1 || _settings.BPM != null)
                nameFormat += "{R}{BPM}";

            if (_settings.DifficultyModified)
                nameFormat += "{CS}{AR}{OD}{HP}";

            if (_settings.RemoveSpinners)
                nameFormat += _settings.NameFormat[^1] == ' ' ? "No Spinners" : " No Spinners";

            if (ScriptType is ScriptType.CreateMap or ScriptType.CreateMaps or ScriptType.AddMap)
                _settings.NameFormat = overrideNameFormat ?? nameFormat;
            else if (ScriptType == ScriptType.CreateDiffs)
            {
                nameFormat += " ({i}/{n})";
                _settings.PracticeDiffSettings.NameFormat = overrideNameFormat ?? nameFormat;
            }
        }

        // to avoid changing the original beatmap
        private FileSection[] GetRequiredCloneSections()
        {
            var requiredSections = new HashSet<FileSection>
            {
                FileSection.Metadata,
                FileSection.General
            };

            if (ScriptType is ScriptType.CreateDiffs or ScriptType.UpdateDiff)
            {
                requiredSections.Add(FileSection.Events);
                requiredSections.Add(FileSection.TimingPoints);
                requiredSections.Add(FileSection.HitObjects);
            }

            if (_settings.HardRock || _settings.SpeedRate != 1 || _settings.FlipDirection != null || _settings.RemoveSpinners)
            {
                requiredSections.Add(FileSection.HitObjects);

                if (_settings.HardRock)
                    requiredSections.Add(FileSection.Difficulty);

                if (_settings.SpeedRate != 1)
                {
                    requiredSections.Add(FileSection.Editor);
                    requiredSections.Add(FileSection.Events);
                    requiredSections.Add(FileSection.TimingPoints);
                }
            }

            if (_settings.DifficultyModified)
                requiredSections.Add(FileSection.Difficulty);

            return requiredSections.ToArray();
        }

        public int Run(string beatmapFile, string beatmapFolder, List<int[]> diffTimes, Dictionary<string, HashSet<ScriptSettings>> beatmapFiles, int currentPlayTime, int osuStatus = 0)
        {
            if (_delayedParse)
                ParseSettings(ScriptString, _delayedParse);
            
            
            var sections = Array.Empty<FileSection>();

            if (ScriptType is ScriptType.CreateDiffs or ScriptType.CreateMap or ScriptType.CreateMaps or ScriptType.UpdateDiff or ScriptType.UpdateEndDiff)
            {
                if (ScriptType != ScriptType.UpdateDiff && ScriptType != ScriptType.UpdateEndDiff)
                    sections = GetRequiredCloneSections();

                if (ParsedBeatmap is null && ScriptType != ScriptType.CreateMaps)
                {
                    ParsedBeatmap = BeatmapDecoder.Decode(beatmapFile);
                    ParsedBeatmap.Metadata.Tags.Add("prTools");
                }
            }

            if (ScriptType is ScriptType.CreateMap or ScriptType.CreateDiffs)
            {
                if (_settings.BPM is not null)
                    _settings.SpeedRate = BPMToSpeedRate(ParsedBeatmap, (double)_settings.BPM);

                if (_settings.SpeedRate != 1 || _settings.DifficultyModified)
                    ParsedBeatmap.Metadata.Tags.Add("osutrainer");
            }

            switch (ScriptType)
            {
                case ScriptType.Reload:
                    Program.ReloadHotkeys(null, EventArgs.Empty);
                    return (int)ScriptType.Reload;

                case ScriptType.Set:
                    ScriptHelper.CopySettings(GlobalSettings, _settings);
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

                case ScriptType.UpdateDiff:
                    if (!ParsedBeatmap.Metadata.Tags.Contains("pdiffmaker"))
                        return -1;
                    var originalBeatmapFile = BeatmapHelper.GetOriginalBeatmap(beatmapFile, beatmapFolder);
                    if (beatmapFile == originalBeatmapFile)
                        return -1;

                    var oldStartTime = ParsedBeatmap.General.StartTime;

                    var originalBeatmap = BeatmapDecoder.Decode(originalBeatmapFile);
                    originalBeatmap.General.Script = ParsedBeatmap.General.Script;
                    PracticeDiffSettings settings;

                    // try to figure out what settings were used
                    if (string.IsNullOrEmpty(ParsedBeatmap.General.Script))
                    {
                        settings = new PracticeDiffSettings();

                        // not really possible to get nameformat from version, so keep version the same
                        settings.NameFormat = ParsedBeatmap.Metadata.Version;

                        if (oldStartTime is null)
                        {
                            foreach (var hitObject in ParsedBeatmap.HitObjects)
                            {
                                if (ParsedBeatmap.TimingPointAt(hitObject.StartTime).Volume != 5)
                                {
                                    oldStartTime = hitObject.StartTime;
                                    break;
                                }
                            }
                        }

                        var comboObjects = ParsedBeatmap.HitObjects.Where(h => h.StartTime < oldStartTime && h is not HitCircle).ToList();
                        if (comboObjects.Count > 0)
                        {
                            settings.GapDuration = (int)oldStartTime - comboObjects.Last().EndTime;
                            if (comboObjects.OfType<Slider>().Any())
                            {
                                var slider = comboObjects.First(h => h is Slider) as Slider;
                                settings.ComboAmount = slider.Slides + 1;
                                settings.ComboType = ComboType.Slider;
                                settings.SliderDuration = slider.EndTime - slider.StartTime;
                            }
                            else
                            {
                                settings.ComboAmount = comboObjects.OfType<Spinner>().Count();
                                settings.ComboType = ComboType.Spinner;
                            }
                        }
                    }
                    else
                        settings = new Script(ParsedBeatmap.General.Script)._settings.PracticeDiffSettings;

                    var practiceDiff = new PracticeDiff(originalBeatmap, currentPlayTime, ParsedBeatmap.HitObjects[^1].EndTime + 1);
                    practiceDiff.ApplySettings(settings);
                    practiceDiff.FormatName(settings.NameFormat);

                    // unable to get some information, so keep version the same
                    if (!settings.NameFormat.Contains("{s}") && !settings.NameFormat.Contains("{sc}"))
                        practiceDiff.Name = ParsedBeatmap.Metadata.Version;

                    // replace file
                    File.Move(beatmapFile, Path.Combine(beatmapFolder, practiceDiff.FileName));

                    practiceDiff.Save(beatmapFolder, beatmapFolder, true);
                    return (int)ScriptType.UpdateDiff;

                case ScriptType.UpdateEndDiff:
                    if (!ParsedBeatmap.Metadata.Tags.Contains("pdiffmaker"))
                        return -1;

                    var oldEndTime = ParsedBeatmap.HitObjects[^1].EndTime;
                    if (currentPlayTime == oldEndTime)
                        return (int)ScriptType.UpdateEndDiff;

                    if (currentPlayTime > oldEndTime)
                    {
                        originalBeatmapFile = BeatmapHelper.GetOriginalBeatmap(beatmapFile, beatmapFolder);
                        if (beatmapFile == originalBeatmapFile)
                            return -1;

                        originalBeatmap = BeatmapDecoder.Decode(originalBeatmapFile);
                        ParsedBeatmap.HitObjects.AddRange(originalBeatmap.HitObjects.Where(h => h.EndTime > oldEndTime && h.EndTime < currentPlayTime));
                    }
                    else
                        ParsedBeatmap.HitObjects.RemoveAll(h => h.EndTime >= currentPlayTime);

                    if (ParsedBeatmap.General.Script.Contains("{e}") || ParsedBeatmap.General.Script.Contains("{ec}"))
                    {
                        // have to create temp practice diff just to rename :(
                        settings = new Script(ParsedBeatmap.General.Script)._settings.PracticeDiffSettings;
                        practiceDiff = new PracticeDiff(ParsedBeatmap, ParsedBeatmap.General.StartTime ?? ParsedBeatmap.HitObjects[0].StartTime, currentPlayTime);
                        practiceDiff.ApplySettings(settings);
                        practiceDiff.FormatName(settings.NameFormat);

                        ParsedBeatmap.Metadata.Version = practiceDiff.Name;
                    }
                    else
                        ParsedBeatmap.Save(beatmapFolder);

                    return (int)ScriptType.UpdateEndDiff;

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
                        beatmapFiles[beatmapFile] = new HashSet<ScriptSettings>();
                    beatmapFiles[beatmapFile].Add(_settings);
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

                    if (_settings.ScriptDiffsType == ScriptDiffsType.Interval)
                        times = PracticeDiffExtensions.GetTimesFromInterval(_settings.Interval, ParsedBeatmap, _settings.IntervalType, osuStatus == 1 ? currentPlayTime : null, _settings.IntervalQuota);

                    if (!times.Any())
                        return -1;

                    var newBeatmap = ParsedBeatmap.Clone(sections);
                    newBeatmap.General.Script = ScriptString;

                    if (_settings.HardRock || _settings.SpeedRate != 1 || _settings.FlipDirection != null || _settings.RemoveSpinners)
                    {
                        if (_settings.RemoveSpinners)
                            newBeatmap.RemoveSpinners();

                        if (_settings.HardRock)
                            newBeatmap.ApplyHR();

                        if (_settings.FlipDirection != null)
                            newBeatmap.ApplyFlip((FlipDirection)_settings.FlipDirection);

                        if (_settings.SpeedRate != 1)
                        {
                            newBeatmap.ChangeSpeedRate(GlobalConstants.BEATMAP_TEMP, beatmapFolder, _settings.SpeedRate, _settings.Pitch);
                            foreach (var time in times)
                            {
                                time[0] = (int) (time[0] / _settings.SpeedRate);
                                time[1] = (int) Math.Ceiling(time[1] / _settings.SpeedRate);
                            }
                        }
                    }

                    var diffs = PracticeDiffExtensions.GetDiffsFromTimes(times, newBeatmap);

                    diffs.CreateDiffs(_settings, GlobalConstants.BEATMAP_TEMP, beatmapFolder);
                    return (int)ScriptType.CreateDiffs;

                case ScriptType.CreateMap:
                    Create(_settings, ParsedBeatmap, GlobalConstants.BEATMAP_TEMP, beatmapFolder, sections);

                    return (int)ScriptType.CreateMap;

                case ScriptType.CreateMaps:
                    if (!beatmapFiles.Any())
                        return -1;
                    Parallel.ForEach(beatmapFiles, bmapFileSet =>
                    {
                        var beatmap = BeatmapDecoder.Decode(bmapFileSet.Key);
                        beatmap.Metadata.Tags.Add("prTools");

                        Parallel.ForEach(bmapFileSet.Value, settings =>
                        {
                            settings ??= _settings;
                            if (settings.BPM is not null)
                                settings.SpeedRate = BPMToSpeedRate(beatmap, (double)settings.BPM);

                            if (settings.SpeedRate != 1 || _settings.DifficultyModified)
                                beatmap.Metadata.Tags.Add("osutrainer");

                            var bmapFolder = Path.GetDirectoryName(bmapFileSet.Key);
                            var tempFolder = Path.Combine(GlobalConstants.BEATMAPS_TEMP, new DirectoryInfo(bmapFolder).Name);
                            Directory.CreateDirectory(tempFolder);

                            Create(settings, beatmap, tempFolder, bmapFolder, sections);
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

        private void Create(ScriptSettings settings, Beatmap originalBeatmap, string tempFolder, string beatmapFolder, FileSection[] sections)
        {
            var beatmap = originalBeatmap.Clone(sections);
            beatmap.General.Script = settings.ScriptString;

            if (settings.RemoveSpinners)
                beatmap.RemoveSpinners();

            if (settings.HardRock)
                beatmap.ApplyHR();

            if (settings.FlipDirection != null)
                beatmap.ApplyFlip((FlipDirection)settings.FlipDirection);

            if (settings.SpeedRate != 1)
                beatmap.ChangeSpeedRate(tempFolder, beatmapFolder, settings.SpeedRate, settings.Pitch);

            var newBeatmap = beatmap;
            if (settings.DifficultyModified)
            {
                newBeatmap = beatmap.Clone(new[] { FileSection.Difficulty });
                newBeatmap.ModifyDifficulty(settings.CS, settings.AR, settings.OD, settings.HP,
                    settings.MinCS, settings.MaxCS, settings.MinAR, settings.MaxAR, settings.MinOD, settings.MaxOD);
            }

            newBeatmap.FormatName(beatmap, settings.NameFormat, settings.SpeedRate);

            // save to temp folder to be zipped to osz
            newBeatmap.Save(tempFolder, beatmapFolder, false, true);
        }
    }
}
