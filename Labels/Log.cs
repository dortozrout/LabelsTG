using System.Text;

namespace LabelsTG.Labels
{
    public static class Log
    {
        public static void Write(string message, string path = "log.txt")
        {
            message = Parse(message);
            try
            {
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    sw.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Log", ex);
            }
        }
        private static string Parse(string eplBody)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var line in eplBody.Split('\n'))
            {
                if (line.Trim().StartsWith("P"))
                {
                    sb.Append($"{line.Trim().Substring(1)}\n");
                }
                else if (line.Contains("\""))
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