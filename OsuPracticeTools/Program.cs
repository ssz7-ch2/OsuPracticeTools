﻿using OsuMemoryDataProvider;
using OsuPracticeTools.Enums;
using OsuPracticeTools.Helpers;
using OsuPracticeTools.Helpers.BeatmapHelpers;
using OsuPracticeTools.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Un4seen.Bass;
using Keys = System.Windows.Forms.Keys;
using Timer = System.Windows.Forms.Timer;

namespace OsuPracticeTools
{
    internal class Program
    {
        private static IOsuMemoryReader _osuReader;
        private static string _songsFolder;
        private static bool _prevGameState;
        private static string _prevOsuFile;
        private static bool _gameRunning;
        private static Timer _timer;
        private static double _gameClosedDuration; // in minutes
        private static double _sameMapDuration; // in minutes
        private static double _lastMapAddedDuration;
        private static bool _hotkeysLoaded;
        private static Mutex _mutex;
        private static readonly List<int[]> Diffs = new();
        private static readonly Dictionary<string, HashSet<ScriptOptions>> BeatmapFiles = new();
        private static readonly Dictionary<List<Keys>, List<Script>> KeyScriptDictionary = new();
        private static readonly SoundPlayer ScriptStart = new("Resources/scriptStart.wav");
        private static readonly SoundPlayer ScriptFinish = new("Resources/scriptFinish.wav");
        private static readonly SoundPlayer ScriptError = new("Resources/scriptError.wav");
        internal static Keys[] StatKeys = { Keys.X, Keys.C, Keys.V };
        internal static Keys RateKey = Keys.Z;
        internal static List<Keys> ResetGlobalKey = new() {Keys.Z, Keys.X};

        private static void Main()
        {
            _mutex = new Mutex(true, "osu Practice Tools Singleton", out var createdNew);
            if (!createdNew)
            {
                Logger.LogMessage("OsuPracticeTools is already running");
                return;
            }

            _songsFolder = Properties.Settings.Default.SongsFolder;
            Application.ApplicationExit += OnExit;

            _osuReader = OsuMemoryReader.Instance.GetInstanceForWindowTitleHint("");
            _timer = new Timer {Interval = 20000};
            _timer.Tick += CheckGameRunning;
            _timer.Start();

            InitialCheckGameRunning();

            Application.Run(new SystemTray());
        }

        private static void OnExit(object sender, EventArgs e)
        {
            Bass.BASS_Free();
            _timer.Dispose();
            GlobalKeyboardHook.Unhook();
            ScriptStart.Dispose();
            ScriptFinish.Dispose();
            ScriptError.Dispose();
        }

        private static void CheckGameRunning(object sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName("osu!");

            if (processes.Length == 0)
            {
                _gameRunning = false;
                GameNotRunning(_timer.Interval);
            }
            else
            {
                _gameRunning = true;
                GameRunning(processes, _timer.Interval);
            }

            _prevGameState = _gameRunning;
        }

        private static void InitialCheckGameRunning()
        {
            var processes = Process.GetProcessesByName("osu!");

            if (processes.Length == 0)
            {
                _gameRunning = false;
                GameNotRunning(0);
            }
            else
            {
                _gameRunning = true;
                GameRunning(processes, 0);
            }

            _prevGameState = _gameRunning;
        }

        private static void GameNotRunning(int timeElapsed)
        {
            _gameClosedDuration += timeElapsed / 60000d;

            if (_gameClosedDuration >= 20 || _prevGameState != _gameRunning)
            {
                Logger.LogMessage("Game closed, unloading hotkeys");
                Bass.BASS_Free();
                GlobalKeyboardHook.Unhook();
                UnloadHotkeys();
                Script.ParsedBeatmap = null;
                _prevOsuFile = null;
                Diffs.Clear();
            }
        }

