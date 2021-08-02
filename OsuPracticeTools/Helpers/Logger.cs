using System;
using System.IO;

namespace OsuPracticeTools.Helpers
{
    public static class Logger
    {
        private const string FILE_PATH = "logs.txt";

        public static void LogError(Exception ex)
        {
            using (var writer = new StreamWriter(FILE_PATH, true))
            {
                var header = $"----------------------------------------Date: {DateTime.Now}----------------------------------------";
                writer.WriteLine(header);
                writer.WriteLine();

                while (ex != null)
                {
                    writer.WriteLine(ex.GetType().FullName);
                    writer.WriteLine("Message : " + ex.Message);
                    writer.WriteLine("StackTrace : " + ex.StackTrace);

                    ex = ex.InnerException;
                }
                writer.WriteLine();
                writer.WriteLine($"{new string('-', (header.Length - 3) / 2)}End{new string('-', (header.Length - 3) - (header.Length - 3) / 2)}");
                writer.WriteLine();
            }
        }
        public static void LogMessage(string message)
        {
            using (var writer = new StreamWriter(FILE_PATH, true))
            {
                var header = $"----------------------------------------Date: {DateTime.Now}----------------------------------------";
                writer.WriteLine(header);
                writer.WriteLine();
                writer.WriteLine(message);
                writer.WriteLine();
                writer.WriteLine($"{new string('-', (header.Length - 3) / 2)}End{new string('-', (header.Length - 3) - (header.Length - 3) / 2)}");
                writer.WriteLine();
            }
        }
    }
}
