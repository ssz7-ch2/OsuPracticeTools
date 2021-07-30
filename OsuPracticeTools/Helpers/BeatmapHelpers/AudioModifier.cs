using NAudio.MediaFoundation;
using NAudio.Vorbis;
using NAudio.Wave;
using OsuPracticeTools.Enums;
using System;
using System.Diagnostics;
using System.IO;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.Fx;

namespace OsuPracticeTools.Helpers.BeatmapHelpers
{
    public static class AudioModifier
    {
        // outFile is final destination, tempFile is temporary destination for zipping into .osz file
        public static int ChangeAudioRate(string inFile, string outFile, string tempFile, double rate, bool changePitch = false, AudioProcessor processor = AudioProcessor.Bass)
        {
            if (!File.Exists(inFile))
                throw new IOException($"Error: Failed to modify audio file. {inFile} does not exist.");
            if (File.Exists(outFile))
                return 0;

            var adjustTiming = -5;

            var ext = Path.GetExtension(inFile).ToLower();
            if (ext != ".mp3" && ext != ".ogg")
                throw new InvalidOperationException($"Error: the file type {ext} is not supported");

            var stream = 0;
            var streamFX = 0;
            var encoder = 0;
            try
            {
                if (processor == AudioProcessor.Bass)
                {
                    /*if (File.Exists(outFile.Replace(".mp3", $" -t {adjustTiming}.mp3")))
                        return adjustTiming;

                    tempFile = tempFile.Replace(".mp3", $" -t {adjustTiming}.mp3");*/

                    stream = Bass.BASS_StreamCreateFile(inFile, 0, 0, BASSFlag.BASS_STREAM_DECODE);
                    if (stream == 0)
                        throw new Exception($"Error: BASS_StreamCreateFile failed - {Bass.BASS_ErrorGetCode()}");

                    streamFX = BassFx.BASS_FX_TempoCreate(stream, BASSFlag.BASS_STREAM_DECODE);
                    if (streamFX == 0)
                        throw new Exception($"Error: BASS_FX_TempoCreate failed - {Bass.BASS_ErrorGetCode()}");

                    if (!Bass.BASS_ChannelSetAttribute(streamFX, BASSAttribute.BASS_ATTRIB_TEMPO, (float)((rate - 1) * 100)))
                        throw new Exception($"Error: BASS_ChannelSetAttribute failed - {Bass.BASS_ErrorGetCode()}");

                    if (changePitch)
                    {
                        var semitones = 1200.0 * Math.Log(rate) / Math.Log(2) / 100;
                        if (!Bass.BASS_ChannelSetAttribute(streamFX, BASSAttribute.BASS_ATTRIB_TEMPO_PITCH, (float)semitones))
                            throw new Exception($"Error: BASS_ChannelSetAttribute failed - {Bass.BASS_ErrorGetCode()}");
                    }

                    encoder = BassEnc.BASS_Encode_Start(streamFX, $"binaries/lame --alt-preset standard - \"{tempFile}\"",
                        0, null, IntPtr.Zero);
                    if (encoder == 0)
                        throw new Exception($"Error: BASS_Encode_Start failed - {Bass.BASS_ErrorGetCode()}");

                    var data = new short[32768];
                    while (Bass.BASS_ChannelIsActive(streamFX) == BASSActive.BASS_ACTIVE_PLAYING)
                    {
                        var len = Bass.BASS_ChannelGetData(streamFX, data, 32768);
                        if (len == -1)
                            throw new Exception($"Error: BASS_ChannelGetData failed - {Bass.BASS_ErrorGetCode()}");
                    }

                    BassEnc.BASS_Encode_Stop(encoder);
                    Bass.BASS_StreamFree(stream);
                    Bass.BASS_StreamFree(streamFX);
                    //return adjustTiming;
                    return 0;
                }
            }
            catch (Exception ex)
            {
                BassEnc.BASS_Encode_Stop(encoder);
                Bass.BASS_StreamFree(stream);
                Bass.BASS_StreamFree(streamFX);
                Logger.LogMessage($"Failed to change rate for {inFile}\n{ex.Message}\nSwitching to FFMPEG.");

                //tempFile = tempFile.Replace($" -t {adjustTiming}.mp3", ".mp3");
            }


            // backup
            adjustTiming = 15;

            if (File.Exists(outFile.Replace(".mp3", $" -t {adjustTiming}.mp3")))
                return adjustTiming;

            tempFile = tempFile.Replace(".mp3", $" -t {adjustTiming}.mp3");

            var temp1 = Path.Combine(Guid.NewGuid() + ext);
            var temp2 = Path.Combine(Guid.NewGuid() + ".wav"); // decoded wav
            var temp3 = Path.Combine(Guid.NewGuid() + ".wav");

            File.Copy(inFile, temp1);

            switch (processor)
            {
                case AudioProcessor.NAudio:
                    if (ext == ".mp3")
                    {
                        using var mp3 = new Mp3FileReader(temp1);
                        //using var wav = WaveFormatConversionStream.CreatePcmStream(mp3);
                        WaveFileWriter.CreateWaveFile(temp2, mp3);
                    }

                    if (ext == ".ogg")
                    {
                        using var ogg = new VorbisWaveReader(temp1);
                        WaveFileWriter.CreateWaveFile(temp2, ogg.ToWaveProvider16());
                    }
                    break;
                default:
                    var ffmpeg = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "binaries/ffmpeg.exe",
                            Arguments = $"-y -loglevel quiet -i \"{temp1}\" \"{temp2}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        },
                    };
                    ffmpeg.Start();

                    ffmpeg.WaitForExit(60000);
                    break;
            }



            var highQuality = false;
            var quick = highQuality ? "" : "-quick";
            var naa = highQuality ? "" : "-naa";

            var tempo = $"-tempo={(rate - 1) * 100}";

            var pitch = "";
            if (changePitch)
                pitch = $"-pitch={(decimal)(1200.0 * Math.Log(rate) / Math.Log(2)) / 100.0M}";

            var soundstretch = new Process
            {
                StartInfo =
                {
                    FileName = Path.Combine("binaries", "soundstretch.exe"),
                    Arguments = $"\"{temp2}\" \"{temp3}\" {quick} {naa} {tempo} {pitch}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            soundstretch.Start();
            soundstretch.WaitForExit();

            /*using (var wav = new WaveFileReader(temp3))
            using (var mp3 = new LameMP3FileWriter(tempFile, wav.WaveFormat, highQuality ? LAMEPreset.STANDARD : LAMEPreset.MEDIUM))
                wav.CopyTo(mp3);*/

            // faster but adds some extra silence (~15ms) to beginning of mp3 :(
            MediaFoundationApi.Startup();
            using (var wav = new WaveFileReader(temp3))
            {
                MediaFoundationEncoder.EncodeToMp3(wav, tempFile);
            }

            try
            {
                File.Delete(temp1);
                File.Delete(temp2);
                File.Delete(temp3);
            }
            catch { }

            return adjustTiming;
        }
    }
}
