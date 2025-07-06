using System.Text;
using System.Text.RegularExpressions;

namespace LabelsTG.Labels
{
    public class EplFileLoader
    {
        public List<EplFile> LoadFiles(string directoryPath, string filter = null)
        {
            var eplFiles = new List<EplFile>();

            try
            {
                var files = Directory.GetFiles(directoryPath);
                foreach (var file in files)
                {
                    string content = File.ReadAllText(file, Configuration.EplFileEncoding);
                    var eplFile = new EplFile(Path.GetFileName(file), Path.GetFullPath(file), content);
                    eplFiles.Add(eplFile);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(this, ex);
            }
            if (filter != null)
            {
                eplFiles = eplFiles.FindAll(e => e.Key.Contains(filter, StringComparison.CurrentCultureIgnoreCase));
            }
            return eplFiles;
        }
        public List<EplFile> ReadFromFile(string filePath, string templatePath, string searchedText = "")
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(templatePath))
            {
                return [];
            }
            var eplFiles = new List<EplFile>();
            try
            {
                var fileLines = File.ReadAllLines(filePath);
                string template = File.ReadAllText(templatePath);
                //List<string> keys;
                string[] keys = [];
                string[] values;
                foreach (string line in fileLines)
                {
                    if (!line.StartsWith(';') && !line.StartsWith('─') && !line.StartsWith('#') && line != string.Empty)
                    {
                        if (line.StartsWith("keys"))
                        {
                            int indexOfSeparator = line.IndexOf(':');
                            keys = Split(line[(indexOfSeparator + 1)..].Trim());
                        }
                        else
                        {
                            // values = line.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            values = Split(line);
                            if (keys.Length == values.Length)
                            {
                                string templateCopy = template;
                                //Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                                for (int i = 0; i < keys.Length; i++)
                                {
                                    templateCopy = templateCopy.Replace(keys[i], values[i]);
                                    //keyValuePairs.Add(keys[i], values[i]);
                                }
                                eplFiles.Add(new EplFile(values[0], "", templateCopy));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(this, ex);
            }
            if (!string.IsNullOrWhiteSpace(searchedText)) return eplFiles.FindAll(epl => epl.Key.Contains(searchedText, StringComparison.CurrentCultureIgnoreCase));
            return eplFiles;
        }
        public static string[] Split(string input)
        {
            // Regular expression to match quoted text or words separated by spaces
            string pattern = @"(?<=\s|^)(\""[^\""]*\""|\S+)(?=\s|$)";

            // Perform the regex match
            var matches = Regex.Matches(input, pattern);

            // Convert matches to an array of strings
            string[] resultArray = matches.Cast<Match>()
                                          .Select(match => match.Value.Trim('"')) // Optional: trim the quotes
                                          .ToArray();
            return resultArray;
        }
        public static List<BaseItem> LoadSettings(List<object> configItems)
        {
            string content = File.ReadAllText(Configuration.ConfigFilePath);
            var confFile = new ConfigItem<string>("ConfigFile", "", "", true, () => Path.Combine(Configuration.ConfigPath, Configuration.ConfigFile), (value) => { Configuration.ConfigFile = value; }, "", content);
            var settingsFiles = new List<BaseItem> { confFile };
            foreach (var conf in configItems)
            {
                if (conf is ConfigItem<string> configItem && configItem.IsFile)
                {
                    string fileAddress = configItem.Value;
                    if (File.Exists(fileAddress))
                    {
                        string contentFile = File.ReadAllText(fileAddress, Configuration.EplFileEncoding);
                        configItem.Content = contentFile;
                        settingsFiles.Add(configItem);
                    }
                    else
                    {
                        settingsFiles.Add(configItem);
                    }
                }
                else
                {
                    settingsFiles.Add(conf as BaseItem);
                }
            }
            return settingsFiles;
        }
    }
}
