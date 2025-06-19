namespace LabelsTG.Labels
{
    public class Parser
    {
        private readonly Dictionary<string, string> primaryData;
        private EplFile CurrentEplFile { get; set; }
        //private Model Model { get; set; }
        private bool continueProcessing;
        public event Action<string> OnTemplateSave;
        public Parser()
        {
            // Load primary data from the specified address
            primaryData = LoadPrimaryData();
        }

        private Dictionary<string, string> LoadPrimaryData()
        {
            var rv = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(Configuration.PrimaryDataAdress)) return rv;

            try
            {
                string[] dataArray = File.ReadAllLines(Configuration.PrimaryDataAdress);
                foreach (string line in dataArray)
                {
                    int indexOfSeparator = line.IndexOf(':');
                    if (!line.StartsWith('#') && indexOfSeparator > 0)
                    {
                        string key = line[..indexOfSeparator].Trim().ToLower();
                        string value = line[(indexOfSeparator + 1)..].Trim();
                        rv.Add(key, value);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(this, ex);
            }

            return rv;
        }

        public void Process(ref EplFile eplFile)
        {
            continueProcessing = true;
            CurrentEplFile = eplFile;
            eplFile.Print = true;
            eplFile.Body = FillOutTemplate(eplFile.Template);
        }

        private string FillOutTemplate(string template)
        {
            template = RemoveCommentedLines(template);
            if (template.Trim().EndsWith('P'))
            {
                template = string.Format("{0}<pocet>{1}", template.TrimEnd(), Environment.NewLine);
            }

            List<string> keys = ReadKeys(template);

            // Check if there's a sequence key, and handle it
            string sequenceKey = keys.FirstOrDefault(k => k.StartsWith("<sequence|"));

            if (sequenceKey != null)
            {
                // If a sequence key exists, handle the sequence and other keys
                return HandleSequenceAndOtherKeys(template, keys, sequenceKey);
            }

            // Otherwise, replace keys normally
            return ReplaceAllKeys(template, keys);
        }

        private string HandleSequenceAndOtherKeys(string template, List<string> keys, string sequenceKey)
        {
            // Parse the sequence start and count from the key
            var keyParts = sequenceKey.Trim('<', '>').Split('|');
            if (keyParts.Length < 3 || keyParts.Length > 5)
            {
                // Handle the error for invalid sequence key format
                View.ShowError($"Chybný formát sekvence klíče ({sequenceKey})! Zkontroluj formát klíče a zkus to znovu.");
                //new NotificationForm("Chyba v klíči sequence", $"Chybný formát seqence klíče ({sequenceKey})!").Display();
                //Console.ReadKey();
            }

            int start = HandleInput<int>(CurrentEplFile, "Zadej počátek sekvence: ", keyParts[1]);
            if (!continueProcessing) return string.Empty;
            int count = HandleInput<int>(CurrentEplFile, "Zadej počet kroků: ", keyParts[2]);
            string format;
            if (sequenceKey.Contains("save", StringComparison.CurrentCultureIgnoreCase))
            {
                string newSequenceKey = $"<sequence|{start + count}|{keyParts[2]}";

                // If there are additional parts to the sequence key, append them
                for (int i = 3; i < keyParts.Length; i++)
                {
                    newSequenceKey += $"|{keyParts[i]}";
                }
                newSequenceKey += ">";

                string newTemplate = CurrentEplFile.Template.Replace(sequenceKey, newSequenceKey);
                // Save the new template to the file
                OnTemplateSave?.Invoke(newTemplate);
                //Model.SaveTemplate(CurrentEplFile, newTemplate);
                format = keyParts.Length == 5 ? keyParts[4] : "";
            }
            else format = keyParts.Length == 4 ? keyParts[3] : "";

            // Generate the sequence of numbers
            var sequenceValues = Enumerable.Range(start, count).ToList();

            string result = string.Empty;

            // For each value in the sequence, replace the sequence key
            foreach (var sequenceValue in sequenceValues)
            {
                // Replace the sequence key with the current value
                string tempTemplate = template.Replace(sequenceKey, sequenceValue.ToString(format));

                // Append the updated template block to the result
                result += tempTemplate + Environment.NewLine;
            }
            // Replace other keys in the template
            result = ReplaceAllKeys(result, keys.Where(k => k != sequenceKey).ToList());
            return result;
        }

        private string ReplaceAllKeys(string template, List<string> keys)
        {
            foreach (var key in keys)
            {
                var keyValue = continueProcessing ? FindValue(key) : "";
                template = template.Replace(key, keyValue);
            }

            return template;
        }

        private List<string> ReadKeys(string template)
        {
            var rv = new List<string>();
            int start = template.IndexOf('<');
            int end = template.IndexOf('>');

            while (start != -1 && end != -1)
            {
                if (end < start) ErrorHandler.HandleError(this, new ArgumentOutOfRangeException("Invalid template format: '>' found before '<'."));
                string key = template.Substring(start, end - start + 1);
                rv.Add(key);
                start = template.IndexOf('<', end + 1);
                end = template.IndexOf('>', end + 1);
            }

            return rv.Distinct().ToList();
        }

        private string FindValue(string key)
        {
            try
            {
                return primaryData[key.Trim('<', '>').ToLower()];
            }
            catch (KeyNotFoundException)
            {
                return FindValueNotInPrimaryData(key);
            }
        }

        private string FindValueNotInPrimaryData(string key)
        {
            if (key == "<GS1>")
                return "\u001D";

            if (Configuration.Login && key == "<uzivatel>")
                return Configuration.User;

            if (key.StartsWith("<time"))
                return HandleTimeKey(key);

            if (key.StartsWith("<date"))
                return HandleDateKey(key);

            if (key.StartsWith("<pocet"))
                return HandlePocetKey(key);

            if (key.StartsWith("<number"))
                return HandleNumberKey(key);

            return HandleDefaultKey(key);
        }
        private T HandleInput<T>(EplFile eplFile, string prompt, string defaultValue)
        {
            string input = View.LaunchDialog(prompt, defaultValue);
            if (string.IsNullOrEmpty(input) || input == "0")
            {
                // Handle the case where the user cancels the input or enters an empty string
                continueProcessing = false;
                eplFile.Print = false;
                return default;
            }
            if (typeof(T) == typeof(int))
            {
                if (int.TryParse(input, out int intValue))
                    return (T)(object)intValue;
                else
                {
                    intValue = 0;
                    return (T)(object)intValue;
                }

            }
            else if (typeof(T) == typeof(DateTime))
            {
                if (DateTime.TryParse(input, out DateTime dateTimeValue))
                    return (T)(object)dateTimeValue;
                else
                    return (T)(object)default(DateTime);
            }
            else if (typeof(T) == typeof(string))
            {
                return (T)(object)input;
            }
            else
            {
                throw new InvalidOperationException($"Unsupported type: {typeof(T)}");
            }
        }

        private string HandleTimeKey(string key)
        {
            int indexOfPlus = key.IndexOf('+');
            if (indexOfPlus == -1)
                return DateTime.Now.ToString("H:mm");

            if (int.TryParse(key[(indexOfPlus + 1)..].TrimEnd('>'), out int drift))
                return DateTime.Now.AddMinutes(drift).ToString("H:mm");

            drift = HandleInput<int>(CurrentEplFile, "Zadej počet minut: ", "30");
            return DateTime.Now.AddMinutes(drift).ToString("H:mm");
        }

        private string HandleDateKey(string key)
        {
            // Extract the date format if specified, otherwise use the default format
            string format = key.Contains("format:")
            ? key[(key.IndexOf("format:") + 7)..].TrimEnd('>')
            : "dd.MM.yyyy";

            // Check if the key contains a drift value (e.g., <date+5>)
            int indexOfPlus = key.IndexOf('+');
            if (indexOfPlus == -1)
                return DateTime.Now.ToString(format);

            // Split the key into parts for further processing
            var keyParts = key.Trim('<', '>').Split(new[] { '+', '|' }, StringSplitOptions.None);

            // Remove the format part from the key parts if it exists
            if (keyParts.Last().StartsWith("format:"))
                keyParts = keyParts.Take(keyParts.Length - 1).ToArray();

            // Handle the "exp" keyword for expiration
            if (keyParts[1] == "exp")
                return "expirace";

            // Parse the drift value and calculate the bottle expiration date
            if (int.TryParse(keyParts[1], out int drift))
            {
                DateTime bottleExpiration = DateTime.Now.AddDays(drift);

                // If no additional parts, return the bottle expiration date
                if (keyParts.Length == 2)
                    return bottleExpiration.ToString(format);

                // Handle the case where a lot expiration key is provided
                if (keyParts.Length == 3)
                {
                    DateTime lotExpiration = GetLotExpiration(keyParts[2]);

                    // Check if the lot expiration date is in the past
                    if (lotExpiration < DateTime.Today)
                    {
                        View.ShowInfo($"Datum expirace materiálu ({lotExpiration:dd.MM.yyyy}) je v minulosti. Štítky nebudou vytištěny! Zkontroluj případně uprav expiraci...");
                        continueProcessing = false;
                        CurrentEplFile.Print = false;
                        return string.Empty;
                    }

                    // Warn if the lot expiration date is within one month
                    if (lotExpiration < DateTime.Today.AddMonths(1))
                    {
                        View.ShowInfo($"Datum expirace materiálu ({lotExpiration:dd.MM.yyyy}) je menší než 1 měsíc. Zkontroluj případně uprav expiraci...");
                    }

                    // Return the earlier of the bottle expiration and lot expiration dates
                    DateTime dateToPrint = bottleExpiration < lotExpiration ? bottleExpiration : lotExpiration;
                    return dateToPrint.ToString(format);
                }
            }

            // Return an empty string if the key format is invalid
            return string.Empty;
        }
        private DateTime GetLotExpiration(string key)
        {
            DateTime lotExpiration;
            if (key == "0")
            {
                return DateTime.MaxValue;
            }
            if (DateTime.TryParse(key, out lotExpiration))
            {
                return lotExpiration;
            }
            if (primaryData.TryGetValue(key.ToLower(), out string lotExpirationStr)
                    && DateTime.TryParse(lotExpirationStr, out lotExpiration))
            {
                return lotExpiration;
            }
            lotExpiration = HandleInput<DateTime>(CurrentEplFile, "Zadej expiraci: ", DateTime.MaxValue.ToString("dd.MM.yyyy"));
            return lotExpiration;
        }

        private string HandlePocetKey(string key)
        {
            int quantity;
            int indexOfSeparator = key.IndexOf('|');
            // if (indexOfSeparator < 1)
            // {
            //     quantity = HandleInput<int>(CurrentEplFile, "Zadej počet štítků: ", "1");
            // }
            if (int.TryParse(key.Substring(indexOfSeparator + 1).TrimEnd('>'), out quantity))
            {
                quantity = HandleInput<int>(CurrentEplFile, "Zadej počet štítků", quantity.ToString());
            }
            else quantity = HandleInput<int>(CurrentEplFile, "Zadej počet štítků", "1");
            quantity = quantity > Configuration.maxQuantity ? Configuration.maxQuantity : quantity;
            return quantity.ToString();
        }

        private string HandleNumberKey(string key)
        {
            // <number|text|format:format>
            // Example: <number|serial|format:0000>
            string[] parts = key.Trim('<', '>').Split('|');
            if (parts.Length < 2)
            {
                View.ShowError($"Wrong key format ({key})! Check the key format and try again.");
                return string.Empty;
            }

            string prompt = parts[1];
            string format = "";

            // Check for format: in the third part
            if (parts.Length == 3 && parts[2].StartsWith("format:"))
            {
                format = parts[2]["format:".Length..];
            }

            if (int.TryParse(prompt, out int number))
            {
                // If the second part is a valid number, return it formatted
                return string.IsNullOrEmpty(format) ? number.ToString() : number.ToString(format);
            }
            else
            {
                // Prompt the user for input
                number = HandleInput<int>(CurrentEplFile, "Zadej " + prompt + ": ", "");
                return string.IsNullOrEmpty(format) ? number.ToString() : number.ToString(format);
            }
        }
        private string HandleDefaultKey(string key)
        {
            string rv = HandleInput<string>(CurrentEplFile, "Zadej " + key.Trim('<', '>'), "");
            return rv;
        }
        private string RemoveCommentedLines(string input)
        {
            // Split the input string into lines
            var lines = input.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            // Filter out lines that start with '#', after trimming leading whitespace
            var filteredLines = lines.Where(line => !line.TrimStart().StartsWith("#"));

            // Join the filtered lines back into a single string
            return string.Join(Environment.NewLine, filteredLines);
        }
    }
}
