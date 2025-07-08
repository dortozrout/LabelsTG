using Terminal.Gui;
using System.Runtime.InteropServices;
using System.Text;

namespace LabelsTG.Labels
{
    /// <summary>
    /// Static class that holds the configuration for the program.
    /// </summary>
    class Configuration
    {
        //Nazev aplikace ktery se zobrazi v hlavicce
        public const string AppName = "TG202506";
        public static readonly Encoding DefaultEncoding = Encoding.UTF8; //default encoding for reading files

        //Adresa konfig souboru
        public static string ConfigFile { get; set; }

        //Cesta ke konf. adresari
        public static readonly string ConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TiskStitku");

        //Cesta ke konfig souboru
        //public static string ConfigFilePath => Path.Combine(ConfigPath, ConfigFile);

        //Adresář se soubory obsahujícími EPL příkazy
        public static string TemplatesDirectory { get; private set; } = ConfigPath;
        //Vyraz pro vyhledani jednoho nebo nekolika epl prikazu v adresari
        public static string SearchedText { get; private set; } = string.Empty;
        //Tisk pouze jednoho souboru?
        public static bool PrintOneFile { get; private set; }
        //Opakovany tisk 1 souboru?
        public static bool Repeate { get; private set; }
        //Kodovani souborů
        private static Encoding _eplFileEncoding = SetupEncoding("UTF-8");
        public static Encoding EplFileEncoding
        {
            get => _eplFileEncoding;
            set => _eplFileEncoding = value;
        }
        //ip adresa nebo jméno tiskárny
        public static string PrinterAddress { get; private set; } = string.Empty;
        //typ tiskarny - lokalni, sdilena nebo sitova
        public static int PrinterType { get; private set; } = 3; //defaultne 3 - vystup na obrazovku
        public static string PrinterTypeByWords { get; private set; }
        //jestli vyzadovat zadani login
        public static bool Login { get; private set; }
        //uzivatelske jmeno
        public static string User { get; set; }
        //adresa souboru s daty expirace, sarze atd.
        public static string PrimaryDataAdress { get; private set; }
        //adresa hlavni sablony (pro tisk v rezimu jedne sablony)
        public static string MasterTemplateAddress { get; private set; } = string.Empty;
        public static string MasterTemplateInputAddress { get; private set; } = string.Empty;
        //adresa log souboru
        public static string LogFile { get; private set; } = string.Empty;
        //kodovani pouzivane v epl prikazech "I8,B"
        public const string EplEncoding = "windows-1250";

        //format data v aplikaci
        public const string DateFormat = "yyyy-MM-dd";
        private static Color _userDefinedColor = SetupColor("Green", Color.Green);
        public static Color UserDefinedColor
        {
            get => _userDefinedColor;
            set => _userDefinedColor = value;
        }

        public static string Header { get; private set; }

        //maximalni pocet stitku ktery lze vytisknout najednou
        public const int maxQuantity = 50;

        public static List<BaseItem> ConfigItems { get; private set; }

        internal static readonly char[] separator = ['\r', '\n'];

