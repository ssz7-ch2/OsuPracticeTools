using OsuMemoryDataProvider;
using OsuPracticeTools.Core;
using OsuPracticeTools.Core.BeatmapHelpers;
using OsuPracticeTools.Core.GlobalSettings;
using OsuPracticeTools.Core.Scripts;
using OsuPracticeTools.Core.Scripts.BeatmapScripts;
using OsuPracticeTools.Core.Scripts.Helpers;
using OsuPracticeTools.Core.Scripts.PracticeDiffScripts;
using OsuPracticeTools.Helpers;
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
        private static bool _gameRunning;
        private static Timer _timer;
        private static bool _hotkeysLoaded;
        private static Mutex _mutex;

        private static readonly Dictionary<List<Keys>, List<Script>> KeyScriptDictionary = new();
        private static readonly SoundPlayer ScriptStart = new("Resources/scriptStart.wav");
        private static readonly SoundPlayer ScriptFinish = new("Resources/scriptFinish.wav");
        private static readonly SoundPlayer ScriptError = new("Resources/scriptError.wav");

        internal static Keys[] StatKeys = { Keys.Z, Keys.X, Keys.C, Keys.V };
        internal static Keys RateKey = Keys.R;
        internal static List<Keys> ResetGlobalKey = new() { Keys.Back | Keys.Shift };
        internal static Process OsuProcess;

        private static void Main()
        {
            _mutex = new Mutex(true, "osu Practice Tools Singleton", out var createdNew);
            if (!createdNew)
            {
                Logger.LogMessage("OsuPracticeTools is already running");
                return;
            }

            _songsFolder = Properties.Settings.Default.SongsFolder;
            AppDomain.CurrentDomain.UnhandledException += UnhandledException;
            Application.ApplicationExit += OnExit;

            _osuReader = OsuMemoryReader.Instance.GetInstanceForWindowTitleHint("");
            _timer = new Timer {Interval = 20000};
            _timer.Tick += CheckGameRunning;
            _timer.Start();

            CheckGameRunning(0);

            Application.Run(new SystemTray());
        }

        private static void UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.LogError(e.ExceptionObject as Exception);
            Application.Exit();
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

        private static void CheckGameRunning(object sender, EventArgs e) => CheckGameRunning(_timer.Interval);

        private static void CheckGameRunning(int timeElapsed)
        {
            var processes = Process.GetProcessesByName("osu!");

            if (processes.Length == 0)
            {
                _gameRunning = false;
                GameNotRunning(timeElapsed);
            }
            else
            {
                _gameRunning = true;
                OsuProcess = processes[0];
                GameRunning(timeElapsed);
            }

            _prevGameState = _gameRunning;
        }

        private static void GameNotRunning(int timeElapsed)
        {
            if (_prevGameState != _gameRunning)
            {
                Logger.LogMessage("Game closed, unloading hotkeys");

                Bass.BASS_Free();

                GlobalKeyboardHook.Unhook();
                UnloadHotkeys();

                Info.Clear();
            }
        }

        private static void GameRunning(int timeElapsed)
        {
            if (_prevGameState != _gameRunning)
            {
                Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
                GlobalKeyboardHook.Hook();
                LoadHotkeys();
            }

            if (string.IsNullOrEmpty(_songsFolder) && OsuProcess != null)
                GetSongsFolder();

            Info.Update(timeElapsed, _osuReader.GetOsuFileName());

            Info.PreviousOsuFile = Info.CurrentOsuFile;
        }

        private static void GetSongsFolder()
        {
            try
            {
                var osuExePath = OsuProcess.MainModule.FileName;
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
            // reset global settings
            GlobalKeyboardHook.HookedDownKeys.AddUnique(ResetGlobalKey);

            foreach (var statKey in StatKeys)
            {
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey, Keys.OemMinus });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey, Keys.Oemplus });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey, Keys.Back });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey | Keys.Shift, Keys.OemMinus | Keys.Shift });
                GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { statKey | Keys.Shift, Keys.Oemplus | Keys.Shift });
            }

            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey, Keys.OemMinus });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey, Keys.Oemplus });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey, Keys.Back });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey | Keys.Shift, Keys.OemMinus | Keys.Shift });
            GlobalKeyboardHook.HookedDownKeys.Add(new List<Keys> { RateKey | Keys.Shift, Keys.Oemplus | Keys.Shift });
        }

        private static void LoadHotkeys()
        {
            if (!_hotkeysLoaded)
            {
                ScriptParser.ParseScripts(KeyScriptDictionary);
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

        private static async void RunScripts(List<Keys> multiKey)
        {
            Info.CurrentPlayTime = _osuReader.ReadPlayTime();

            _osuReader.GetCurrentStatus(out var osuStatus);
            Info.CurrentOsuStatus = osuStatus;

            try
            {
                Directory.CreateDirectory(GlobalConstants.BEATMAP_TEMP);
                Directory.CreateDirectory(GlobalConstants.BEATMAPS_TEMP);


                var messages = new List<string>();

                var playFinish = false;
                var playError = false;

                var playFinishTypes = new[]
                {
                    typeof(CreateMapScript), typeof(CreateMapsScript), typeof(CreateDiffsScript),
                    typeof(UpdateDiffScript), typeof(UpdateDiffEndScript)
                };

                await Task.Factory.StartNew(() => {
                    Parallel.ForEach(KeyScriptDictionary[multiKey], script =>
                    {
                        var result = script.Run();

                        if (result is null)
                        {
                            messages.Add($"Failed to run script {script.ScriptString}");
                            playError = true;
                        }

                        if (playFinishTypes.Contains(result))
                            playFinish = true;
                    });
                });

                Logger.LogMessage(string.Join('\n', messages));

                var outputOsz = new DirectoryInfo(Info.BeatmapFolder).Name + ".osz";
                BeatmapHelper.LoadBeatmapWithOsz(GlobalConstants.BEATMAP_TEMP, outputOsz, Info.BeatmapFolder);
                BeatmapHelper.LoadBeatmapsWithOsz(GlobalConstants.BEATMAPS_TEMP, _songsFolder);


                DeleteFiles();

                if (playFinish)
                {
                    ScriptFinish.Play();
                }
                else if (playError)
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
            try
            {
                keys = KeyScriptDictionary.Keys.FirstOrDefault(keys.SequenceEqual);
                if (keys is null) return;

                ScriptStart.Play();

                if (!GetCurrentBeatmapInfo())
                    return;

                RunScripts(keys);

                Info.PreviousOsuFile = Info.CurrentOsuFile;
            }
            catch (Exception ex)
            {
                ScriptError.Play();
                Logger.LogError(ex);
            }
        }

        private static void OnGlobalKeyDown(object sender, List<Keys> keys)
        {
            try
            {
                if (!GetCurrentBeatmapInfo())
                    return;

                if (keys.SequenceEqual(ResetGlobalKey))
                    ScriptStart.Play();

                GlobalSettingsHelper.SetGlobalSettings(keys, StatKeys, RateKey, ResetGlobalKey);

                Info.PreviousOsuFile = Info.CurrentOsuFile;
            }
            catch (Exception ex)
            {
                ScriptError.Play();
                Logger.LogError(ex);
            }
        }

        private static bool GetCurrentBeatmapInfo()
        {
            Info.Update(0, _osuReader.GetOsuFileName());

            var mapFolder = _osuReader.GetMapFolderName();

            if (string.IsNullOrEmpty(Info.CurrentOsuFile) || string.IsNullOrEmpty(mapFolder))
            {
                ScriptError.Play();
                Logger.LogMessage("Error: couldn't get current osu file.");
                Info.BeatmapFile = null;
                Info.BeatmapFolder = null;
                Info.CurrentBeatmapFile = null;
                return false;
            }

            Info.BeatmapFolder = Path.Combine(_songsFolder, mapFolder);
            Info.CurrentBeatmapFile = Path.Combine(Info.BeatmapFolder, Info.CurrentOsuFile);
            Info.BeatmapFile = BeatmapHelper.GetOriginalBeatmap(Info.CurrentBeatmapFile, Info.BeatmapFolder, GlobalConstants.BEATMAP_TAGS);
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
