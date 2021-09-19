using OsuLightBeatmapParser;
using OsuLightBeatmapParser.Enums;
using OsuLightBeatmapParser.Objects;
using OsuPracticeTools.Core.BeatmapHelpers;
using OsuPracticeTools.Enums;
using OsuPracticeTools.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OsuPracticeTools.Core.PracticeDiffs
{
    public static class PracticeDiffExtensions
    {
        public static void ReadjustEndTimes(this List<PracticeDiff> diffs, PracticeDiffSettings settings)
        {
            if (settings.EndTimeType != EndTimeType.NextDiff || diffs.Count <= 1) return;

            var reorder = diffs.OrderBy(p => p.StartTime).ToList();

            // reorder.Count - 1 since last diff already has correct end time
            for (int i = 0; i < reorder.Count - (settings.ExtendAmount + 1); i++)
                reorder[i].EndTime = reorder[i + settings.ExtendAmount + 1].StartTime;
        }

        public static void RenameDiffs(this List<PracticeDiff> diffs, PracticeDiffSettings settings)
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
                diff.Index ??= index + 1;
                diff.Total ??= diffs.Count;
                diff.FormatName();
            }
        }

        public static void CreateDiffs(this List<PracticeDiff> diffs, PracticeDiffSettings diffSettings, string tempFolder, string beatmapFolder, bool overwrite)
        {
            foreach (var practiceDiff in diffs)
                practiceDiff.ApplySettings(diffSettings);

            diffs.ReadjustEndTimes(diffSettings);
            diffs.RenameDiffs(diffSettings);
            diffs.ForEach(p => p.Save(tempFolder, beatmapFolder, overwrite));
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

        public static List<int[]> GetTimesFromBookmarksDiff(string bookmarksDiffPath)
        {
            var beatmap = BeatmapDecoder.DecodeRead(bookmarksDiffPath, new[] { FileSection.Editor });
            return beatmap.Editor.Bookmarks.Select(b => new[] { b, -1 }).ToList();
        }

        public static Beatmap GetBookmarksDiff(Beatmap originalBeatmap, string beatmapFolder, out string path)
        {
            path = null;
            var maxSimilarity = int.MinValue;
            foreach (var file in Directory.GetFiles(beatmapFolder, "*.osu"))
            {
                var beatmap = BeatmapDecoder.DecodeRead(file, new[] { FileSection.Metadata });
                if (beatmap.Metadata.Tags.Contains(GlobalConstants.BOOKMARKS_TAG))
                {
                    var similarity = beatmap.Metadata.Version.Similarity(originalBeatmap.Metadata.Version);
                    if (similarity > maxSimilarity && beatmap.Metadata.BeatmapID == originalBeatmap.Metadata.BeatmapID)
                    {
                        path = file;
                        maxSimilarity = similarity;
                    }
                }
            }

            if (path != null)
                return BeatmapDecoder.Decode(path, new[] { FileSection.Editor });

            return null;
        }

        public static void CreateBookmarksDiffFromTimes(int[] times, Beatmap beatmap, string tempFolder, string bookmarksDiffName, bool bookmarksAdd)
        {
            var bookmarksDiff = beatmap.Clone(new[] { FileSection.Editor, FileSection.Metadata });

            if (bookmarksAdd)
                bookmarksDiff.Editor.Bookmarks = GetBookmarksDiff(beatmap, Info.BeatmapFolder, out _)?.Editor.Bookmarks.Concat(times).ToArray() ?? times;
            else
                bookmarksDiff.Editor.Bookmarks = times;

            bookmarksDiff.Metadata.Version += $" {bookmarksDiffName}";
            bookmarksDiff.Metadata.Tags.Add(GlobalConstants.BOOKMARKS_TAG);
            bookmarksDiff.Save(tempFolder);
        }

        public static List<int[]> GetTimesFromInterval(int interval, Beatmap beatmap, IntervalType intervalType, double? startTime, int objectQuota = 0)
        {
            var times = new List<int[]>();

            switch (intervalType)
            {
                case IntervalType.HitObjects:

                    var endTime = beatmap.HitObjects.Last().EndTime;
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
                    return GetTimesFromMeasures(interval, beatmap, startTime, objectQuota);
            }

            return times;
        }

        private static List<int[]> GetTimesFromMeasures(int interval, Beatmap beatmap, double? startTime, int objectQuota)
        {
            var times = new List<int[]>();

            var lastHitObject = beatmap.HitObjects.Last();
            var endTime = lastHitObject.EndTime;

            var uninheritedTimingPoints = beatmap.TimingPoints.Where(t => t.Uninherited).GetEnumerator();
            uninheritedTimingPoints.MoveNext();

            var breaks = beatmap.Events.Breaks.GetEnumerator();
            breaks.MoveNext();

            var currentBeatLength = uninheritedTimingPoints.Current.BeatLength;
            var currentMeter = uninheritedTimingPoints.Current.Meter;
            var nextTimingPoint = uninheritedTimingPoints.MoveNext() ? uninheritedTimingPoints.Current : null;


            startTime ??= beatmap.TimingTickBefore(beatmap.HitObjects.First().StartTime, 1d / (interval < 0 ? currentMeter * -interval : interval));
            var currentTime = (double)startTime;

            var time = (int)currentTime;
            var nextTime = GetNextTime(currentTime, ref currentBeatLength, ref currentMeter, interval, nextTimingPoint, uninheritedTimingPoints, out nextTimingPoint);
            var quotaFilled = 0;

            foreach (var hitObject in beatmap.HitObjects)
            {
                if (hitObject.StartTime < startTime)
                    continue;

                if (hitObject.StartTime >= time && hitObject.StartTime < (int)nextTime && hitObject.StartTime < (breaks.Current?.EndTime ?? int.MaxValue))
                {
                    quotaFilled++;
                    continue;
                }

                if (quotaFilled >= objectQuota || hitObject == lastHitObject || hitObject.StartTime < (int)nextTime)
                {
                    time = (int)currentTime;
                    times.Add(new[] { time, endTime + 1 });

                    if (hitObject.StartTime < (int)nextTime)
                    {
                        currentTime = beatmap.TimingTickBefore(hitObject.StartTime, 1d / (interval < 0 ? currentMeter * -interval : interval));
                        breaks.MoveNext();
                    }
                    else
                        currentTime = nextTime;
                    nextTime = GetNextTime(currentTime, ref currentBeatLength, ref currentMeter, interval, nextTimingPoint, uninheritedTimingPoints, out nextTimingPoint);

                    quotaFilled = 1;
                }
                else
                {
                    nextTime = GetNextTime(nextTime, ref currentBeatLength, ref currentMeter, interval, nextTimingPoint, uninheritedTimingPoints, out nextTimingPoint);
                    quotaFilled++;
                }
            }

            uninheritedTimingPoints.Dispose();
            breaks.Dispose();

            return times;
        }

        private static double GetNextTime(double currentTime, ref double currentBeatLength, ref int currentMeter, int interval, TimingPoint nextTimingPoint, IEnumerator<TimingPoint> uninheritedTimingPoints, out TimingPoint updatedNextTimingPoint)
        {
            var nextTime = currentTime + currentBeatLength * (interval < 0 ? currentMeter * -interval : interval);
            updatedNextTimingPoint = nextTimingPoint;
            if (nextTimingPoint != null && nextTime >= nextTimingPoint.Time)
            {
                nextTime = nextTimingPoint.Time;
                currentBeatLength = nextTimingPoint.BeatLength;
                currentMeter = nextTimingPoint.Meter;
                updatedNextTimingPoint = uninheritedTimingPoints.MoveNext() ? uninheritedTimingPoints.Current : null;
            }
            return nextTime;
        }
    }
}
