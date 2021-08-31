using OsuPracticeTools.Enums;

namespace OsuPracticeTools.Objects
{
    public class PracticeDiffSettings
    {
        public string NameFormat { get; set; } = "{v}";
        public IndexFormatType IndexType { get; set; } = IndexFormatType.AddOrder;
        public EndTimeType EndTimeType { get; set; } = EndTimeType.MapEnd;
        public int ExtendAmount { get; set; } = 0;
        public ComboType ComboType { get; set; } = ComboType.None;
        public int GapDuration { get; set; } = 1500;
        public int SliderDuration { get; set; } = 830;
        public int SkinComboColors { get; set; } = 4;
        public bool CirclesComboColor { get; set; } = false;
    }
}