        private static void GameRunning(Process[] processes, int timeElapsed)
        {
            _gameClosedDuration = 0;

            if (_prevGameState != _gameRunning)
            {
                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                GlobalKeyboardHook.Hook();
                LoadHotkeys();
            }

            if (string.IsNullOrEmpty(_songsFolder))
                GetSongsFolder(processes);
            

            var currentOsuFile = _osuReader.GetOsuFileName();

            UpdateVariables(currentOsuFile, timeElapsed);

            _prevOsuFile = currentOsuFile;
        }

        private static void UpdateVariables(string currentOsuFile, int timeElapsed)
        {
            _lastMapAddedDuration += timeElapsed / 60000d;
            if (_lastMapAddedDuration >= 10)
            {
                _lastMapAddedDuration = 0;
                BeatmapFiles.Clear();
            }

            if (currentOsuFile == _prevOsuFile)
            {
                _sameMapDuration += timeElapsed / 60000d;
                if (_sameMapDuration >= 10)
                {
                    _sameMapDuration = 0;
                    Script.ParsedBeatmap = null;
                    Diffs.Clear();
                }
            }
            else
            {
                _sameMapDuration = 0;
                Script.ParsedBeatmap = null;
                Diffs.Clear();
            }
        }

        private static void GetSongsFolder(Process[] processes)
        {
            try
            {
                var osuExePath = processes[0].MainModule.FileName;
                var songsFolder = Path.Combine(Path.GetDirectoryName(osuExePath), "Songs");
                if (Directory.Exists(songsFolder))
                {
                    _songsFolder = songsFolder;
                    Properties.Settings.Default.SongsFolder = _songsFolder;
                    Properties.Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private static void LoadExtraHotkeys()
        {
            // reset global options
            GlobalKeyboardHook.HookedDownKeys.AddUnique(ResetGlobalKey);

            foreach (var statKey in StatKeys)
            {
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey, Keys.OemMinus });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey, Keys.Oemplus });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey, Keys.Delete });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey | Keys.Shift, Keys.OemMinus | Keys.Shift });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey | Keys.Shift, Keys.Oemplus | Keys.Shift });
            }

            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey, Keys.OemMinus });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey, Keys.Oemplus });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey, Keys.Delete });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey | Keys.Shift, Keys.OemMinus | Keys.Shift });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey | Keys.Shift, Keys.Oemplus | Keys.Shift });
        }

        private static void LoadHotkeys()
        {
            if (!_hotkeysLoaded)
            {
                ScriptHelper.ParseScripts(KeyScriptDictionary);
                GlobalKeyboardHook.HookedUpKeys.AddRange(KeyScriptDictionary.Keys);

                LoadExtraHotkeys();

                GlobalKeyboardHook.KeyUp += OnGlobalKeyUp;
                GlobalKeyboardHook.KeyDown += OnGlobalKeyDown;

                _hotkeysLoaded = true;
            }
        }

        private static void UnloadHotkeys()
        {
            if (_hotkeysLoaded)
            {
                KeyScriptDictionary.Clear();
                GlobalKeyboardHook.HookedUpKeys.Clear();
                GlobalKeyboardHook.HookedDownKeys.Clear();
                GlobalKeyboardHook.KeyUp -= OnGlobalKeyUp;
                GlobalKeyboardHook.KeyDown -= OnGlobalKeyDown;

                _hotkeysLoaded = false;
            }
        }

        internal static void ReloadHotkeys(object sender, EventArgs e)
        {
            if (_gameRunning)
            {
                UnloadHotkeys();
                LoadHotkeys();
            }
        }

        private static void RunScripts(List<Keys> multiKey, string beatmapFile, string beatmapFolder)
        {
            var currentTime = _osuReader.ReadPlayTime();

            try
            {
                Directory.CreateDirectory(GlobalConstants.BEATMAP_TEMP);
                Directory.CreateDirectory(GlobalConstants.BEATMAPS_TEMP);


                var messages = new List<string>();
                var scriptTypes = new List<int>();

                Parallel.ForEach(KeyScriptDictionary[multiKey], script =>
                {
                    var scriptType = script.Run(beatmapFile, beatmapFolder, Diffs, BeatmapFiles, currentTime);
                    scriptTypes.Add(scriptType);

                    if (scriptType < 0)
                        messages.Add($"Failed to run script {script.ScriptString}");

                    if (scriptType is (int)ScriptType.AddDiff or (int)ScriptType.CreateDiffs)
                        _sameMapDuration = 0;

                    if (scriptType is (int)ScriptType.AddMap or (int)ScriptType.CreateMaps)
                        _lastMapAddedDuration = 0;
                    
                });

                foreach (var message in messages)
                    Logger.LogMessage(message);


                var outputOsz = new DirectoryInfo(beatmapFolder).Name + ".osz";
                BeatmapHelper.LoadBeatmapWithOsz(GlobalConstants.BEATMAP_TEMP, outputOsz, beatmapFolder);
                BeatmapHelper.LoadBeatmapsWithOsz(GlobalConstants.BEATMAPS_TEMP, _songsFolder);


                DeleteFiles();

                if (scriptTypes.Any(s => s is (int)ScriptType.CreateDiffs or (int)ScriptType.CreateMap or (int)ScriptType.CreateMaps))
                {
                    ScriptFinish.Play();
                }
                else if (scriptTypes.All(s => s < 0))
                {
                    ScriptError.Play();
                }
            }
            catch (Exception ex)
            {
                ScriptError.Play();
                Logger.LogError(ex);
            }
        }

        private static void OnGlobalKeyUp(object sender, List<Keys> keys)
        {
            keys = KeyScriptDictionary.Keys.FirstOrDefault(keys.SequenceEqual);
            if (keys is null) return;
            ScriptStart.Play();

            if (!GetCurrentBeatmapInfo(_songsFolder, out var currentOsuFile, out var beatmapFile, out var beatmapFolder))
                return;

            if (currentOsuFile != _prevOsuFile)
            {
                _sameMapDuration = 0;
                Script.ParsedBeatmap = null;
                Diffs.Clear();
            }

            RunScripts(keys, beatmapFile, beatmapFolder);

            _prevOsuFile = currentOsuFile;
        }

        private static void OnGlobalKeyDown(object sender, List<Keys> keys)
        {
            if (!GetCurrentBeatmapInfo(_songsFolder, out _, out var beatmapFile, out _))
                return;

            if (keys.SequenceEqual(ResetGlobalKey))
                ScriptStart.Play();

            ScriptHelper.SetGlobalOptions(keys, beatmapFile, StatKeys, RateKey, ResetGlobalKey);
        }

        private static bool GetCurrentBeatmapInfo(string songsFolder, out string currentOsuFile, out string beatmapFile, out string beatmapFolder)
        {
            currentOsuFile = _osuReader.GetOsuFileName();
            var mapFolder = _osuReader.GetMapFolderName();

            if (string.IsNullOrEmpty(currentOsuFile) || string.IsNullOrEmpty(mapFolder))
            {
                ScriptError.Play();
                Logger.LogMessage("Error: couldn't get current osu file.");
                beatmapFile = null;
                beatmapFolder = null;
                return false;
            }

            beatmapFolder = Path.Combine(songsFolder, mapFolder);
            beatmapFile = BeatmapHelper.GetOriginalBeatmap(Path.Combine(beatmapFolder, currentOsuFile), beatmapFolder);
            return true;
        }

        private static void DeleteFiles()
        {
            foreach (var file in new DirectoryInfo(GlobalConstants.BEATMAP_TEMP).GetFiles())
                file.Delete();
            foreach (var dir in new DirectoryInfo(GlobalConstants.BEATMAPS_TEMP).GetDirectories())
                dir.Delete(true);
        }
    }
}
