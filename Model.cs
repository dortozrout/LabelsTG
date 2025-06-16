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
            //nacteni epl prikazu z adresare
            if (string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
            {
                EplFiles = Configuration.SearchedText == "" ?
                   new EplFileLoader().LoadFiles(Configuration.TemplatesDirectory) :
                   new EplFileLoader().LoadFiles(Configuration.TemplatesDirectory, Configuration.SearchedText);
            }
            else //ze souboru definovaneho v Configuration.MasterTemplateInputAddress
                EplFiles = new EplFileLoader().ReadFromFile(Configuration.MasterTemplateInputAddress, Configuration.MasterTemplateAddress, Configuration.SearchedText);

            //nacteni nastaveni
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
        public List<EplFile> GetEplFiles()
        {
            return EplFiles;
        }
        public void AddEplFile(EplFile eplFile)
        {
            EplFiles.Add(eplFile);
        }
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
        public static int SaveFile(BaseItem item)
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
                        File.WriteAllText(configItem.Value, configItem.Content);
                        return 0;

                    case EplFile eplFile:
                        File.WriteAllText(eplFile.FileAddress, eplFile.Template);
                        return 0;

                    default:
                        throw new InvalidOperationException("Unknown item type.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Error saving file: \n" + ex.Message);
            }
        }
        public int DeleteFile(BaseItem item)
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
                        return 0;

                    case EplFile eplFile:
                        File.Delete(eplFile.FileAddress);
                        EplFiles.Remove(eplFile);
                        return 0;

                    default:
                        throw new InvalidOperationException("Unknown item type.");
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleError(this, ex);
                return -1;
            }
        }
        public List<BaseItem> GetSettingsFiles()
        {
            return SettingsFiles;
        }
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