﻿using OsuLightBeatmapParser;
using OsuLightBeatmapParser.Enums;
using OsuLightBeatmapParser.Helpers;
using OsuLightBeatmapParser.Objects;
using OsuLightBeatmapParser.Sections;
using OsuPracticeTools.Core.BeatmapHelpers;
using OsuPracticeTools.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace OsuPracticeTools.Core.PracticeDiffs
{
    public class PracticeDiff
    {
        private readonly Beatmap _beatmap;
        private readonly Beatmap _originalBeatmap;
        private readonly HitObject _hitObjectAtStartTime;
        private PracticeDiffSettings _settings;
        private int _startCombo;
        private int _endCombo;
        private int _endTime;

        public string Name
        {
            get => _beatmap.Metadata.Version;
            set => _beatmap.Metadata.Version = value;
        }
        public int StartTime { get; set; }
        public int EndTime
        {
            get => _endTime;
            set
            {
                if (value > StartTime)
                {
                    _endCombo = _originalBeatmap.ComboAt(value);
                    _endTime = _beatmap.HitObjectEndTimeBefore(value).EndTime;
                }
            }
        }

        public int Combo { get; set; }
        public ComboType ComboType { get; set; }
        public int? Index { get; set; }
        public int? Total { get; set; }
        public PracticeDiff(Beatmap beatmap, int startTime, int endTime)
        {
            _originalBeatmap = beatmap;
            _beatmap = InitialClone(beatmap);

            _hitObjectAtStartTime = beatmap.HitObjectAtOrAfter(startTime);
            StartTime = _hitObjectAtStartTime?.StartTime ?? _originalBeatmap.HitObjects.First().StartTime;
            _startCombo = beatmap.ComboAt(startTime);

            EndTime = endTime;

            Index = beatmap.General.Index;
            Total = beatmap.General.Total;
        }

        private static Beatmap InitialClone(Beatmap beatmap)
        {
            // clone metadata for sake of name formatting
            return beatmap.Clone(new[] { FileSection.Metadata });
        }

        private void CloneBeatmap()
        {
            // clone rest of necessary sections

            _beatmap.General = _beatmap.CloneGeneralSection();
            _beatmap.General.Countdown = CountdownType.None;
            _beatmap.Events = _beatmap.CloneEventsSection();
            _beatmap.TimingPoints = _beatmap.CloneTimingPointsSection();

            // shallow clone of hitObjects, since properties of each hitObject won't be modified
            var newHitObjectsSection = new HitObjectsSection();
            _beatmap.HitObjects.ForEach(h => newHitObjectsSection.Add(h));
            _beatmap.HitObjects = newHitObjectsSection;
        }

        public void FormatName()
        {
            const string timeFormat = @"m\:ss";
            var regex = new Regex(@"{([a-zA-Z]+)}");
            _beatmap.Metadata.Version = regex.Replace(_beatmap.Metadata.Version, m =>
            {
                switch (m.Groups[1].Value)
                {
                    case "i":
                        return Index.ToString();
                    case "n":
                        return Total.ToString();
                    case "s":
                        return TimeSpan.FromMilliseconds(StartTime).ToString(timeFormat);
                    case "e":
                        return TimeSpan.FromMilliseconds(EndTime).ToString(timeFormat);
                    case "c":
                        return Combo.ToString();
                    case "sc":
                        return _startCombo.ToString();
                    case "ec":
                        return _endCombo.ToString();
                    default:
                        return $"{{{m.Groups[1].Value}}}";
                }
            }).Trim();
        }

        private List<HitObject> GenerateCombo()
        {
            if (ComboType == ComboType.None)
                return new List<HitObject>();

            var comboEndTime = StartTime - _settings.GapDuration;

            comboEndTime = (int)_beatmap.TimingTickBefore(comboEndTime, 2);

            var colorOffset = 0;
            if (_beatmap.Colours.ComboColours.Any() && !_settings.CirclesComboColor)
            {
                var comboColors = _beatmap.Colours.ComboColours.Count;
                colorOffset = _beatmap.ColorOffsetAt(comboColors, StartTime);
                if (_hitObjectAtStartTime.NewCombo)
                    colorOffset = (((colorOffset - 1) % comboColors) + comboColors) % comboColors;
            }

            var circles = 0;
            if (ComboType != ComboType.None && _settings.CirclesComboColor)
            {
                var comboColors = _settings.SkinComboColors;
                circles = _beatmap.ColorOffsetAt(comboColors, StartTime, true);
                if (_hitObjectAtStartTime.NewCombo || circles != 0)
                {
                    if (ComboType == ComboType.Slider)
                    {
                        if (_hitObjectAtStartTime.NewCombo)
                            circles = (((circles - 1) % comboColors) + comboColors) % comboColors;
                    }
                    else if (ComboType == ComboType.Spinner)
                    {
                        if (!_hitObjectAtStartTime.NewCombo)
                            circles += 1;
                    }
                }
            }

            return ComboType switch
            {
                ComboType.Slider => GenerateSlider(comboEndTime, colorOffset, circles),
                ComboType.Spinner => GenerateSpinners(comboEndTime, circles),
                _ => new List<HitObject>()
            };
        }

        private List<HitObject> GenerateSlider(int endTime, int colorOffset, int circles = 0)
        {
            var position = _hitObjectAtStartTime.Position;

            var sliderDuration = _settings.SliderDuration;

            var startTime = (int)_beatmap.TimingTickBefore(endTime - sliderDuration, 1);
            sliderDuration = endTime - startTime;

            double length = 1;

            var bpmMultiplier =
                MathHelper.CalculateBpmMultiplierFromSliderLength(_beatmap, length, startTime,
                    sliderDuration, Combo - circles < 2 ? Combo : Combo - circles);
            while (bpmMultiplier < 0.1)
            {
                length *= 2;
                bpmMultiplier =
                    MathHelper.CalculateBpmMultiplierFromSliderLength(_beatmap, length, startTime,
                        sliderDuration, Combo - circles < 2 ? Combo : Combo - circles);
            }

            var newBeatLength = MathHelper.CalculateBeatLengthFromBpmMultiplier(bpmMultiplier);
            GenerateSoftTimingPoint(startTime, newBeatLength);

            var curvePoints = new List<Vector2>
            {
                position + new Vector2(0, (float)Math.Ceiling(length))
            };

            var slider = new Slider
            {
                Position = position,
                StartTime = startTime,
                EndTime = endTime,
                HitSound = HitSoundType.None,
                CurveType = CurveType.Linear,
                CurvePoints = curvePoints,
                Slides = Combo - circles < 2 ? Combo - 1 : Combo - circles - 1,
                Length = length,
                NewCombo = true,
                ComboColourOffset = _settings.CirclesComboColor ? 0 : colorOffset
            };

            var hitObjects = new List<HitObject>();

            if (Combo - circles >= 2)
            {
                var beatLength = _originalBeatmap.BeatLengthAt(StartTime);
                var circleStartTime = startTime - (beatLength / 2 * circles);
                for (int i = 0; i < circles; i++)
                {
                    hitObjects.Add(
                        new HitCircle
                        {
                            Position = _hitObjectAtStartTime.Position,
                            StartTime = (int)(circleStartTime + (beatLength / 2 * i)),
                            EndTime = (int)(circleStartTime + (beatLength / 2 * i)),
                            HitSound = HitSoundType.None,
                            HitSample = new HitSample { NormalSet = SampleSet.None, AdditionSet = SampleSet.None, Index = 0, Volume = 0 },
                            NewCombo = true,
                            ComboColourOffset = 0
                        });
                }
            }

            hitObjects.Add(slider);

            return hitObjects;
        }

        private List<HitObject> GenerateSpinners(int endTime, int circles = 0)
        {
            var beatLength = _originalBeatmap.BeatLengthAt(StartTime);
            if (Combo - circles >= 0)
                endTime = (int)(endTime - (beatLength / 2 * circles));

            GenerateSoftTimingPoint(endTime);
            GenerateHighBPMTimingPoint(endTime);

            var spinner = new Spinner
            {
                Position = new Vector2(256, 192),
                StartTime = endTime,
                EndTime = endTime,
                HitSound = HitSoundType.None,
                HitSample = new HitSample { NormalSet = SampleSet.None, AdditionSet = SampleSet.None, Index = 0, Volume = 0, Filename = "_" },
                NewCombo = true,
                ComboColourOffset = 0

            };

            var hitObjects = new List<HitObject>();
            hitObjects.AddRange(Enumerable.Repeat(spinner, Combo - circles < 0 ? Combo : Combo - circles));

            if (Combo - circles >= 0)
            {
                var startTime = _hitObjectAtStartTime.StartTime - (beatLength / 2 * circles);
                for (int i = 0; i < circles; i++)
                {
                    hitObjects.Add(
                        new HitCircle
                        {
                            Position = _hitObjectAtStartTime.Position,
                            StartTime = (int)(startTime + (beatLength / 2 * i)),
                            EndTime = (int)(startTime + (beatLength / 2 * i)),
                            HitSound = HitSoundType.None,
                            HitSample = new HitSample { NormalSet = SampleSet.None, AdditionSet = SampleSet.None, Index = 0, Volume = 0 },
                            NewCombo = true,
                            ComboColourOffset = 0
                        });
                }
            }

            return hitObjects;
        }

        public void GenerateTimingPointAtStart()
        {
            var timingPointAtStart = _beatmap.TimingPointAt(StartTime);
            if (timingPointAtStart.Time != StartTime)
            {
                var newTimingPoint = new TimingPoint()
                {
                    BeatLength = timingPointAtStart.BeatLength,
                    SampleIndex = timingPointAtStart.SampleIndex,
                    Effects = timingPointAtStart.Effects,
                    Uninherited = false,
                    Time = StartTime,
                    SampleSet = timingPointAtStart.SampleSet,
                    Meter = timingPointAtStart.Meter,
                    Volume = timingPointAtStart.Volume
                };
                if (timingPointAtStart.Uninherited)
                    newTimingPoint.BeatLength = -100;

                _beatmap.TimingPoints.Insert(_beatmap.TimingPoints.IndexOf(timingPointAtStart) + 1, newTimingPoint);
            }
        }

        public void GenerateSoftTimingPoint(int time, double beatLength = -100)
        {
            GenerateTimingPointAtStart();

            var currentTimingPoint = _beatmap.TimingPointAt(time);


            if (currentTimingPoint.Time == time && !currentTimingPoint.Uninherited)
            {
                currentTimingPoint.BeatLength = beatLength;
                currentTimingPoint.Volume = 5;
            }
            else
            {
                var newTimingPoint = new TimingPoint()
                {
                    BeatLength = beatLength,
                    SampleIndex = currentTimingPoint.SampleIndex,
                    Effects = currentTimingPoint.Effects,
                    Uninherited = false,
                    Time = time,
                    SampleSet = currentTimingPoint.SampleSet,
                    Meter = currentTimingPoint.Meter,
                    Volume = 5
                };

                _beatmap.TimingPoints.Insert(_beatmap.TimingPoints.IndexOf(currentTimingPoint) + 1, newTimingPoint);
            }

            foreach (var timingPoint in _beatmap.TimingPoints.Where(t => t.Time > time && t.Time < StartTime))
            {
                timingPoint.Volume = 5;
            }
        }

        public void GenerateHighBPMTimingPoint(int time)
        {
            var firstUninheritedTimingPoint = _beatmap.UninheritedTimingPointAt(_beatmap.TimingPoints[0].Time);
            if (_beatmap.HitObjects[0].StartTime < firstUninheritedTimingPoint.Time)
            {
                var fixFirstTimingPoint = new TimingPoint()
                {
                    BeatLength = firstUninheritedTimingPoint.BeatLength,
                    SampleIndex = firstUninheritedTimingPoint.SampleIndex,
                    Effects = firstUninheritedTimingPoint.Effects,
                    Uninherited = true,
                    Time = _beatmap.HitObjects[0].StartTime,
                    SampleSet = firstUninheritedTimingPoint.SampleSet,
                    Meter = firstUninheritedTimingPoint.Meter,
                    Volume = firstUninheritedTimingPoint.Volume
                };
                _beatmap.TimingPoints.Insert(_beatmap.TimingPoints.IndexOf(_beatmap.TimingPoints.First(t => t.Time >= _beatmap.HitObjects[0].StartTime)), fixFirstTimingPoint);
                firstUninheritedTimingPoint = fixFirstTimingPoint;
            }

            if (firstUninheritedTimingPoint.Time < 5)
            {
                var fixFirstTimingPoint = new TimingPoint()
                {
                    BeatLength = firstUninheritedTimingPoint.BeatLength,
                    SampleIndex = firstUninheritedTimingPoint.SampleIndex,
                    Effects = firstUninheritedTimingPoint.Effects,
                    Uninherited = true,
                    Time = firstUninheritedTimingPoint.Time + (int)firstUninheritedTimingPoint.BeatLength,
                    SampleSet = firstUninheritedTimingPoint.SampleSet,
                    Meter = firstUninheritedTimingPoint.Meter,
                    Volume = firstUninheritedTimingPoint.Volume
                };

                _beatmap.TimingPoints.Insert(_beatmap.TimingPoints.IndexOf(_beatmap.TimingPoints.First(t => t.Time >= fixFirstTimingPoint.Time)), fixFirstTimingPoint);
            }

            var decreaseLagTimingPoint = new TimingPoint()
            {
                BeatLength = 32d,
                SampleIndex = firstUninheritedTimingPoint.SampleIndex,
                Effects = firstUninheritedTimingPoint.Effects,
                Uninherited = true,
                Time = -31,
                SampleSet = firstUninheritedTimingPoint.SampleSet,
                Meter = firstUninheritedTimingPoint.Meter,
                Volume = firstUninheritedTimingPoint.Volume
            };

            var decreaseLagTimingPoint2 = new TimingPoint()
            {
                BeatLength = firstUninheritedTimingPoint.BeatLength,
                SampleIndex = firstUninheritedTimingPoint.SampleIndex,
                Effects = firstUninheritedTimingPoint.Effects,
                Uninherited = true,
                Time = 5,
                SampleSet = firstUninheritedTimingPoint.SampleSet,
                Meter = firstUninheritedTimingPoint.Meter,
                Volume = firstUninheritedTimingPoint.Volume
            };


            _beatmap.TimingPoints.Insert(_beatmap.TimingPoints.IndexOf(_beatmap.TimingPoints.First(t => t.Time >= -31)), decreaseLagTimingPoint);
            _beatmap.TimingPoints.Insert(_beatmap.TimingPoints.IndexOf(_beatmap.TimingPoints.First(t => t.Time >= 5)), decreaseLagTimingPoint2);

            if (firstUninheritedTimingPoint.Time == 4)
            {
                firstUninheritedTimingPoint.BeatLength = 0.1d;
            }
            else
            {
                var highBPMTimingPoint = new TimingPoint()
                {
                    BeatLength = 0.1d,
                    SampleIndex = firstUninheritedTimingPoint.SampleIndex,
                    Effects = firstUninheritedTimingPoint.Effects,
                    Uninherited = true,
                    Time = 4,
                    SampleSet = firstUninheritedTimingPoint.SampleSet,
                    Meter = firstUninheritedTimingPoint.Meter,
                    Volume = firstUninheritedTimingPoint.Volume
                };

                _beatmap.TimingPoints.Insert(_beatmap.TimingPoints.IndexOf(_beatmap.TimingPoints.First(t => t.Time >= highBPMTimingPoint.Time)), highBPMTimingPoint);
            }
        }

        private void RemoveHitObjects()
        {
            _beatmap.HitObjects.RemoveAll(h => h.StartTime < StartTime || h.EndTime > EndTime);
        }

        private void RemoveBreaks()
        {
            _beatmap.Events.Breaks.RemoveAll(b =>
                b.StartTime <= StartTime || b.EndTime >= EndTime);
        }

        public void ApplySettings(PracticeDiffSettings settings)
        {
            _settings = settings;
            ComboType = _settings.ComboType;
            var combo = _settings.ComboAmount ?? _startCombo;
            if (combo == 0) ComboType = ComboType.None;
            else if (combo == 1) ComboType = ComboType.Spinner;

            Combo = Math.Clamp(combo, (int)ComboType, 200);
        }

        public void Save(string tempFolder, string beatmapFolder, bool overwrite = false)
        {
            CloneBeatmap();

            var combo = GenerateCombo();
            RemoveHitObjects();
            RemoveBreaks();

            _beatmap.HitObjects.InsertRange(0, combo);

            _beatmap.Metadata.Tags.Add(GlobalConstants.PRACTICE_DIFF_TAG);
            _beatmap.General.Index = Index;
            _beatmap.General.Total = Total;
            _beatmap.General.StartTime = StartTime;

            _beatmap.Save(tempFolder, beatmapFolder, overwrite, true);
        }
    }
}