        static Configuration()
        {
            ConfigItems =
            [
                new ConfigItem<string>("ConfigFile", "", "", true, () => ConfigFile, (value) => ConfigFile = value),
                new ConfigItem<string>("IPTiskarny", "","IP adresa nebo jmeno tiskarny", false, () => PrinterAddress, (value) => PrinterAddress = value),
                new ConfigItem<int>("TypTiskarny","3","typ tiskarny 0 - sdilena, 1 - mistni, 2 - sitova, 3 - výstup na obrazovku",false, () => PrinterType, (value) => PrinterType = value),
                new ConfigItem<string>("Adresar", ConfigPath, "adresar souboru s epl prikazy", false, () => TemplatesDirectory, (value) => TemplatesDirectory = value),
                new ConfigItem<string>("HledanyText", "", "text ktery se hleda v nazvu souboru", false, () => SearchedText, (value) => SearchedText = value),
                new ConfigItem<bool>("JedenSoubor", "false", "jestli se ma tisknout jenom jeden soubor", false, () => PrintOneFile, (value) => PrintOneFile = value),
                new ConfigItem<Encoding>("Kodovani", "UTF-8", "kodovani ulozenych souboru (UTF-8 nebo windows-1250)", false, () => EplFileEncoding, (value) => EplFileEncoding = value),
                new ConfigItem<bool>("Prihlasit", "false", "zda vyzadovat login", false, () => Login, (value) => Login = value),
                new ConfigItem<string>("Data", "", "adresa souboru s primarnimi daty", true, () => PrimaryDataAdress, (value) => PrimaryDataAdress = value,
                "# klíč: hodnota"),
                new ConfigItem<string>("HlavniSablona", "", "adresa sablony pro tisk v modu jedne sablony", true, () => MasterTemplateAddress, (value) => MasterTemplateAddress = value,
                "N\n" +
                "I8,B\n" +
                "A110,0,0,4,1,2,N,\"<name>\"\n" +
                "A110,57,0,2,1,1,N,\"lot: <sarze\"\n" +
                "A315,0,1,1,1,1,N,\"<date>\"\n" +
                "A110,82,0,2,1,1,N,\"exp: <date+<trvanlivost>|<expirace>>\"\n" +
                "A345,10,1,3,1,1,N,\"<uzivatel>\"\n" +
                "P<pocet|<pocetstitku>>\n"),
                new ConfigItem<string>("HlavniSablonaData", "", "adresa souboru se vstupnimi daty pro tisk pomoci hlavni sablony", true, () => MasterTemplateInputAddress, (value) => MasterTemplateInputAddress = value,
                "keys: "),
                new ConfigItem<string>("Logsoubor", "", "umisteni logovaciho souboru", true, () => LogFile, (value) => LogFile = value),
                new ConfigItem<Color>("Barva", "Green", "nastaveni barevneho zvyrazneni (Red, Blue atd., nebo 0 - 15, vychozi = Green)", false, () => UserDefinedColor, (value) => UserDefinedColor = value),
            ];
            Header = string.Empty;
        }

        //metoda slouzici pro nacteni konfigurace
        public static int Load(string configFile) //jmeno konfig souboru v adresari %appdata%/TiskStitku
        {
            ConfigFile = Path.Combine(ConfigPath, configFile); //nastavi cestu ke konfig souboru
            int result;
            try //pokusi se vytvorit adresar CCData v %appdata%, pokud neexistuje
            {
                if (!Directory.Exists(ConfigPath))
                    Directory.CreateDirectory(ConfigPath);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Konfigurace", ex);
            }
            //string fullPath = Path.Combine(ConfigPath, configFile);

            if (File.Exists(ConfigFile)) //pokud konfig soubor existuje
            {
                result = LoadConfigFile(ConfigFile); //nacte ho
            }
            else //pokud konfig soubor neexituje, vytvori novy
            {
                result = CreateDefaultConfigFile(ConfigFile);
                if (result == 0)
                {
                    result = LoadConfigFile(ConfigFile); //a nacte ho
                }
            }
            Initialize();
            return result;
        }

        /// <summary>
        /// Loads the configuration from the specified file.
        /// </summary>
        /// <param name="configFilePath">The full path to the configuration file.</param>
        /// <returns>Returns 0 if successful, 1 if an error occurred.</returns>
        private static int LoadConfigFile(string configFilePath)
        {
            int result = 0;

            try
            {
                string configFileContent = File.ReadAllText(configFilePath);
                LoadFromContent(configFileContent);
            }
            catch (ArgumentException)
            {
                ErrorHandler.HandleError("Configuration", new ArgumentException("Zkontroluj nastaveni adresare v konfig souboru!"));
                result = 1;
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Configuration", ex);
                result = 1;
            }

            return result;
        }

