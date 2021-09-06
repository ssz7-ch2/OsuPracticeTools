using System;
using System.Linq;

namespace OsuPracticeTools.Core.Scripts.PracticeDiffScripts
{
    public class EndDiffScript : Script
    {
        public EndDiffScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            if (Info.DiffTimes.Any())
                Info.DiffTimes[^1][1] = Info.CurrentPlayTime;
            else
                return null;

            return typeof(EndDiffScript);
        }
    }
}
