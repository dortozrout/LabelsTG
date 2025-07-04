using System.Globalization;
using System.Windows.Markup;
using LabelsTG.Labels;
namespace LabelsTG
{
    public class Model
    {
        public List<EplFile> EplFiles { get; set; } = [];
        public List<BaseItem> SettingsFiles { get; set; }
        public List<ConfigItem<string>> NewSettingsFiles { get; set; }
        public List<ConfigItem<string>> SettingsFilesAndDirs { get; set; }
        public EplFile SelectedEplFile { get; set; }
        public event Action<string> OnFilePrintToScreen;

        public Model()
        {
            // Loads EPL files from directory or from a master template file, depending on configuration.
            if (string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
            {
                EplFiles = Configuration.SearchedText == "" ?
                   new EplFileLoader().LoadFiles(Configuration.TemplatesDirectory) :
                   new EplFileLoader().LoadFiles(Configuration.TemplatesDirectory, Configuration.SearchedText);
            }
            else // Loads from file defined in Configuration.MasterTemplateInputAddress
                EplFiles = new EplFileLoader().ReadFromFile(Configuration.MasterTemplateInputAddress, Configuration.MasterTemplateAddress, Configuration.SearchedText);

            // Loads settings files and prepares filtered lists for new files and files/directories.
            SettingsFiles = EplFileLoader.LoadSettings(Configuration.ConfigItems);
            NewSettingsFiles = SettingsFiles
                .OfType<ConfigItem<string>>()
                .Where(configItem => configItem.IsFile && string.IsNullOrEmpty(configItem.Value))
                .ToList();
            SettingsFilesAndDirs = SettingsFiles
                .OfType<ConfigItem<string>>()
                .Where(configItem => (configItem.IsFile || configItem.Key == "Adresar") && configItem.Key != "ConfigFile")
                .ToList();
        }

        /// <summary>
        /// Returns the list of loaded EPL files.
        /// </summary>
        public List<EplFile> GetEplFiles()
        {
            return EplFiles;
        }

        /// <summary>
        /// Adds a new EPL file to the list.
        /// </summary>
        public void AddEplFile(EplFile eplFile)
        {
            EplFiles.Add(eplFile);
        }

        /// <summary>
        /// Prints the given EPL file. If printer type is 3, prints to screen; otherwise, sends to printer and logs if needed.
        /// </summary>
        public void PrintEplFile(EplFile eplFile = null)
        {
            if (eplFile.Print)
            {
                if (Configuration.PrinterType == 3)
                {
                    OnFilePrintToScreen?.Invoke(eplFile.Body);
                }
                else
                {
                    Printer.PrintLabel(eplFile.Body);
                    if (!string.IsNullOrWhiteSpace(Configuration.LogFile)) Log.Write(eplFile.Body, Configuration.LogFile);
                }
            }
        }

        /// <summary>
        /// Saves the given item (ConfigItem or EplFile) to disk.
        /// </summary>
        public static void SaveFile(BaseItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Key))
            {
                //ErrorHandler.HandleError("Model", new InvalidOperationException("Item or item key is null or empty."));
                // return -1;
                throw new InvalidOperationException("Item or item key is null or empty.");
            }

            try
            {
                switch (item)
                {
                    case ConfigItem<string> configItem when configItem.IsFile:
                        File.WriteAllText(configItem.Value, configItem.Content);
                        return;

                    case EplFile eplFile:
                        File.WriteAllText(eplFile.FileAddress, eplFile.Template);
                        return ;

                    default:
                        //ErrorHandler.HandleError("Model", new InvalidOperationException("Unknown item type."));
                        // return -1;
                        throw new InvalidOperationException("Unknown item type.");
                }
            }
            catch (Exception ex)
            {
                //View.ShowError($"Error saving file: {ex.Message}");
                //ErrorHandler.HandleError("Model", ex);
                // return -1;
                throw new InvalidOperationException(ex.Message);
            }
        }

        /// <summary>
        /// Deletes the given item (ConfigItem or EplFile) from disk and updates internal lists.
        /// </summary>
        public void DeleteFile(BaseItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.Key))
            {
                throw new InvalidOperationException("Item or item key is null or empty.");
            }

            try
            {
                switch (item)
                {
                    case ConfigItem<string> configItem when configItem.IsFile:
                        File.Delete(configItem.Value);
                        configItem.Value = "";
                        configItem.Content = "";
                        return;

                    case EplFile eplFile:
                        File.Delete(eplFile.FileAddress);
                        EplFiles.Remove(eplFile);
                        return;

                    default:
                        throw new InvalidOperationException("Unknown item type.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(ex.Message);
            }
        }

        /// <summary>
        /// Returns the list of loaded settings files.
        /// </summary>
        public List<BaseItem> GetSettingsFiles()
        {
            return SettingsFiles;
        }

        /// <summary>
        /// Reloads the settings files and updates the filtered lists for new files.
        /// </summary>
        public void UpdateSettingsFiles()
        {
            SettingsFiles = EplFileLoader.LoadSettings(Configuration.ConfigItems);
            NewSettingsFiles = SettingsFiles
                .OfType<ConfigItem<string>>()
                .Where(configItem => configItem.IsFile && string.IsNullOrEmpty(configItem.Value))
                .ToList();
        }
    }
}