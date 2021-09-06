using System;

namespace OsuPracticeTools.Core.Scripts.PracticeDiffScripts
{
    public class ClearDiffsScript : Script
    {
        public ClearDiffsScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            Info.DiffTimes.Clear();
            return typeof(ClearDiffsScript);
        }
    }
}
