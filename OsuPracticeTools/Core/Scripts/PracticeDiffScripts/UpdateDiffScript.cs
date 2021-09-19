using OsuLightBeatmapParser;
using OsuPracticeTools.Core.BeatmapHelpers;
using OsuPracticeTools.Core.PracticeDiffs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OsuPracticeTools.Core.Scripts.PracticeDiffScripts
{
    public class UpdateDiffScript : Script
    {
        public UpdateDiffScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            Info.ParsedBeatmap ??= BeatmapDecoder.Decode(Info.CurrentBeatmapFile);

            if (!Info.ParsedBeatmap.Metadata.Tags.Contains(GlobalConstants.PRACTICE_DIFF_TAG))
                return null;
            var originalBeatmapFile = BeatmapHelper.GetOriginalBeatmap(Info.CurrentBeatmapFile, Info.BeatmapFolder, new[] { GlobalConstants.PRACTICE_DIFF_TAG });
            if (Info.CurrentBeatmapFile == originalBeatmapFile)
                return null;

            if (Info.ParsedBeatmap.General.StartTime is null || Info.ParsedBeatmap.General.Script is null)
                return null;

            var originalBeatmap = BeatmapDecoder.Decode(originalBeatmapFile);

            var originalScript = new CreateDiffsScript(Info.ParsedBeatmap.General.Script);
            originalScript.ParseSettings();

            var modifiedBeatmap = originalScript.GetModifiedMap(originalBeatmap);
            modifiedBeatmap.CopyGeneralSectionExtra(Info.ParsedBeatmap);
            modifiedBeatmap.Metadata.Tags = modifiedBeatmap.Metadata.Tags.ToList().Concat(Info.ParsedBeatmap.Metadata.Tags).ToHashSet();

            File.Delete(Info.CurrentBeatmapFile);

            var modifiedDiff = originalScript.CreateDiffs(
                new List<int[]>
                {
                    new[] { Info.CurrentPlayTime, Info.ParsedBeatmap.HitObjects[^1].EndTime + 1 }
                },
                modifiedBeatmap, Info.BeatmapFolder, Info.BeatmapFolder, true).First();

            var bookmarksDiff = PracticeDiffExtensions.GetBookmarksDiff(Info.ParsedBeatmap, Info.BeatmapFolder, out var bookmarksPath);
            if (bookmarksDiff != null)
            {
                var index = Array.FindIndex(bookmarksDiff.Editor.Bookmarks, b => b == Info.ParsedBeatmap.General.StartTime);
                if (index >= 0)
                {
                    bookmarksDiff.Editor.Bookmarks[index] = modifiedDiff.StartTime;
                    bookmarksDiff.SaveToPath(bookmarksPath);
                }
            }

            Info.ParsedBeatmap = null;

            return typeof(UpdateDiffScript);
        }
    }
}
