namespace OsuPracticeTools.Helpers.BeatmapHelpers
{
    public static class BeatmapDifficulty
    {
        public static float ApplyRateChangeOD(float difficulty, double rate) => ApplyRateChange(difficulty, rate, 79.5f, 49.5f, 19.5f);
        public static float ApplyRateChangeAR(float difficulty, double rate) => ApplyRateChange(difficulty, rate, 1800, 1200, 450);
        public static float DifficultyRange(float difficulty, float min, float mid, float max)
        {
            if (difficulty > 5)
                return mid + (max - mid) * (difficulty - 5) / 5;
            if (difficulty < 5)
                return mid - (mid - min) * (5 - difficulty) / 5;

            return mid;
        }
        public static float DifficultyRangeReverse(float ms, float min, float mid, float max)
        {
            if (ms < mid)
                return 5 * (ms - mid) / (max - mid) + 5;
            if (ms > mid)
                return 5 - 5 * (mid - ms) / (mid - min);

            return 5;
        }
        public static float ApplyRateChange(float difficulty, double rate, float min, float mid, float max) => DifficultyRangeReverse((float)(DifficultyRange(difficulty, min, mid, max) / rate), min, mid, max);
    }
}
