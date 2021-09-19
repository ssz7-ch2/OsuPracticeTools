using OsuPracticeTools.Enums;

namespace OsuPracticeTools.Core.PracticeDiffs
{
    public class PracticeDiffSettings
    {
        public PracticeDiffsType PracticeDiffsType { get; set; } = PracticeDiffsType.Current;
        public IndexFormatType IndexType { get; set; } = IndexFormatType.AddOrder;
        public EndTimeType EndTimeType { get; set; } = EndTimeType.MapEnd;
        public int ExtendAmount { get; set; } = 0;
        public int? ComboAmount { get; set; } = null;
        public ComboType ComboType { get; set; } = ComboType.None;
        public int GapDuration { get; set; } = 1500;
        public int SliderDuration { get; set; } = 830;
        public int SkinComboColors { get; set; } = 4;
        public bool CirclesComboColor { get; set; } = false;
        public int Interval { get; set; } = 30;
        public int IntervalQuota { get; set; } = 1;
        public IntervalType IntervalType { get; set; } = IntervalType.HitObjects;
        public string BookmarksDiffLoad { get; set; } = null;
        public string BookmarksDiffSave { get; set; } = null;
        public bool BookmarksAdd { get; set; } = false;
    }
}
