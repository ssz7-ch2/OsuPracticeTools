using System;
using System.Linq;

namespace OsuPracticeTools.Core.Scripts.PracticeDiffScripts
{
    public class DeleteDiffScript : Script
    {
        public DeleteDiffScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            if (Info.DiffTimes.Any())
                Info.DiffTimes.RemoveAt(Info.DiffTimes.Count - 1);
            else
                return null;

            return typeof(DeleteDiffScript);
        }
    }
}
