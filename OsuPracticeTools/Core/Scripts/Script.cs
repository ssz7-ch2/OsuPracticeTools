using System;

namespace OsuPracticeTools.Core.Scripts
{
    public abstract class Script
    {
        public string ScriptString { get; }

        protected Script(string script)
        {
            ScriptString = script;
        }

        public abstract Type Run();
    }
}
