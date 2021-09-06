using System;
using System.Collections.Generic;

namespace OsuPracticeTools.Core.Scripts.BeatmapScripts
{
    public class AddMapScript : ScriptWithSettings
    {
        public AddMapScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            base.Run();

            Info.LastMapAddedDuration = 0;

            if (!Info.BeatmapFiles.ContainsKey(Info.BeatmapFile))
                Info.BeatmapFiles[Info.BeatmapFile] = new HashSet<ScriptSettings>();
            Info.BeatmapFiles[Info.BeatmapFile].Add(Settings);
            if (!Info.SortedBeatmapFiles.Contains(Info.BeatmapFile))
                Info.SortedBeatmapFiles.Add(Info.BeatmapFile);

            return typeof(AddMapScript);
        }
    }
}
