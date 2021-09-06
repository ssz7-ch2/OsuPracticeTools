using System;

namespace OsuPracticeTools.Helpers
{
    public static class StringHelper
    {
        public static int Similarity(this string a, string b)
        {
            var len = Math.Min(a.Length, b.Length);

            var count = 0;
            for (int i = 0; i < len; i++)
            {
                if (a[i] == b[i])
                    count++;
            }

            return count;
        }
    }
}
