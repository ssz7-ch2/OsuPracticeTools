﻿using OsuPracticeTools.Enums;

namespace OsuPracticeTools.Objects
{
    public class PracticeDiffOptions
    {
        public string NameFormat { get; set; } = "{v}";
        public IndexFormatType IndexType { get; set; } = IndexFormatType.AddOrder;
        public EndTimeType EndTimeType { get; set; } = EndTimeType.MapEnd;
        public ComboType ComboType { get; set; } = ComboType.None;
        public int GapDuration { get; set; } = 1500;
        public int SliderDuration { get; set; } = 830;
    }
}