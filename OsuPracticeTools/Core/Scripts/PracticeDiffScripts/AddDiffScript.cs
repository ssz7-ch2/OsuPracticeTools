using System;

namespace OsuPracticeTools.Core.Scripts.PracticeDiffScripts
{
    public class AddDiffScript : Script
    {
        public AddDiffScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            Info.SameMapDuration = 0;
            Info.DiffTimes.Add(new[] { Info.CurrentPlayTime, -1 });
            return typeof(AddDiffScript);
        }
    }
}
