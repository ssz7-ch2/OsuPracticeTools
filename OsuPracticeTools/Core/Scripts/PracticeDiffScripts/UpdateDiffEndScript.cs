using OsuLightBeatmapParser;
using OsuPracticeTools.Core.BeatmapHelpers;
using System;
using System.Collections.Generic;
using System.IO;

namespace OsuPracticeTools.Core.Scripts.PracticeDiffScripts
{
    public class UpdateDiffEndScript : Script
    {
        public UpdateDiffEndScript(string script) : base(script)
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

            File.Delete(Info.CurrentBeatmapFile);

            originalScript.CreateDiffs(
                new List<int[]>
                {
                    new[] { (int)Info.ParsedBeatmap.General.StartTime, Info.CurrentPlayTime }
                },
                modifiedBeatmap, Info.BeatmapFolder, Info.BeatmapFolder, true);

            Info.ParsedBeatmap = null;

            return typeof(UpdateDiffEndScript);
        }
    }
}
