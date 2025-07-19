using System.Text;

namespace LabelsTG.Labels
{
    public static class Log
    {
        static readonly string defaultPath = Path.Combine(Configuration.ConfigPath, "log.txt");
        public static event Action<string> OnError;
        public static void Write(string message, string path = null, bool parse = true)
        {
            if (string.IsNullOrEmpty(message))
            {
                return; // Do not log empty messages
            }
            if (parse) message = Parse(message);

            if (string.IsNullOrEmpty(path))
            {
                path = defaultPath;
            }
            try
            {
                using StreamWriter sw = new(path, true);
                sw.WriteLine(message);
            }
            catch (Exception ex)
            {
                // Handle the error by invoking the OnError event
                OnError?.Invoke($"Error writing to log file: {ex.Message}");
            }
        }
        private static string Parse(string eplBody)
        {
            StringBuilder sb = new();
            foreach (var line in eplBody.Split('\n'))
            {
                if (line.Trim().StartsWith('P'))
                {
                    //
                    sb.Append($"{line.Trim().Substring(1)}\n");
                }
                else if (line.Contains('\"'))
                {
                    int start = line.IndexOf('\"') + 1;
                    int end = line.LastIndexOf('\"');
                    sb.Append($"{line.Substring(start, end - start)}\t");
                }
            }
            string[] output = sb.ToString().TrimEnd('\n').Split('\n');
            // Add timestamp to each line
            for (int i = 0; i < output.Length; i++)
            {
                output[i] = $"{DateTime.Now}\t{output[i]}";
            }
            return string.Join("\n", output);
        }
    }
}