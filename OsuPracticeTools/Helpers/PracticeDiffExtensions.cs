using OsuLightBeatmapParser;
using OsuPracticeTools.Enums;
using OsuPracticeTools.Helpers.BeatmapHelpers;
using OsuPracticeTools.Objects;
using System.Collections.Generic;
using System.Linq;

namespace OsuPracticeTools.Helpers
{
    public static class PracticeDiffExtensions
    {
        public static void ReadjustEndTimes(this List<PracticeDiff> diffs, PracticeDiffSettings settings)
        {
            if (settings.EndTimeType != EndTimeType.NextDiff || !diffs.Any()) return;

            var reorder = diffs.OrderBy(p => p.StartTime).ToList();

            // reorder.Count - 1 since last diff already has correct end time
            for (int i = 0; i < reorder.Count - 1; i++)
                reorder[i].EndTime = reorder[i + 1].StartTime;
        }

        public static void RenameDiffs(this List<PracticeDiff> diffs, PracticeDiffSettings settings, double speedRate = 1)
        {
            if (!diffs.Any()) return;

            IEnumerable<PracticeDiff> reorder = settings.IndexType switch
            {
                IndexFormatType.Time => diffs.OrderBy(p => p.StartTime),
                IndexFormatType.TimeReverse => diffs.OrderByDescending(p => p.StartTime),
                _ => diffs
            };

            foreach (var (diff, index) in reorder.Select((value, index) => (value, index)))
            {
                diff.Index = index + 1;
                diff.FormatName(diffs, speedRate);
            }
        }

        public static void CreateDiffs(this List<PracticeDiff> diffs, ScriptSettings settings, string tempFolder, string beatmapFolder)
        {
            foreach (var practiceDiff in diffs)
            {
                practiceDiff.ApplySettings(settings.PracticeDiffSettings);
                practiceDiff.ModifyDifficulty(settings.CS, settings.AR, settings.OD, settings.HP, settings.MinCS, settings.MaxCS, settings.MinAR, settings.MaxAR, settings.MinOD, settings.MaxOD);
            }

            diffs.ReadjustEndTimes(settings.PracticeDiffSettings);
            diffs.RenameDiffs(settings.PracticeDiffSettings, settings.SpeedRate);
            diffs.ForEach(p => p.Save(tempFolder, beatmapFolder));
        }

        public static List<PracticeDiff> GetDiffsFromTimes(List<int[]> times, Beatmap beatmap)
        {
            var diffs = new List<PracticeDiff>();

            var last = beatmap.HitObjects.Last().StartTime;

            foreach (var time in times)
            {
                if (time[0] <= last)
                    diffs.Add(new PracticeDiff(beatmap, time[0], time[1]));
            }

            return diffs;
        }

        public static List<int[]> GetTimesFromInterval(int interval, Beatmap beatmap, IntervalType intervalType, int? startTime)
        {
            var times = new List<int[]>();
            var endTime = beatmap.HitObjects.Last().EndTime;

            switch (intervalType)
            {
                case IntervalType.HitObjects:
                    var startIndex = 0;
                    if (startTime != null)
                    {
                        for (int i = 0; i < beatmap.HitObjects.Count; i++)
                        {
                            if (beatmap.HitObjects[i].StartTime >= startTime)
                            { 
                                startIndex = i;
                                break;
                            }
                        }
                    }
                    for (int i = startIndex; i < beatmap.HitObjects.Count; i += interval)
                        times.Add(new[] { beatmap.HitObjects[i].StartTime, endTime + 1 });
                    break;
                case IntervalType.Measures:
                    var lastHitObjectStart = beatmap.HitObjects.Last().StartTime;
                    double currentTime = startTime ?? beatmap.TimingTickBefore(beatmap.HitObjects.First().StartTime, 1d / interval);

                    var uninheritedTimingPoints = beatmap.TimingPoints.Where(t => t.Uninherited).GetEnumerator();
                    uninheritedTimingPoints.MoveNext();
                    var currentBeatLength = uninheritedTimingPoints.Current.BeatLength;
                    var nextTimingPoint = uninheritedTimingPoints.MoveNext() ? uninheritedTimingPoints.Current : null;

                    int? prevHitObjectTime = null;
                    var timeAdded = false;

                    int time;
                    while (currentTime <= lastHitObjectStart)
                    {
                        time = (int)currentTime;

                        // only add time if it starts on different hit object
                        var currentHitObjectTime = beatmap.HitObjectAtOrAfter(time).StartTime;
                        if (currentHitObjectTime != prevHitObjectTime)
                        {
                            times.Add(new[] { time, endTime + 1 });
                            prevHitObjectTime = currentHitObjectTime;
                            timeAdded = true;
                        }
                        else
                            timeAdded = false;

                        var adjustedCurrentTime = currentTime;
                        while (nextTimingPoint != null && currentTime >= nextTimingPoint.Time)
                        {
                            adjustedCurrentTime = nextTimingPoint.Time;
                            if (currentTime != nextTimingPoint.Time && timeAdded)
                            {
                                times.RemoveAt(times.Count - 1);
                                times.Add(new[] { (int)adjustedCurrentTime, endTime + 1 });
                            }
                            currentBeatLength = nextTimingPoint.BeatLength;
                            nextTimingPoint = uninheritedTimingPoints.MoveNext() ? uninheritedTimingPoints.Current : null;
                        }
                        currentTime = adjustedCurrentTime;

                        currentTime += currentBeatLength * interval;
                    }

                    break;
            }
            
            return times;
        }
    }
}
