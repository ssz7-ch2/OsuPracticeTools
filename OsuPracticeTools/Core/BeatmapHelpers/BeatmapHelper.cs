using Microsoft.VisualBasic.FileIO;
using OsuLightBeatmapParser;
using OsuLightBeatmapParser.Enums;
using OsuPracticeTools.Helpers;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace OsuPracticeTools.Core.BeatmapHelpers
{
    public static class BeatmapHelper
    {
        public static string GetOriginalBeatmap(string beatmapFile, string beatmapFolder, string[] tags)
        {
            var beatmap = BeatmapDecoder.DecodeRead(beatmapFile, new[] { FileSection.Metadata });
            if (!beatmap.Metadata.Tags.Overlaps(tags))
                return beatmapFile;

            foreach (var file in Directory.GetFiles(beatmapFolder, "*.osu"))
            {
                if (!beatmap.Metadata.Version.Contains(file.Split('[').Last().Split(']')[0], StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var originalBeatmap = BeatmapDecoder.DecodeRead(file, new[] { FileSection.Metadata });
                if (!originalBeatmap.Metadata.Tags.Overlaps(tags) && beatmap.Metadata.BeatmapID == originalBeatmap.Metadata.BeatmapID)
                {
                    if (beatmap.Metadata.BeatmapID != 0 || beatmap.Metadata.Version.Contains(originalBeatmap.Metadata.Version))
                        return file;
                }
            }

            return beatmapFile;
        }

        public static void LoadBeatmapsWithOsz(string directory, string songsFolder)
        {
            var directories = new DirectoryInfo(directory).GetDirectories();
            if (!directories.Any())
                return;

            // only load one osz, the game will add all the other beatmaps automatically

            for (int i = 0; i < directories.Length - 1; i++)
                MoveBeatmapFiles(directories[i], Path.Combine(songsFolder, directories[i].Name));

            LoadBeatmapWithOsz(directories[^1].FullName, directories[^1].Name + ".osz", Path.Combine(songsFolder, directories[^1].Name));
        }

        private static void MoveBeatmapFiles(DirectoryInfo directory, string beatmapFolder)
        {
            if (!directory.EnumerateFiles().Any()) return;

            string deleteFolder = null;

            var cutAmount = ValidateBeatmapPath(directory, beatmapFolder);

            if (cutAmount > 0)
                deleteFolder = beatmapFolder;

            beatmapFolder = beatmapFolder[..^cutAmount];

            foreach (var file in directory.GetFiles())
                file.MoveTo(Path.Combine(beatmapFolder, file.Name), true);

            if (deleteFolder is not null)
                DeleteDirectoryAsync(deleteFolder);
        }

        public static void LoadBeatmapWithOsz(string directory, string outputOsz, string beatmapFolder)
        {
            if (!Directory.EnumerateFiles(directory).Any()) return;

            string deleteFolder = null;

            var cutAmount = ValidateBeatmapPath(new DirectoryInfo(directory), beatmapFolder);

            if (cutAmount > 0)
                deleteFolder = beatmapFolder;

            beatmapFolder = beatmapFolder[..^cutAmount];
            outputOsz = Path.GetFileNameWithoutExtension(outputOsz)[..^cutAmount] + ".osz";

            try
            {
                if (File.Exists(outputOsz))
                    File.Delete(outputOsz);


                ZipFile.CreateFromDirectory(directory, outputOsz);

                var proc = new Process
                {
                    StartInfo =
                    {
                        FileName = outputOsz,
                        UseShellExecute = true
                    }
                };
                try
                {
                    proc.Start();
                    proc.WaitForExit();
                }
                catch (Exception ex)
                {
                    if (ex is Win32Exception && ex.Message.Contains("associated"))
                    {
                        Logger.LogMessage(
                            "Error: .osz files have not been configured to open with osu!.exe on this system.");
                    }
                    else
                        Logger.LogError(ex);

                    foreach (var file in new DirectoryInfo(directory).GetFiles())
                        file.MoveTo(Path.Combine(beatmapFolder, file.Name), true);

                    File.Delete(outputOsz);
                }

                // need to wait for game to stop using audio file in order to delete it
                if (deleteFolder is not null)
                    DeleteDirectoryAsync(deleteFolder);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        // shorten osu file so that the game doesn't shorten the osz name and create a new folder
        private static int ValidateBeatmapPath(DirectoryInfo directory, string beatmapFolder)
        {
            var maxPathLength = directory.GetFiles()
                .Max(f => Path.Combine(beatmapFolder, f.Name).Length);


            // technically up to 260 is possible, but game cuts off folder name so that path is < 250
            if (maxPathLength < 250) return 0;

            var beatmapDirectory = new DirectoryInfo(beatmapFolder);

            if (maxPathLength - 249 < beatmapDirectory.Name.Length)
            {
                FileSystem.CopyDirectory(beatmapFolder, beatmapFolder[..^(maxPathLength - 249)]);

                // move osu files into osz so game is forced to update them with new directory
                foreach (var file in new DirectoryInfo(beatmapFolder).GetFiles("*.osu"))
                    file.MoveTo(Path.Combine(directory.FullName, file.Name));

                return maxPathLength - 249;
            }

            // in case the path is way too long
            foreach (var file in directory.GetFiles().Where(f => Path.Combine(beatmapFolder, f.Name).Length >= 250))
                file.MoveTo(Path.Combine(file.DirectoryName, file.Name[(Path.Combine(beatmapFolder, file.Name).Length - 249)..]));

            return 0;
        }

        private static async void DeleteDirectoryAsync(string directory)
        {
            while (true)
            {
                try
                {
                    Directory.Delete(directory, true);
                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(500);
                }
            }
        }
    }
}
