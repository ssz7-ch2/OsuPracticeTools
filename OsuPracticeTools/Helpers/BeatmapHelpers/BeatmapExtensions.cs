using OsuLightBeatmapParser;
using OsuLightBeatmapParser.Enums;
using OsuLightBeatmapParser.Helpers;
using OsuLightBeatmapParser.Objects;
using OsuLightBeatmapParser.Sections;
using OsuPracticeTools.Enums;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace OsuPracticeTools.Helpers.BeatmapHelpers
{
    internal static class BeatmapExtensions
    {
        // check if file exists in beatmap folder, then save to temp folder
        public static void Save(this Beatmap beatmap, string tempFolder, string beatmapFolder, bool overwrite, bool rename = false)
        {
            var path = Path.Combine(beatmapFolder, beatmap.FileName);
            try
            {
                if (!File.Exists(path) || overwrite)
                    beatmap.Save(tempFolder);
                else if (rename)
                {
                    beatmap.Rename(beatmapFolder);
                    beatmap.Save(tempFolder);
                }
            }
            catch (IOException)
            {
                // don't create the same beatmap
            }
        }

        public static void Rename(this Beatmap beatmap, string folder)
        {
            var regex = new Regex(@" v([0-9]+)$");
            while (File.Exists(Path.Combine(folder, beatmap.FileName)))
            {
                var match = regex.Match(beatmap.Metadata.Version);

                if (match.Success)
                    beatmap.Metadata.Version = beatmap.Metadata.Version.Replace(match.Value, $" v{Convert.ToInt32(match.Groups[1].Value) + 1}");
                else
                    beatmap.Metadata.Version += " v2";
            }
        }

        public static void FormatName(this Beatmap beatmap, Beatmap originalBeatmap, string format = "{v}{R}{BPM}{CS}{AR}{OD}{HP}", double speedRate = 1)
        {
            const string timeFormat = @"m\:ss";
            var regex = new Regex(@"{([a-zA-Z]+)}");
            beatmap.Metadata.Version = regex.Replace(format, m =>
            {
                switch (m.Groups[1].Value)
                {
                    case "v":
                        return originalBeatmap.Metadata.Version;
                    case "R":
                        return Math.Abs(speedRate - 1) < 0.001 ? "" : $" {speedRate:0.###}x";
                    case "l":
                        return TimeSpan.FromMilliseconds(originalBeatmap.General.Length).ToString(timeFormat);
                    case "mc":
                        return originalBeatmap.General.MaxCombo.ToString();
                    case "CS":
                        return (Math.Abs(beatmap.Difficulty.CircleSize - originalBeatmap.Difficulty.CircleSize) < 0.001) ? "" : $" CS{beatmap.Difficulty.CircleSize:0.##}";
                    case "AR":
                        return (Math.Abs(beatmap.Difficulty.ApproachRate - originalBeatmap.Difficulty.ApproachRate) < 0.001) ? "" : $" AR{beatmap.Difficulty.ApproachRate:0.##}";
                    case "OD":
                        return (Math.Abs(beatmap.Difficulty.OverallDifficulty - originalBeatmap.Difficulty.OverallDifficulty) < 0.001) ? "" : $" OD{beatmap.Difficulty.OverallDifficulty:0.##}";
                    case "HP":
                        return (Math.Abs(beatmap.Difficulty.HPDrainRate - originalBeatmap.Difficulty.HPDrainRate) < 0.001) ? "" : $" HP{beatmap.Difficulty.HPDrainRate:0.##}";
                    case "BPM":
                        return Math.Abs(speedRate - 1) < 0.001 ? "" : $" ({Convert.ToInt32(beatmap.General.MainBPM)}bpm)";
                    default:
                        return $"{{{m.Groups[1].Value}}}";
                }
            }).Trim();
        }

        public static void ModifyDifficulty(this Beatmap beatmap, float? cs = null, float? ar = null, float? od = null, float? hp = null,
            float? minCS = null, float? maxCS = null, float? minAR = null, float? maxAR = null, float? minOD = null, float? maxOD = null)
        {
            beatmap.Difficulty.CircleSize = Math.Clamp(cs ?? beatmap.Difficulty.CircleSize, Math.Max(minCS ?? 0, 0), Math.Min(maxCS ?? 10, 10));
            beatmap.Difficulty.ApproachRate = Math.Clamp(ar ?? beatmap.Difficulty.ApproachRate, Math.Max(minAR ?? 0, 0), Math.Min(maxAR ?? 10, 10));
            beatmap.Difficulty.OverallDifficulty = Math.Clamp(od ?? beatmap.Difficulty.OverallDifficulty, Math.Max(minOD ?? 0, 0), Math.Min(maxOD ?? 10, 10));
            beatmap.Difficulty.HPDrainRate = Math.Clamp(hp ?? beatmap.Difficulty.HPDrainRate, 0, 10);
        }

        public static void ApplyHR(this Beatmap beatmap)
        {
            // apply hr first then modify beatmap
            beatmap.ModifyDifficulty(
                beatmap.Difficulty.CircleSize * 1.3f, 
                beatmap.Difficulty.ApproachRate * 1.4f,
                beatmap.Difficulty.OverallDifficulty * 1.4f, 
                beatmap.Difficulty.HPDrainRate * 1.4f);

            foreach (var hitObject in beatmap.HitObjects)
            {
                hitObject.Position = new Vector2(hitObject.Position.X, 384 - hitObject.Position.Y);
                if (hitObject is Slider slider)
                    slider.CurvePoints = slider.CurvePoints.ConvertAll(p => new Vector2(p.X, 384 - p.Y));
            }
        }

        public static void ApplyFlip(this Beatmap beatmap, FlipDirection direction)
        {
            foreach (var hitObject in beatmap.HitObjects)
            {
                switch (direction)
                {
                    case FlipDirection.Vertical:
                        hitObject.Position = new Vector2(hitObject.Position.X, 384 - hitObject.Position.Y);
                        if (hitObject is Slider sliderV)
                            sliderV.CurvePoints = sliderV.CurvePoints.ConvertAll(p => new Vector2(p.X, 384 - p.Y));
                        break;
                    case FlipDirection.Horizontal:
                        hitObject.Position = new Vector2(512 - hitObject.Position.X, hitObject.Position.Y);
                        if (hitObject is Slider sliderH)
                            sliderH.CurvePoints = sliderH.CurvePoints.ConvertAll(p => new Vector2(512 - p.X, p.Y));
                        break;
                }
            }
        }

        public static void RemoveSpinners(this Beatmap beatmap) => beatmap.HitObjects.RemoveAll(h => h is Spinner);

        public static void ChangeSpeedRate(this Beatmap beatmap, string tempFolder, string beatmapFolder, double rate, bool pitch)
        {
            var newAudioFile = $"{Path.GetFileNameWithoutExtension(beatmap.General.AudioFilename)} {rate:0.000}x.mp3";
            if (!File.Exists(Path.Combine(beatmapFolder, newAudioFile)))
            {
                if (Path.Combine(new DirectoryInfo(tempFolder).FullName, newAudioFile).Length > 260)
                    newAudioFile = $"audio {rate:0.000}x.mp3";
                AudioModifier.ChangeAudioRate(Path.Combine(beatmapFolder, beatmap.General.AudioFilename), Path.Combine(tempFolder, newAudioFile), rate, pitch);
            }

            beatmap.General.AudioFilename = newAudioFile;
            beatmap.General.AudioLeadIn = (int)(beatmap.General.AudioLeadIn / rate);
            beatmap.General.PreviewTime = (int)(beatmap.General.PreviewTime / rate);
            beatmap.General.MainBPM *= rate;
            beatmap.General.Length = (int)(beatmap.General.Length / rate);

            beatmap.Editor.Bookmarks = beatmap.Editor.Bookmarks?.Select(b => Convert.ToInt32(b / rate)).ToArray();

            beatmap.ModifyDifficulty(
                ar: BeatmapDifficulty.ApplyRateChangeAR(beatmap.Difficulty.ApproachRate, rate),
                od: BeatmapDifficulty.ApplyRateChangeOD(beatmap.Difficulty.OverallDifficulty, rate));

            beatmap.Events.VideoStartTime = (int)(beatmap.Events.VideoStartTime / rate);
            foreach (var b in beatmap.Events.Breaks)
            {
                b.StartTime = (int)(b.StartTime / rate);
                b.EndTime = (int)(b.EndTime / rate);
            }

            foreach (var timingPoint in beatmap.TimingPoints)
            {
                timingPoint.Time = (int)(timingPoint.Time / rate);
                if (timingPoint.Uninherited)
                    timingPoint.BeatLength /= rate;
            }

            foreach (var hitObject in beatmap.HitObjects)
            {
                hitObject.StartTime = (int)(hitObject.StartTime / rate);
                hitObject.EndTime = (int)(hitObject.EndTime / rate);
            }
        }

        public static HitObject HitObjectAtOrAfter(this Beatmap beatmap, int time) => beatmap.HitObjects.FirstOrDefault(t => t.StartTime >= time);

        public static HitObject HitObjectEndTimeBefore(this Beatmap beatmap, int time)
        {
            for (int i = beatmap.HitObjects.Count - 1; i >= 0; i--)
            {
                if (beatmap.HitObjects[i].EndTime < time)
                    return beatmap.HitObjects[i];
            }

            return null;
        }

        public static int TimingTickBefore(this Beatmap beatmap, int time, int beatRate)
        {
            var timingPoint = beatmap.UninheritedTimingPointAt(time);
            var adjustedBeatLength = timingPoint.BeatLength / beatRate;
            return (int)(adjustedBeatLength *
                Math.Floor(Math.Round((time - timingPoint.Time) / adjustedBeatLength, 2)) + timingPoint.Time);
        }

        public static int ColorOffsetAt(this Beatmap beatmap, int comboColours, int time)
        {
            var newCombos = beatmap.HitObjects.Where(h => h.NewCombo && h.StartTime <= time && h is not Spinner);

            return Math.Max(0, newCombos.Aggregate(-1, (current, combo) => (current + 1 + combo.ComboColourOffset) % comboColours));
        }

        // cloning does not include unparsed since if section has unparsed, it means you aren't going to use the section, thus cloning is meaningless
        public static Beatmap Clone(this Beatmap beatmap, FileSection[] sections)
        {
            var newBeatmap = new Beatmap
            {
                General = beatmap.General,
                Editor = beatmap.Editor,
                Metadata = beatmap.Metadata,
                Difficulty = beatmap.Difficulty,
                Events = beatmap.Events,
                Colours = beatmap.Colours,
                TimingPoints = beatmap.TimingPoints,
                HitObjects = beatmap.HitObjects
            };

            foreach (var section in sections)
            {
                switch (section)
                {
                    case FileSection.General:
                        newBeatmap.General = beatmap.CloneGeneralSection();
                        break;
                    case FileSection.Editor:
                        newBeatmap.Editor = beatmap.CloneEditorSection();
                        break;
                    case FileSection.Metadata:
                        newBeatmap.Metadata = beatmap.CloneMetadataSection();
                        break;
                    case FileSection.Difficulty:
                        newBeatmap.Difficulty = beatmap.CloneDifficultySection();
                        break;
                    case FileSection.Events:
                        newBeatmap.Events = beatmap.CloneEventsSection();
                        break;
                    case FileSection.TimingPoints:
                        newBeatmap.TimingPoints = beatmap.CloneTimingPointsSection();
                        break;
                    case FileSection.Colours:
                        newBeatmap.Colours = beatmap.CloneColoursSection();
                        break;
                    case FileSection.HitObjects:
                        newBeatmap.HitObjects = beatmap.CloneHitObjectsSection();
                        break;
                }
            }

            return newBeatmap;
        }

        public static GeneralSection CloneGeneralSection(this Beatmap beatmap)
        {
            return new()
            {
                AudioFilename = beatmap.General.AudioFilename,
                AudioLeadIn = beatmap.General.AudioLeadIn,
                PreviewTime = beatmap.General.PreviewTime,
                Countdown = beatmap.General.Countdown,
                SampleSet = beatmap.General.SampleSet,
                StackLeniency = beatmap.General.StackLeniency,
                Mode = beatmap.General.Mode,
                LetterboxInBreaks = beatmap.General.LetterboxInBreaks,
                UseSkinSprites = beatmap.General.UseSkinSprites,
                OverlayPosition = beatmap.General.OverlayPosition,
                SkinPreference = beatmap.General.SkinPreference,
                EpilepsyWarning = beatmap.General.EpilepsyWarning,
                CountdownOffset = beatmap.General.CountdownOffset,
                SpecialStyle = beatmap.General.SpecialStyle,
                WidescreenStoryboard = beatmap.General.WidescreenStoryboard,
                SamplesMatchPlaybackRate = beatmap.General.SamplesMatchPlaybackRate,
                Length = beatmap.General.Length,
                MaxCombo = beatmap.General.MaxCombo,
                MainBPM = beatmap.General.MainBPM
            };
        }

        public static EditorSection CloneEditorSection(this Beatmap beatmap)
        {
            return new()
            {
                Bookmarks = beatmap.Editor.Bookmarks?.ToArray(),
                DistanceSpacing = beatmap.Editor.DistanceSpacing,
                BeatDivisor = beatmap.Editor.BeatDivisor,
                GridSize = beatmap.Editor.GridSize,
                TimelineZoom = beatmap.Editor.TimelineZoom
            };
        }

        public static MetadataSection CloneMetadataSection(this Beatmap beatmap)
        {
            return new()
            {
                Title = beatmap.Metadata.Title,
                TitleUnicode = beatmap.Metadata.TitleUnicode,
                Artist = beatmap.Metadata.Artist,
                ArtistUnicode = beatmap.Metadata.ArtistUnicode,
                Creator = beatmap.Metadata.Creator,
                Version = beatmap.Metadata.Version,
                Source = beatmap.Metadata.Source,
                Tags = beatmap.Metadata.Tags,
                BeatmapID = beatmap.Metadata.BeatmapID,
                BeatmapSetID = beatmap.Metadata.BeatmapSetID
            };
        }

        public static DifficultySection CloneDifficultySection(this Beatmap beatmap)
        {
            return new()
            {
                CircleSize = beatmap.Difficulty.CircleSize,
                ApproachRate = beatmap.Difficulty.ApproachRate,
                OverallDifficulty = beatmap.Difficulty.OverallDifficulty,
                HPDrainRate = beatmap.Difficulty.HPDrainRate,
                SliderMultiplier = beatmap.Difficulty.SliderMultiplier,
                SliderTickRate = beatmap.Difficulty.SliderTickRate
            };
        }

        public static EventsSection CloneEventsSection(this Beatmap beatmap)
        {
            return new()
            {
                BackgroundImage = beatmap.Events.BackgroundImage,
                Breaks = beatmap.Events.Breaks?.ConvertAll(b => new Break{StartTime = b.StartTime, EndTime = b.EndTime}),
                Video = beatmap.Events.Video,
                VideoStartTime = beatmap.Events.VideoStartTime
            };
        }

        public static TimingPointsSection CloneTimingPointsSection(this Beatmap beatmap)
        {
            var timingPointsSection = new TimingPointsSection();
            beatmap.TimingPoints.ForEach(t => timingPointsSection.Add(CloneTimingPoint(t)));
            return timingPointsSection;
        }

        public static TimingPoint CloneTimingPoint(this TimingPoint timingPoint)
        {
            return new()
            {
                BeatLength = timingPoint.BeatLength,
                SampleIndex = timingPoint.SampleIndex,
                Effects = timingPoint.Effects,
                Uninherited = timingPoint.Uninherited,
                Time = timingPoint.Time,
                SampleSet = timingPoint.SampleSet,
                Meter = timingPoint.Meter,
                Volume = timingPoint.Volume
            };
        }

        public static ColoursSection CloneColoursSection(this Beatmap beatmap)
        {
            return new()
            {
                ComboColours = beatmap.Colours.ComboColours?.ConvertAll(c => c.ToArray()),
                SliderTrackOverride = beatmap.Colours.SliderTrackOverride?.ToArray(),
                SliderBorder = beatmap.Colours.SliderBorder?.ToArray()
            };
        }

        public static HitObjectsSection CloneHitObjectsSection(this Beatmap beatmap)
        {
            var hitObjectsSection = new HitObjectsSection();
            beatmap.HitObjects.ForEach(h => hitObjectsSection.Add(CloneHitObject(h)));
            return hitObjectsSection;
        }

        public static HitObject CloneHitObject(HitObject hitObject)
        {
            switch (hitObject)
            {
                case HitCircle:
                    return new HitCircle
                    {
                        Position = hitObject.Position,
                        StartTime = hitObject.StartTime,
                        EndTime = hitObject.EndTime,
                        HitSound = hitObject.HitSound,
                        HitSample = hitObject.HitSample,
                        NewCombo = hitObject.NewCombo,
                        ComboColourOffset = hitObject.ComboColourOffset
                    };
                case Slider slider:
                    return new Slider
                    {
                        Position = slider.Position,
                        StartTime = slider.StartTime,
                        EndTime = slider.EndTime,
                        HitSound = slider.HitSound,
                        HitSample = slider.HitSample,
                        NewCombo = slider.NewCombo,
                        ComboColourOffset = slider.ComboColourOffset,
                        CurveType = slider.CurveType,
                        CurvePoints = slider.CurvePoints,
                        Slides = slider.Slides,
                        Length = slider.Length,
                        EdgeSounds = slider.EdgeSounds,
                        EdgeSets = slider.EdgeSets
                    };
                case Spinner:
                    return new Spinner
                    {
                        Position = hitObject.Position,
                        StartTime = hitObject.StartTime,
                        EndTime = hitObject.EndTime,
                        HitSound = hitObject.HitSound,
                        HitSample = hitObject.HitSample,
                        NewCombo = hitObject.NewCombo,
                        ComboColourOffset = hitObject.ComboColourOffset
                    };
                case Hold:
                    return new Hold
                    {
                        Position = hitObject.Position,
                        StartTime = hitObject.StartTime,
                        EndTime = hitObject.EndTime,
                        HitSound = hitObject.HitSound,
                        HitSample = hitObject.HitSample,
                        NewCombo = hitObject.NewCombo,
                        ComboColourOffset = hitObject.ComboColourOffset
                    };
            }

            return null;
        }
    }
}
