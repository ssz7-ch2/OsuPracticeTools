using OsuLightBeatmapParser;
using OsuPracticeTools.Enums;
using OsuPracticeTools.Objects;
using System.Collections.Generic;
using System.Linq;

namespace OsuPracticeTools.Helpers
{
    public static class PracticeDiffExtensions
    {
        public static void ReadjustEndTimes(this List<PracticeDiff> diffs, PracticeDiffOptions options)
        {
            if (options.EndTimeType != EndTimeType.NextDiff || !diffs.Any()) return;

            var reorder = diffs.OrderBy(p => p.StartTime).ToList();

            // reorder.Count - 1 since last diff already has correct end time
            for (int i = 0; i < reorder.Count - 1; i++)
                reorder[i].EndTime = reorder[i + 1].StartTime;
        }

        public static void RenameDiffs(this List<PracticeDiff> diffs, PracticeDiffOptions options, double speedRate = 1)
        {
            if (!diffs.Any()) return;

            IEnumerable<PracticeDiff> reorder = options.IndexType switch
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

        public static void CreateDiffs(this List<PracticeDiff> diffs, ScriptOptions options, string tempFolder, string beatmapFolder)
        {
            foreach (var practiceDiff in diffs)
            {
                practiceDiff.ApplyOptions(options.PracticeDiffOptions);
                practiceDiff.ModifyDifficulty(options.CS, options.AR, options.OD, options.HP, options.MinCS, options.MaxCS, options.MinAR, options.MaxAR, options.MinOD, options.MaxOD);
            }

            diffs.ReadjustEndTimes(options.PracticeDiffOptions);
            diffs.RenameDiffs(options.PracticeDiffOptions, options.SpeedRate);
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

        public static List<int[]> GetTimesFromInterval(int interval, Beatmap beatmap)
        {
            var times = new List<int[]>();
            var endTime = beatmap.HitObjects.Last().EndTime;

            for (int i = 0; i < beatmap.HitObjects.Count; i += interval)
                times.Add(new[] { beatmap.HitObjects[i].StartTime, endTime + 1 });
            

            return times;
        }
    }
}
