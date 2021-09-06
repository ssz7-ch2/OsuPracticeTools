using System;
using System.Diagnostics;
using System.IO;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.Fx;
using Un4seen.Bass.Misc;

namespace OsuPracticeTools.Core.BeatmapHelpers
{
    public static class AudioModifier
    {
        private static void IncreaseRate(int stream, string inFile, string outFile, double rate, bool changePitch)
        {
            var streamFX = 0;
            var encoder = 0;

            streamFX = BassFx.BASS_FX_TempoCreate(stream, BASSFlag.BASS_STREAM_DECODE);
            if (streamFX == 0)
                throw new Exception($"Error: Failed to change rate for {inFile}\nBASS_FX_TempoCreate failed - {Bass.BASS_ErrorGetCode()}");

            if (!Bass.BASS_ChannelSetAttribute(streamFX, BASSAttribute.BASS_ATTRIB_TEMPO, (float)((rate - 1) * 100)))
                throw new Exception($"Error: Failed to change rate for {inFile}\nBASS_ChannelSetAttribute failed - {Bass.BASS_ErrorGetCode()}");

            if (!Bass.BASS_ChannelSetAttribute(streamFX, BASSAttribute.BASS_ATTRIB_TEMPO_OPTION_USE_QUICKALGO, 1))
                throw new Exception($"Error: Failed to change rate for {inFile}\nBASS_ChannelSetAttribute failed - {Bass.BASS_ErrorGetCode()}");
            if (!Bass.BASS_ChannelSetAttribute(streamFX, BASSAttribute.BASS_ATTRIB_TEMPO_OPTION_OVERLAP_MS, 4))
                throw new Exception($"Error: Failed to change rate for {inFile}\nBASS_ChannelSetAttribute failed - {Bass.BASS_ErrorGetCode()}");
            if (!Bass.BASS_ChannelSetAttribute(streamFX, BASSAttribute.BASS_ATTRIB_TEMPO_OPTION_SEQUENCE_MS, 30))
                throw new Exception($"Error: Failed to change rate for {inFile}\nBASS_ChannelSetAttribute failed - {Bass.BASS_ErrorGetCode()}");

            if (changePitch)
            {
                var semitones = 1200.0 * Math.Log(rate) / Math.Log(2) / 100;
                if (!Bass.BASS_ChannelSetAttribute(streamFX, BASSAttribute.BASS_ATTRIB_TEMPO_PITCH, (float)semitones))
                    throw new Exception($"Error: BASS_ChannelSetAttribute failed - {Bass.BASS_ErrorGetCode()}");
            }

            encoder = BassEnc.BASS_Encode_Start(streamFX, $"binaries/lame --alt-preset standard - \"{outFile}\"",
                0, null, IntPtr.Zero);
            if (encoder == 0)
                throw new Exception($"Error: Failed to change rate for {inFile}\nBASS_Encode_Start failed - {Bass.BASS_ErrorGetCode()}");

            var data = new short[32768];
            while (Bass.BASS_ChannelIsActive(streamFX) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                var len = Bass.BASS_ChannelGetData(streamFX, data, 32768);
                if (len == -1)
                    throw new Exception($"Error: Failed to change rate for {inFile}\nBASS_ChannelGetData failed - {Bass.BASS_ErrorGetCode()}");
            }

            Bass.BASS_StreamFree(streamFX);
            BassEnc.BASS_Encode_Stop(encoder);
        }

        // use rubberband for decreasing rate
        private static void DecreaseRate(int stream, double rate, bool changePitch, string outFile)
        {
            var temp1 = Path.Combine(Guid.NewGuid() + ".wav");
            var temp2 = Path.Combine(Guid.NewGuid() + ".wav");

            var waveEncoder = new EncoderWAV(stream)
            {
                InputFile = null,
                OutputFile = temp1
            };
            waveEncoder.Start(null, IntPtr.Zero, false);
            Utils.DecodeAllData(stream, true);
            waveEncoder.Stop();

            var pitch = "";
            if (changePitch)
                pitch = $"--pitch {1200.0 * Math.Log(rate) / Math.Log(2) / 100.0}";

            var rubberband = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine("binaries", "rubberband.exe"),
                    Arguments = $"--tempo {rate} {pitch} \"{temp1}\" \"{temp2}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            rubberband.Start();
            rubberband.WaitForExit();

            var lame = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine("binaries", "lame.exe"),
                    Arguments = $"--alt-preset standard \"{temp2}\" \"{outFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            lame.Start();
            lame.WaitForExit();

            try
            {
                File.Delete(temp1);
                File.Delete(temp2);
            }
            catch
            {
            }
        }

        // outFile is final destination, tempFile is temporary destination for zipping into .osz file
        public static void ChangeAudioRate(string inFile, string outFile, double rate, bool changePitch = false)
        {
            if (!File.Exists(inFile))
                throw new IOException($"Error: Failed to modify audio file. {inFile} does not exist.");

            var ext = Path.GetExtension(inFile).ToLower();
            if (ext != ".mp3" && ext != ".ogg")
                throw new InvalidOperationException($"Error: the file type {ext} is not supported");

            var stream = 0;

            try
            {
                stream = Bass.BASS_StreamCreateFile(inFile, 0, 0, BASSFlag.BASS_STREAM_DECODE);
                if (stream == 0)
                    throw new Exception($"Error: Failed to change rate for {inFile}\nBASS_StreamCreateFile failed - {Bass.BASS_ErrorGetCode()}");

                if (rate > 1)
                {
                    IncreaseRate(stream, inFile, outFile, rate, changePitch);
                }
                else if (rate < 1)
                {
                    DecreaseRate(stream, rate, changePitch, outFile);
                }

                Bass.BASS_StreamFree(stream);
            }
            catch (Exception)
            {
                Bass.BASS_StreamFree(stream);
                throw;
            }
        }
    }
}
