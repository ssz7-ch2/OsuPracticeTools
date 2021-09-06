using OsuLightBeatmapParser;
using OsuPracticeTools.Core.Scripts;
using System.Collections.Generic;

namespace OsuPracticeTools.Core
{
    // static class containing info to be used in program
    public static class Info
    {
        public static Beatmap ParsedBeatmap { get; set; }
        public static string BeatmapFile { get; set; }
        public static string CurrentBeatmapFile { get; set; }
        public static string BeatmapFolder { get; set; }
        public static List<int[]> DiffTimes { get; } = new();
        public static Dictionary<string, HashSet<ScriptSettings>> BeatmapFiles { get; } = new();
        public static string PreviousOsuFile { get; set; }
        public static string CurrentOsuFile { get; set; }
        public static int CurrentPlayTime { get; set; }
        public static int CurrentOsuStatus { get; set; }
        public static double SameMapDuration { get; set; } 
        public static double LastMapAddedDuration { get; set; }
        public static List<string> SortedBeatmapFiles { get; } = new();
        public static ScriptSettings GlobalSettings { get; set; } = new();


        public static void Update(int timeElapsed, string osuFile)
        {
            CurrentOsuFile = osuFile;

            LastMapAddedDuration += timeElapsed / 60000d;
            if (LastMapAddedDuration >= 10)
            {
                LastMapAddedDuration = 0;
                BeatmapFiles.Clear();
            }

            if (CurrentOsuFile == PreviousOsuFile)
            {
                SameMapDuration += timeElapsed / 60000d;
                if (SameMapDuration >= 10)
                {
                    SameMapDuration = 0;
                    ParsedBeatmap = null;
                    DiffTimes.Clear();
                }
            }
            else
            {
                SameMapDuration = 0;
                ParsedBeatmap = null;
                DiffTimes.Clear();
            }
        }
        public static void Clear()
        {
            ParsedBeatmap = null;

            BeatmapFile = null;
            CurrentBeatmapFile = null;
            CurrentOsuFile = null;
            PreviousOsuFile = null;

            BeatmapFolder = null;

            DiffTimes.Clear();
            BeatmapFiles.Clear();
            SortedBeatmapFiles.Clear();
        }
    }
}
