using OsuLightBeatmapParser;
using OsuPracticeTools.Core.BeatmapHelpers;
using System;
using System.IO;

namespace OsuPracticeTools.Core.Scripts.BeatmapScripts
{
    public class CreateMapScript : ScriptWithSettings
    {
        public CreateMapScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            base.Run();

            Info.CurrentOsuFile = Path.GetFileName(Info.BeatmapFile);

            var sections = GetRequiredCloneSections();

            Info.ParsedBeatmap ??= BeatmapDecoder.Decode(Info.BeatmapFile);
            Info.ParsedBeatmap.Metadata.Tags.Add(GlobalConstants.PROGRAM_TAG);

            Info.ParsedBeatmap.CreateModifiedMap(Settings, GlobalConstants.BEATMAP_TEMP, Info.BeatmapFolder, sections);

            return typeof(CreateMapScript);
        }
    }
}
