using OsuPracticeTools.Enums;

namespace OsuPracticeTools.Objects
{
    public class ScriptOptions
    {
        public ScriptDiffsType ScriptDiffsType { get; set; } = ScriptDiffsType.Current;
        public int Interval { get; set; } = 20;
        public double SpeedRate { get; set; } = 1;
        public bool Pitch { get; set; } = false;
        public double? BPM { get; set; } = null;
        public bool HardRock { get; set; } = false;
        public FlipDirection? FlipDirection { get; set; } = null;
        public bool RemoveSpinners { get; set; } = false;
        public float? CS { get; set; }
        public float? AR { get; set; }
        public float? OD { get; set; }
        public float? HP { get; set; }
        public float? MinCS { get; set; }
        public float? MaxCS { get; set; }
        public float? MinAR { get; set; }
        public float? MaxAR { get; set; }
        public float? MinOD { get; set; }
        public float? MaxOD { get; set; }
        public bool DifficultyModified { get; set; } = false;
        public string NameFormat { get; set; } = "{v}";
        public PracticeDiffOptions PracticeDiffOptions { get; set; } = new();
        public AudioProcessor AudioProcessor { get; set; } = AudioProcessor.Bass;
    }
}
