using OsuPracticeTools.Core.Scripts.Helpers;
using System;

namespace OsuPracticeTools.Core.Scripts
{
    public class SetSettingsScript : ScriptWithSettings
    {
        public SetSettingsScript(string script) : base(script)
        {
        }

        public override Type Run()
        {
            base.Run();

            ScriptHelper.CopySettings(Info.GlobalSettings, Settings);

            return typeof(SetSettingsScript);
        }
    }
}
