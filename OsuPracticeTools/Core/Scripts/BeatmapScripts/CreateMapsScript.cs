using OsuLightBeatmapParser;
using OsuPracticeTools.Core.BeatmapHelpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace OsuPracticeTools.Core.Scripts.BeatmapScripts
{
    public class CreateMapsScript : ScriptWithSettings
    {
        public CreateMapsScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            base.Run();

            Info.LastMapAddedDuration = 0;

            if (!Info.BeatmapFiles.Any())
                return null;

            Parallel.ForEach(Info.BeatmapFiles, bmapFileSet =>
            {
                var beatmap = BeatmapDecoder.Decode(bmapFileSet.Key);
                beatmap.Metadata.Tags.Add(GlobalConstants.PROGRAM_TAG);

                Parallel.ForEach(bmapFileSet.Value, settings =>
                {
                    settings ??= Settings;
                    var sections = GetRequiredCloneSections(settings);

                    var bmapFolder = Path.GetDirectoryName(bmapFileSet.Key);
                    var tempFolder = Path.Combine(GlobalConstants.BEATMAPS_TEMP, new DirectoryInfo(bmapFolder).Name);
                    Directory.CreateDirectory(tempFolder);

                    beatmap.CreateModifiedMap(settings, tempFolder, bmapFolder, sections);
                });

            });

            return typeof(CreateMapScript);
        }
    }
}
