using System;

namespace OsuPracticeTools.Core.Scripts.BeatmapScripts
{
    public class ClearMapsScript : Script
    {
        public ClearMapsScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            Info.BeatmapFiles.Clear();
            Info.SortedBeatmapFiles.Clear();
            return typeof(ClearMapsScript);
        }
    }
}
