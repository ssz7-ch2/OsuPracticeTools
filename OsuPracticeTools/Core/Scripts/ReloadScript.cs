using System;

namespace OsuPracticeTools.Core.Scripts
{
    public class ReloadScript : Script
    {
        public ReloadScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            Program.ReloadHotkeys(null, EventArgs.Empty);
            return typeof(ReloadScript);
        }
    }
}
