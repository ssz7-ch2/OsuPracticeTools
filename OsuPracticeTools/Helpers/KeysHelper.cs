using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace OsuPracticeTools.Helpers
{
    public static class KeysHelper
    {
        public static Keys Parse(string s)
        {
            Keys key;
            if (char.IsNumber(s[0]))
            {
                Enum.TryParse("D" + s[0], true, out key);
                return key;
            }

            Enum.TryParse(s, true, out key);
            return key;
        }

        public static void AddUnique(this List<List<Keys>> keys, List<Keys> newKey)
        {
            if (!keys.Any(newKey.SequenceEqual))
                keys.Add(newKey);
        }
    }
}
