using System;
using System.Linq;

namespace OsuPracticeTools.Core.Scripts.BeatmapScripts
{
    public class DeleteMapScript : Script
    {
        public DeleteMapScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            if (Info.BeatmapFiles.Any() && Info.SortedBeatmapFiles.Any())
            {
                // try to remove current beatmap, else remove last added
                if (!Info.BeatmapFiles.Remove(Info.BeatmapFile))
                    Info.SortedBeatmapFiles.RemoveAt(Info.SortedBeatmapFiles.Count - 1);
            }
            else
                return null;

            return typeof(DeleteMapScript);
        }
    }
}