        /// <summary>
        /// Creates a default configuration file.
        /// </summary>
        /// <param name="configFilePath">The full path to the configuration file.</param>
        /// <returns>Returns 0 if successful, 1 if an error occurred.</returns>
        private static int CreateDefaultConfigFile(string configFilePath)
        {
            int result = 0;
            try
            {
                SaveConfiguration();
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError("Configuration", ex);
                result = 1;
            }
            return result;
        }
        private static void Initialize()
        {
            switch (PrinterType)
            {
                case 0:
                    PrinterTypeByWords = "sdílená tiskárna";
                    break;
                case 1:
                    PrinterTypeByWords = "lokální tiskárna";
                    break;
                case 2:
                    PrinterTypeByWords = "síťová tiskárna";
                    break;
                case 3:
                    PrinterTypeByWords = "výstup na obrazovku";
                    break;
                default:
                    PrinterType = 3;
                    PrinterTypeByWords = "výstup na obrazovku";
                    break;
            }
        }
        public static Terminal.Gui.Color SetupColor(string color, Terminal.Gui.Color defaultColor)
        {
            return Enum.TryParse(color, true, out Terminal.Gui.Color parsedColor)
                ? parsedColor
                : defaultColor;
        }
        public static Encoding SetupEncoding(string encodingName)
        {
            try
            {
                return Encoding.GetEncoding(encodingName);
            }
            catch (ArgumentException)
            {
                // If the specified encoding is not valid, return the default encoding
                return DefaultEncoding;
            }
        }
        public static void LoadFromContent(string content)
        {
            try
            {
                // Split the content into lines
                string[] configLines = content.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                // Parse the lines into a dictionary
                var dict = configLines
                    .Where(line => !line.StartsWith('#') && line.Contains(':')) // Ignore comments and invalid lines
                    .Select(line => line.Split(':', 2)) // Split into key and value
                    .ToDictionary(x => x[0].Trim(), x => x[1].Trim(), StringComparer.OrdinalIgnoreCase);
                if (!dict.ContainsKey("ConfigFile"))
                    dict.Add("ConfigFile", ConfigFile);
                // Update the configuration items
                foreach (var item in ConfigItems)
                {
                    var itemType = item.GetType().GetGenericArguments()[0];
                    var keyProp = item.GetType().GetProperty("Key");
                    var valueProp = item.GetType().GetProperty("Value");

                    var key = (string)keyProp.GetValue(item);
                    if (dict.TryGetValue(key, out var strValue))
                    {
                        object typedValue;
                        if (itemType == typeof(Terminal.Gui.Color))
                        {
                            typedValue = SetupColor(strValue, Terminal.Gui.Color.Green);
                        }
                        else if (itemType == typeof(Encoding))
                        {
                            typedValue = SetupEncoding(strValue);
                        }
                        else if (itemType == typeof(bool))
                        {
                            typedValue = bool.TryParse(strValue, out var boolValue) && boolValue;
                        }
                        else if (itemType == typeof(int))
                        {
                            typedValue = int.TryParse(strValue, out var intValue) ? intValue : 0;
                        }
                        else if (itemType == typeof(string))
                        {
                            // For string, check if it has a DefaultValue property
                            var defaultValueProp = item.GetType().GetProperty("DefaultValue");
                            // If it has a DefaultValue property, use it if the string value is empty
                            var defaultValue = defaultValueProp?.GetValue(item);
                            typedValue = string.IsNullOrEmpty(strValue) ? defaultValue : strValue;
                            // If the item has an IsFile property, check if it's a file path
                            if (item.GetType().GetProperty("IsFile")?.GetValue(item) is bool isFile && isFile)
                            {
                                // If it's a file path, ensure it exists
                                if (!string.IsNullOrEmpty(typedValue as string) && File.Exists(typedValue as string))
                                {
                                    // Read the content of the file and set it to the Content property
                                    string contentOfFile = File.ReadAllText(typedValue as string);
                                    item.GetType().GetProperty("Content")?.SetValue(item, contentOfFile);
                                }
                            }
                        }
                        else
                        {
                            typedValue = Convert.ChangeType(strValue, itemType);
                        }
                        valueProp.SetValue(item, typedValue);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to parse configuration content.", ex);
            }
        }
        public static void SaveConfiguration()
        {
            try
            {
                var lines = new List<string>(){
                    "# Configuration file for LabelsTG",
                    "# Configuration file path: " + ConfigFile,
                    "# Last modified: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine,
                };
                foreach (dynamic item in ConfigItems)
                {
                    string comment = item.Description;
                    if (!string.IsNullOrEmpty(comment))
                    {
                        lines.Add($"# {comment}");
                    }
                    string key = item.Key;
                    if (key == "ConfigFile")
                    {
                        // Skip ConfigFile key as it is not saved in the file
                        continue;
                    }
                    string value;
                    if (item is ConfigItem<Encoding> encodingItem)
                    {
                        // Use WebName for encoding
                        value = encodingItem.Value.WebName;
                        lines.Add($"{key}:{value}");
                        continue;
                    }
                    value = item.Value == null ? "" : item.Value.ToString();
                    lines.Add($"{key}:{value}");
                }
                File.WriteAllLines(ConfigFile, lines);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save configuration.", ex);
            }
        }
        /// <summary>
        /// Gets the content of a configuration file item by its key.
        /// </summary>
        /// <param name="key">The key of the configuration item.</param>
        public static string GetConfigFileContent(string key)
        {
            var item = ConfigItems.OfType<ConfigItem<string>>().FirstOrDefault(i => i.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
            return item.Content?.ToString() ?? string.Empty;
        }
    }
}