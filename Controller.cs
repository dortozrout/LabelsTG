#nullable enable
using LabelsTG.Labels;
using Terminal.Gui;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace LabelsTG
{
    public class Controller
    {
        public View View { get; private set; }
        public Model Model { get; private set; }
        private string searchBuffer = "";
        public bool isSetting = false;
        private List<EplFile> currentListWievSource = [];
        private readonly string readmeUrl = "https://github.com/dortozrout/LabelsTG?tab=readme-ov-file#readme";
        public event Action? RestartRequested;
        public event Action? PrintOneFileRequested;

        /// <summary>
        /// Initializes a new instance of the Controller class and wires up event handlers.
        /// </summary>
        public Controller(View view, Model model)
        {
            View = view;
            Model = model;

            View.NewFileRequested += CreateNewFile;
            View.CopyFileRequested += CopySelectedEplFile;
            View.RenameFileRequested += RenameSelectedEplFile;
            View.SaveFileRequested += SaveSelectedFile;
            View.DeleteFileRequested += DeleteSelectedFile;
            View.PrintFileRequested += () => PrintEplFile();
            View.ToggleSettingsViewRequested += ToggleSettingsView;
            View.RestartRequested += () => RestartRequested?.Invoke();
            View.AddNewFileRequested += () => AddNewFile(true);
            Model.OnFilePrintToScreen += (body) => View.SetTextView(body);
            View.ShowHelpRequested += () => RunExternalProcess(readmeUrl);
            View.EditFileRequested += EditSelectedFile;
            View.OpenInExtEditorRequested += () => OpenInExternalEditor();

            View.Loaded += () =>
            {
                HandleViewLoaded();
            };
            View.listView.SelectedItemChanged += (args) =>
            {
                UpdateTextView();
            };
            View.listView.OpenSelectedItem += (args) =>
            {
                HandleOpenSelectedItem();
            };
            View.listView.KeyPress += (args) =>
            {
                HandleKeyPress(args);
            };
            Model.OnError += View.ShowError;
            Log.OnError += View.ShowError;
            Printer.OnPrintError += View.ShowError;
        }

        /// <summary>
        /// Handles logic when the view is loaded, such as updating the list view and handling login.
        /// </summary>
        private void HandleViewLoaded()
        {
            UpdateListView();

            if (Configuration.Login)
            {
                string user = View.LaunchDialog("Je vyžadována identifikace uživatele", "");
                Configuration.User = user;

                if (string.IsNullOrEmpty(user))
                {
                    Environment.Exit(0);
                }
            }

            if (Configuration.PrintOneFile)
            {
                PrintOneFileRequested?.Invoke();
            }
        }

        /// <summary>
        /// Toggles between settings view and EPL files view.
        /// </summary>
        private void ToggleSettingsView()
        {
            isSetting = !isSetting;
            if (isSetting)
            {
                searchBuffer = "";
                View.filterField.Visible = false;
            }
            UpdateListView();
            View.listView.SetFocus();
        }

        /// <summary>
        /// Prints the selected EPL file, optionally taking a specific file as parameter.
        /// </summary>
        public void PrintEplFile(EplFile? eplFile = null)
        {
            eplFile ??= GetSelectedItem() as EplFile;
            var parser = new Parser();

            parser.OnTemplateSave += newTemplate =>
            {
                if (newTemplate == null || eplFile == null) return;

                eplFile.Template = newTemplate;
                SaveAndNotify(eplFile, false);
            };

            parser.InputRequested += (sender, e) =>
            {
                string? input = View.LaunchDialog(e.Prompt, e.DefaultValue);
                e.Result = input ?? e.DefaultValue;
            };

            parser.OnError += (message) => View.ShowError(message);
            parser.ShowInfo += (message) => View.ShowInfo(message);

            if (eplFile != null)
            {
                parser.Process(ref eplFile);
                Model.PrintEplFile(eplFile);
            }
        }

        /// <summary>
        /// Finds files whose key contains the searched text (case-insensitive).
        /// </summary>
        private static List<EplFile> FindFiles(List<EplFile> files, string searchedText)
        {
            return files.FindAll(e => e.Key.Contains(searchedText, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Updates the list view source and related UI elements based on the current mode (settings/EPL).
        /// </summary>
        private void UpdateListView()
        {
            if (isSetting)
            {
                View.SetListViewSource(Model.GetSettingsFiles());
                if (Model.SettingsFiles.Count == 0)
                    View.SetTextView("No settings files found.");
                else UpdateTextView();
                View.buttonPrint.Visible = false;
                View.buttonRestart.Visible = true;
                View.buttonEditSettings.Text = "EPL files";
                View.label.Text = "Settings file:";
                View.TextToggler = "EPL files";
                View.textView.CanFocus = true;
            }
            else
            {
                currentListWievSource = Model.GetEplFiles();
                View.listView.SetSource(currentListWievSource);
                if (currentListWievSource.Count == 0)
                    View.SetTextView("No EPL files found.");
                else UpdateTextView();
                View.buttonPrint.Visible = true;
                View.buttonRestart.Visible = false;
                View.buttonEditSettings.Text = "Settings";
                View.label.Text = "EPL template:";
                View.TextToggler = "Settings";
                if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
                {
                    View.textView.CanFocus = false;
                }
            }
            View.listView.SelectedItem = 0;
        }

        /// <summary>
        /// Creates a new file (EPL or settings) based on the current mode.
        /// </summary>
        public void CreateNewFile()
        {
            if (!isSetting)
            {
                if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
                {
                    View.ShowError("Cannot create new file when master template is set.");
                    return;
                }
                var newFileName = View.LaunchDialog("Enter file name", "newfile.txt");
                //Create a new file with the given name and a default template
                string template = "N\nI8,B\n";
                if (string.IsNullOrEmpty(newFileName))
                {
                    return;
                }
                var newEplFile = new EplFile(newFileName, Path.Combine(Configuration.TemplatesDirectory, newFileName), template);
                Model.AddEplFile(newEplFile);
                currentListWievSource = Model.GetEplFiles();
                View.SetListViewSource(currentListWievSource);
                int index = currentListWievSource.FindIndex(file => file.Key == newEplFile.Key);
                View.listView.SelectedItem = index;
                View.listView.EnsureSelectedItemVisible();
                View.textView.Text = newEplFile.Template;
                View.textView.SetFocus();
            }
            else
            {
                ConfigItem<string>? selectedFile = View.LaunchListDialog("Select a settings file", Model.NewSettingsFiles);
                if (selectedFile != null)
                {
                    int index = Model.SettingsFiles.FindIndex(file => file.Key == selectedFile.Key);
                    View.listView.SelectedItem = index;
                    View.SetTextView(selectedFile.DefaultContent);
                    View.textView.SetFocus();
                }
            }
        }

        /// <summary>
        /// Gets the currently selected item from the list view.
        /// </summary>
        public BaseItem GetSelectedItem()
        {
            //Get the selected item from the list view
            if (View.listView.SelectedItem >= 0)
            {
                return View.listView.Source.ToList()[View.listView.SelectedItem] as BaseItem ?? throw new InvalidOperationException("Selected item is not a BaseItem.");
            }
            return null!; // Return null if no item is selected
        }

        /// <summary>
        /// Deletes the currently selected file or configuration item.
        /// </summary>
        public void DeleteSelectedFile()
        {
            BaseItem item = GetSelectedItem();
            if (item is EplFile && !string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
            {
                View.ShowError("Cannot delete EPL file when master template is set.");
                return;
            }
            if (item != null)
            {
                bool del = View.Confirm($"Are you sure you want to delete {item.Key}?");
                if (!del) return;
            }
            if (item is EplFile eplFile)
            {
                // if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
                // {
                //     View.ShowError("Cannot delete EPL file when master template is set.");
                //     return;
                // }
                DeleteAndNotify(eplFile);
            }
            else if (item is ConfigItem<string> configItem)
            {
                DeleteStringConfigItem(configItem);
            }
            else if (item is ConfigItem<int> configItemInt)
            {
                configItemInt.Value = int.TryParse(configItemInt.DefaultValue, out var value) ? value : 0;
                WriteConfig();
            }
            else if (item is ConfigItem<bool> configItemBool)
            {
                configItemBool.Value = false;
                WriteConfig();
            }
            else if (item is ConfigItem<Color> configItemColor)
            {
                configItemColor.Value = Color.Green;
                WriteConfig();
            }
            else if (item is ConfigItem<System.Text.Encoding> configItemEncoding)
            {
                configItemEncoding.Value = Encoding.UTF8; // Reset to default encoding
                WriteConfig();
            }
            else
            {
                View.ShowError("Unknown item type.");
                return;
            }
            UpdateListView();
        }

        /// <summary>
        /// Saves the currently selected file or configuration item.
        /// </summary>
        public void SaveSelectedFile()
        {
            BaseItem item = GetSelectedItem();
            if (item is EplFile eplFile)
            {
                if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
                {
                    View.ShowError("Cannot save EPL file when master template is set.");
                    return;
                }
                eplFile.Template = View.textView.Text.ToString();
                SaveAndNotify(item);
            }
            else if (item is ConfigItem<string> configItem)
            {
                SaveStringConfigItem(configItem);
            }
            else if (item is ConfigItem<int> configItemInt)
            {
                configItemInt.Value = int.TryParse(View.textView.Text.ToString(), out var value) ? value : 0;
                WriteConfig();
            }
            else if (item is ConfigItem<bool> configItemBool)
            {
                configItemBool.Value = bool.TryParse(View.textView.Text.ToString(), out var value) && value;
                WriteConfig();
            }
            else if (item is ConfigItem<Color> configItemColor)
            {
                if (Enum.TryParse(View.textView.Text.ToString(), out Color color))
                {
                    configItemColor.Value = color;
                    WriteConfig();
                    View.ShowInfo("Configuration file saved successfully.");
                }
                else
                {
                    View.ShowError("Invalid color format.");
                }
            }
            else if (item is ConfigItem<System.Text.Encoding> configItemEncoding)
            {
                try
                {
                    configItemEncoding.Value = System.Text.Encoding.GetEncoding(View.textView.Text.ToString()?.Trim() ?? "utf-8");
                    WriteConfig();
                    View.ShowInfo("Configuration file saved successfully.");
                }
                catch (ArgumentException)
                {
                    View.ShowError("Invalid encoding format.");
                }
            }
            else
            {
                View.ShowError("Unknown item type.");
                return;
            }
        }

        /// <summary>
        /// Deletes a string configuration item, handling file and non-file cases.
        /// </summary>
        private void DeleteStringConfigItem(ConfigItem<string> configItem)
        {
            if (configItem.IsFile)
            {
                if (configItem.Key == "ConfigFile")
                {
                    View.ShowError("Cannot delete the configuration file.");
                    return;
                }
                if (string.IsNullOrEmpty(configItem.Value))
                {
                    View.ShowError("File path is empty.");
                    return;
                }
                else
                {
                    DeleteAndNotify(configItem);
                    configItem.Value = "";
                    configItem.Content = "";
                    WriteConfig();
                }
            }
            else
            {
                configItem.Value = configItem.DefaultValue;
                WriteConfig();
            }
        }

        /// <summary>
        /// Saves a string configuration item, handling file and non-file cases.
        /// </summary>
        private void SaveStringConfigItem(ConfigItem<string> configItem)
        {
            if (configItem.IsFile)
            {
                configItem.Content = View.textView.Text.ToString();

                if (configItem.Key == "ConfigFile")
                {
                    try
                    {
                        // Load configuration from configItem.Content
                        Configuration.LoadFromContent(configItem.Content);

                        // Update the list view source
                        Model.UpdateSettingsFiles();
                        View.SetListViewSource(Model.GetSettingsFiles());

                        // Write updated configuration to the file
                        WriteConfig();
                        View.ShowInfo("Configuration file saved and reloaded successfully.");
                    }
                    catch (Exception ex)
                    {
                        View.ShowError($"Error saving configuration file: {ex.Message}");
                    }
                    return;
                }

                if (string.IsNullOrEmpty(configItem.Value))
                {
                    string? path = View.LaunchSaveDialog("Save File", "Select a file to save");
                    if (string.IsNullOrEmpty(path)) return;

                    try
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                        configItem.Value = path;
                    }
                    catch (Exception ex)
                    {
                        View.ShowError($"Error creating directory: {ex.Message}");
                        return;
                    }
                }

                SaveAndNotify(configItem);
                WriteConfig();
            }
            else
            {
                configItem.Value = View.textView.Text?.ToString()?.Trim() ?? string.Empty;
                WriteConfig();
                View.ShowInfo("Configuration value saved successfully.");
            }
        }

        /// <summary>
        /// Saves a file or configuration item and notifies the user of the result.
        /// </summary>
        private static void SaveAndNotify(BaseItem item, bool showInfo = true)
        {
            try
            {
                Model.SaveFile(item);
                if (showInfo) View.ShowInfo("File saved successfully.");
            }
            catch (Exception ex)
            {
                View.ShowError($"Error saving file: {ex.Message}");
            }
        }

        /// <summary>
        /// Deletes a file or configuration item and notifies the user of the result.
        /// </summary>
        private void DeleteAndNotify(BaseItem item)
        {
            try
            {
                Model.DeleteFile(item);
                View.ShowInfo("File deleted successfully.");
            }
            catch (Exception ex)
            {
                View.ShowError($"Error deleting file: {ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Writes the configuration items to the configuration file.
        /// </summary>
        private void WriteConfig()
        {
            //string configFilePath = Configuration.ConfigFile;
            try
            {
                Configuration.SaveConfiguration();
                // Update the ConfigItem in the list
                int index = View.listView.SelectedItem;
                //Model.SettingsFiles[0] = new ConfigItem<string>("ConfigFile", "", "", true, () => configFilePath, (value) => { Configuration.ConfigFile = value; }, "", File.ReadAllText(configFilePath));
                View.SetListViewSource(Model.GetSettingsFiles());
                View.listView.SelectedItem = index;
            }
            catch (Exception ex)
            {
                View.ShowError($"Error writing configuration file: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the text view with the content of the currently selected item.
        /// </summary>
        private void UpdateTextView()
        {
            var selected = GetSelectedItem();
            View.SetTextView(selected switch
            {
                EplFile selectedEpl => selectedEpl.Template,
                ConfigItem<string> selectedConfigItem when selectedConfigItem.IsFile =>
                    !string.IsNullOrEmpty(selectedConfigItem.Content)
                        ? selectedConfigItem.Content
                        : selectedConfigItem.DefaultContent,
                ConfigItem<string> selectedConfigItem => selectedConfigItem.Value,
                ConfigItem<int> selectedConfigItem => selectedConfigItem.Value.ToString(),
                ConfigItem<bool> selectedConfigItem => selectedConfigItem.Value.ToString(),
                ConfigItem<Color> selectedConfigItem => selectedConfigItem.Value.ToString(),
                ConfigItem<System.Text.Encoding> selectedConfigItem => selectedConfigItem.Value.WebName, // použij WebName pro zápis do configu
                _ => ""
            });
        }

        /// <summary>
        /// Handles opening the selected item (printing or showing content).
        /// </summary>
        private void HandleOpenSelectedItem()
        {
            if (isSetting)
            {
                View.textView.SetFocus();
                return;
            }

            var selectedItem = GetSelectedItem();
            if (selectedItem is EplFile eplFile)
            {
                PrintEplFile(eplFile);
            }
        }

        /// <summary>
        /// Handles key press events in the list view, including search, delete, and navigation.
        /// </summary>
        private void HandleKeyPress(Terminal.Gui.View.KeyEventEventArgs args)
        {
            var key = args.KeyEvent;

            if (key.IsAlt && char.IsLetter((char)args.KeyEvent.KeyValue))
            {
                // Mask ALT+letter key press
                return;
            }

            if ((char)key.KeyValue == 'Q' && key.IsCtrl)
            {
                Application.RequestStop();
                args.Handled = true;
            }
            else if (key.Key == (Key.DeleteChar | Key.ShiftMask) || key.Key == Key.F8)
            {
                DeleteSelectedFile();
                args.Handled = true;
            }
            else if (key.Key == (Key.O | Key.CtrlMask) || key.Key == Key.F9)
            {
                AddNewFile(fromMenu: false);
                args.Handled = true;
            }
            else if (key.Key == (Key.N | Key.CtrlMask) || key.Key == Key.F7)
            {
                CreateNewFile();
                args.Handled = true;
            }
            else if (key.Key == (Key.C | Key.ShiftMask | Key.CtrlMask) || key.Key == Key.F5)
            {
                CopySelectedEplFile();
                args.Handled = true;
            }
            else if (key.Key == (Key.R | Key.CtrlMask) || key.Key == Key.F6)
            {
                RenameSelectedEplFile();
                args.Handled = true;
            }
            else if (key.Key == (Key.E | Key.CtrlMask) || key.Key == Key.F3)
            {
                EditSelectedFile();
                args.Handled = true;
            }
            else if (key.Key == (Key.E | Key.CtrlMask | Key.ShiftMask) || key.Key == Key.F4)
            {
                OpenInExternalEditor();
                args.Handled = true;
            }
            else if (key.Key == Key.F1)
            {
                RunExternalProcess(readmeUrl);
                args.Handled = true;
            }
            else if ((char.IsLetterOrDigit((char)key.KeyValue) || key.Key == Key.Space) && !isSetting)
            {
                HandleSearchKeyPress((char)key.KeyValue);
                args.Handled = true;
            }
            else if (key.Key == Key.Backspace && searchBuffer.Length > 0 && !isSetting)
            {
                HandleBackspaceKeyPress();
                args.Handled = true;
            }
            else if (key.Key == Key.Esc)
            {
                HandleEscapeKeyPress();
                args.Handled = true;
            }
        }

        /// <summary>
        /// Handles search key presses, updating the filter and list view.
        /// </summary>
        private void HandleSearchKeyPress(char keyValue)
        {
            searchBuffer += keyValue;
            UpdateFilterField(searchBuffer);
            var matchFiles = FindFiles(currentListWievSource, searchBuffer);
            View.listView.SetSource(matchFiles);

            if (matchFiles.Count > 0)
            {
                View.listView.SelectedItem = 0;
                View.textView.Text = matchFiles[0].Template;
            }
            else
            {
                View.textView.Text = "";
            }
        }

        /// <summary>
        /// Handles backspace key presses in the search buffer.
        /// </summary>
        private void HandleBackspaceKeyPress()
        {
            searchBuffer = searchBuffer[..^1];
            UpdateFilterField(searchBuffer);
            var matchFiles = FindFiles(currentListWievSource, searchBuffer);
            View.SetListViewSource(matchFiles);
        }

        /// <summary>
        /// Handles escape key presses, clearing the search buffer and resetting the list view.
        /// </summary>
        private void HandleEscapeKeyPress()
        {
            searchBuffer = "";
            UpdateFilterField(searchBuffer);
            UpdateListView();
        }

        /// <summary>
        /// Updates the filter field UI element with the current search buffer.
        /// </summary>
        private void UpdateFilterField(string searchBuffer)
        {
            if (!string.IsNullOrEmpty(searchBuffer))
                View.filterField.Text = "Filter:" + searchBuffer;
            View.filterField.Visible = !string.IsNullOrEmpty(searchBuffer);
        }

        /// <summary>
        /// Adds a new file or settings file, depending on the current mode.
        /// </summary>
        private void AddNewFile(bool fromMenu = false)
        {
            if (isSetting)
            {
                HandleAddNewSettingsFile(fromMenu);
            }
            else if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
            {
                View.ShowError("Cannot add new file when master template is set.");
            }
            else
            {
                HandleAddNewEplFile();
            }
        }

        /// <summary>
        /// Handles adding a new settings file, either from menu or current selection.
        /// </summary>
        private void HandleAddNewSettingsFile(bool fromMenu)
        {
            BaseItem? selectedItem;

            if (fromMenu)
            {
                List<ConfigItem<string>> settingsFiles = Model.SettingsFilesAndDirs;
                selectedItem = View.LaunchListDialog("Select a settings file", settingsFiles);
                if (selectedItem == null) return;
            }
            else
            {
                selectedItem = GetSelectedItem();
            }

            if (selectedItem is ConfigItem<string> configItem && configItem.IsFile)
            {
                HandleSettingsFileSelection(configItem);
            }
            else if (selectedItem != null && selectedItem.Key == "Adresar")
            {
                HandleDirectorySelection(selectedItem);
            }
        }

        /// <summary>
        /// Handles the selection and loading of a settings file.
        /// </summary>
        private void HandleSettingsFileSelection(ConfigItem<string> configItem)
        {
            string? filePath = View.LaunchOpenDialog("Select a settings file", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            if (string.IsNullOrEmpty(filePath))
            {
                View.ShowError("No file selected.");
                return;
            }

            try
            {
                string content = File.ReadAllText(filePath);
                configItem.Value = filePath;
                configItem.Content = content;
                WriteConfig();
                Model.UpdateSettingsFiles();
                View.SetListViewSource(Model.GetSettingsFiles());
                View.listView.SelectedItem = Model.SettingsFiles.FindIndex(file => file.Key == configItem.Key);
                View.textView.Text = content;
                View.textView.SetFocus();
            }
            catch (Exception ex)
            {
                View.ShowError($"Error loading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles the selection and setting of a directory for templates.
        /// </summary>
        private void HandleDirectorySelection(BaseItem selectedItem)
        {
            string? directory = View.LaunchOpenDialog("Select templates directory", "Select a directory", Configuration.TemplatesDirectory, canChooseFiles: false);
            if (string.IsNullOrEmpty(directory))
            {
                View.ShowError("No directory selected.");
                return;
            }

            if (File.Exists(directory))
            {
                directory = Path.GetDirectoryName(directory) ?? directory;
            }
            else if (!Directory.Exists(directory))
            {
                View.ShowError("Selected path is not a valid directory.");
                return;
            }

            if (selectedItem is ConfigItem<string> templatesDir)
            {
                templatesDir.Value = directory;
            }
            WriteConfig();
        }

        /// <summary>
        /// Handles adding a new EPL file by selecting and loading its content.
        /// </summary>
        private void HandleAddNewEplFile()
        {
            if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
            {
                View.ShowError("Cannot add new file when master template is set.");
                return;
            }
            string? fileAddress = View.LaunchOpenDialog("Select a file", Configuration.TemplatesDirectory);
            if (string.IsNullOrEmpty(fileAddress)) return;

            try
            {
                string template = File.ReadAllText(fileAddress);
                string fileName = Path.GetFileName(fileAddress);
                string filePath = Path.Combine(Configuration.TemplatesDirectory, fileName);
                EplFile newEplFile = new(fileName, filePath, template);
                Model.AddEplFile(newEplFile);
                currentListWievSource = Model.GetEplFiles();
                View.SetListViewSource(currentListWievSource);
                int index = currentListWievSource.FindIndex(file => file.Key == newEplFile.Key);
                View.listView.SelectedItem = index;
                View.textView.Text = newEplFile.Template;
                View.textView.SetFocus();
            }
            catch (Exception ex)
            {
                View.ShowError($"Error loading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Copies an existing EPL file to a new file with a user-defined name.
        /// </summary>
        private void CopySelectedEplFile()
        {
            if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
            {
                View.ShowError("Cannot copy file when master template is set.");
                return;
            }
            if (GetSelectedItem() is not EplFile eplFile) return;
            string? newFileName = View.LaunchDialog("Enter new file name", eplFile.Key, "Copy of " + eplFile.Key);
            if (string.IsNullOrEmpty(newFileName)) return;
            string newFilePath = Path.Combine(Configuration.TemplatesDirectory, newFileName);
            try
            {
                File.Copy(eplFile.FileAddress, newFilePath, true);
                EplFile newEplFile = new(newFileName, newFilePath, eplFile.Template);
                Model.AddEplFile(newEplFile);
                currentListWievSource = Model.GetEplFiles();
                View.SetListViewSource(currentListWievSource);
                int index = currentListWievSource.FindIndex(file => file.Key == newEplFile.Key);
                View.listView.SelectedItem = index;
                View.textView.Text = newEplFile.Template;
                View.textView.SetFocus();
            }
            catch (Exception ex)
            {
                View.ShowError($"Error copying file: {ex.Message}");
            }
        }

        /// <summary>
        /// Renames the currently selected EPL file to a new name provided by the user. 
        /// </summary>
        private void RenameSelectedEplFile()
        {
            if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
            {
                View.ShowError("Cannot rename file when master template is set.");
                return;
            }
            if (GetSelectedItem() is not EplFile eplFile) return;
            string? newFileName = View.LaunchDialog("Enter new file name", eplFile.Key, "Rename " + eplFile.Key);
            if (string.IsNullOrEmpty(newFileName)) return;
            string newFilePath = Path.Combine(Configuration.TemplatesDirectory, newFileName);
            try
            {
                File.Move(eplFile.FileAddress, newFilePath);
                EplFile renamedEplFile = new(newFileName, newFilePath, eplFile.Template);
                Model.RemoveEplFile(eplFile);
                Model.AddEplFile(renamedEplFile);
                currentListWievSource = Model.GetEplFiles();
                View.SetListViewSource(currentListWievSource);
                int index = currentListWievSource.FindIndex(file => file.Key == renamedEplFile.Key);
                View.listView.SelectedItem = index;
            }
            catch (Exception ex)
            {
                View.ShowError($"Error renaming file: {ex.Message}");
                return;
            }
        }

        private void EditSelectedFile()
        {
            BaseItem? selectedItem = GetSelectedItem();
            if (selectedItem is EplFile && !string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
            {
                View.ShowError("Cannot edit EPL file when master template is set.");
                return;
            }
            View.textView.SetFocus();
        }

        /// <summary>
        /// Runs an external process or opens a file/URL, optionally waiting for it to exit.
        /// </summary>
        private static void RunExternalProcess(string filePath, string? arguments = null, bool waitForExit = false)
        {
            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = arguments ?? "",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                using var process = Process.Start(processStartInfo);
                if (waitForExit && process != null)
                {
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                View.ShowError($"Error running external process: {ex.Message}");
            }
        }
        /// <summary>
        /// Opens the selected item in an external editor or file viewer.
        /// /// </summary>
        private void OpenInExternalEditor()
        {
            BaseItem? selectedItem = GetSelectedItem();
            if (selectedItem is EplFile eplFile)
            {
                if (!string.IsNullOrEmpty(Configuration.MasterTemplateAddress))
                {
                    View.ShowError("Cannot open EPL file in external editor when master template is set.");
                    return;
                }
                RunExternalProcess(eplFile.FileAddress, waitForExit: false);
            }
            else if (selectedItem is ConfigItem<string> configItem && configItem.IsFile)
            {
                RunExternalProcess(configItem.Value, waitForExit: false);
            }
            else if (selectedItem is ConfigItem<string> configItemString && Directory.Exists(configItemString.Value))
            {
                RunExternalProcess(configItemString.Value, waitForExit: false);
            }
            else
            {
                View.ShowError("No valid file selected for external editor.");
            }
        }
    }
}