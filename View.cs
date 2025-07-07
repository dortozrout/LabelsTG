#nullable enable
using Terminal.Gui;
using LabelsTG.Labels;
using System;

namespace LabelsTG
{
    /// <summary>
    /// Represents the main view of the application.
    /// It contains the menu bar, list view, text view, and buttons for various actions.
    /// </summary>
    public class View : Window
    {
        public event Action? NewFileRequested;
        public event Action? SaveFileRequested;
        public event Action? DeleteFileRequested;
        public event Action? PrintFileRequested;
        public event Action? ToggleSettingsViewRequested;
        public event Action? RestartRequested;
        public event Action? AddNewFileRequested;
        public event Action? ShowHelpRequested;
        public event Action? OpenInExtEditorRequested;

        public ListView listView;
        public Button buttonQuit;
        public Button buttonEditSettings;
        public Button buttonRestart;
        public Button buttonPrint;
        public Label label;
        public TextView textView;
        public TextField filterField;
        private readonly MenuItem ToggleSettings;
        public string TextToggler
        {
            set => ToggleSettings.Title = value;
        }
        private readonly ColorScheme baseColorScheme;
        private readonly ColorScheme menuColorScheme;
        private static ColorScheme? dialogColorScheme;
        private readonly ColorScheme filterColorScheme;

        /// <summary>
        /// Initializes a new instance of the <see cref="View"/> class.
        /// This constructor sets up the window with a title, color schemes, menu bar, list view,
        /// text view, buttons, and a filter field.
        /// It also defines the layout and behavior of the main view.
        /// The window is designed to display and manage label templates for printing on EPL/ZPL printers.
        /// </summary>
        public View() : base("Tisk štítků na EPL tiskárně v. " + Configuration.AppName)
        {
            // Set the colors of the window
            Color usercolor = Configuration.UserDefinedColor;
            ToggleSettings = new MenuItem("Edit Settings", "", () => ToggleSettingsViewRequested?.Invoke());

            baseColorScheme = new ColorScheme()
            {
                //Set the color of the normal text/background
                Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
                //Set the color of the focused text/background
                Focus = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                HotNormal = Application.Driver.MakeAttribute(usercolor, Color.Black),
                HotFocus = Application.Driver.MakeAttribute(usercolor, Color.Gray),
            };
            ColorScheme = baseColorScheme;

            menuColorScheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                Focus = Application.Driver.MakeAttribute(Color.White, Color.Black),
                HotNormal = Application.Driver.MakeAttribute(usercolor, Color.Gray),
                HotFocus = Application.Driver.MakeAttribute(usercolor, Color.DarkGray),
            };

            dialogColorScheme = new ColorScheme()
            {
                Normal = Application.Driver.MakeAttribute(Color.Black, Color.Gray),
                Focus = Application.Driver.MakeAttribute(Color.White, Color.DarkGray),
                HotNormal = Application.Driver.MakeAttribute(usercolor, Color.Gray),
                HotFocus = Application.Driver.MakeAttribute(usercolor, Color.DarkGray),
            };
            Color fg = (int)usercolor < 8 ? Color.White : Color.Black; // Ensure contrast

            filterColorScheme = new ColorScheme()
            {
                Focus = Application.Driver.MakeAttribute(fg, usercolor),
            };

            // Set the size of the window
            Width = Dim.Fill();
            Height = Dim.Fill();

            // Set the menu bar
            var menu = new MenuBar(
            [
                new MenuBarItem("_File", new MenuItem[]
                {
                    new("New", "", () => NewFileRequested?.Invoke()),
                    new("Open new file", "", () => AddNewFileRequested?.Invoke()),
                    new("Edit", "", () => textView?.SetFocus()),
                    new("Edit in external editor", "", () => OpenInExtEditorRequested?.Invoke()),
                    new("Save", "", () => SaveFileRequested?.Invoke()),
                    new("Delete", "", () => DeleteFileRequested?.Invoke()),
                    new("Print", "", () => PrintFileRequested?.Invoke()),
                    new("Quit", "", () => Application.RequestStop()),
                }),
                new MenuBarItem("_Edit", new MenuItem[]
                {
                    //dynamically change the text of the menu item
                    ToggleSettings,
                    //new("_Edit Settings", "", () => ToggleSettingsViewRequested?.Invoke()),
                    new("Restart", "", () => RestartRequested?.Invoke()),
                }),
                new MenuBarItem("_Help", new MenuItem[]
                {
                    new("Help", "", () => ShowHelpRequested?.Invoke()),
                    new("About", "", () => ShowInfo("LabelsTG - Print Labels On EPL/ZPL Printer\nVersion: " + Configuration.AppName + "\nAuthor: Z.Hunal\nLicense: MIT")),
                }),
            ])
            {
                ColorScheme = menuColorScheme,
            };
            listView = new ListView()
            {
                X = 1,
                Y = 1,
                Width = Dim.Percent(30),
                Height = Dim.Fill() - 2,
            };
            buttonQuit = new Button("Quit")
            {
                X = Pos.AnchorEnd() - 10,
                Y = Pos.Bottom(listView) + 1,
            };
            buttonQuit.Clicked += () =>
            {
                Application.RequestStop();
            };
            buttonEditSettings = new Button("Settings")
            {
                X = Pos.Right(buttonQuit) - 21,
                Y = Pos.Bottom(listView) + 1,
            };
            buttonEditSettings.Clicked += () =>
            {
                ToggleSettingsViewRequested?.Invoke();
            };
            buttonRestart = new Button("Restart")
            {
                X = Pos.Right(buttonEditSettings) - 24,
                Y = Pos.Bottom(listView) + 1,
            };
            buttonRestart.Clicked += () =>
            {
                RestartRequested?.Invoke();
            };
            label = new Label("Epl template:")
            {
                X = Pos.Right(listView) + 1,
                Y = 1,
            };
            textView = new TextView()
            {
                Text = "Select a template to view its content here.",
                ReadOnly = false,
                X = Pos.Right(listView) + 1,
                Y = Pos.Bottom(label),
                Width = Dim.Fill(),
                Height = Dim.Fill() - 1,
                WordWrap = true,
                AllowsTab = true,
            };
            textView.KeyPress += (args) =>
            {
                var key = args.KeyEvent.Key;
                if (key == Key.F2 || key == (Key.S | Key.CtrlMask))
                {
                    SaveFileRequested?.Invoke();
                    listView.SetFocus();
                    args.Handled = true;
                }
                if (key == Key.Esc)
                {
                    listView.SetFocus();
                    args.Handled = true;
                }
            };
            buttonPrint = new Button("Print")
            {
                X = 1,
                Y = Pos.Bottom(listView) + 1,
            };
            buttonPrint.Clicked += () =>
            {
                PrintFileRequested?.Invoke();
                listView.SetFocus();
            };
            filterField = new TextField("")
            {
                X = 1,
                Y = Pos.Bottom(listView),
                Width = Dim.Percent(30),
                Visible = false,
                CanFocus = false,
                ColorScheme = filterColorScheme,
            };
            //Set the order of the views
            Add(menu, listView, filterField, label, textView, buttonPrint, buttonRestart, buttonEditSettings, buttonQuit);
        }
        /// <summary>
        /// Launches a dialog to get user input.
        /// </summary>
        /// <param name="prompt">The prompt message to display in the dialog.</param>
        /// <param name="defaultText">The default text to pre-fill in the input field.</param>
        /// <returns>The text entered by the user, or an empty string if the user cancels the dialog.</returns>
        /// <remarks>
        /// This method creates a dialog with a label, a text field for user input, and two buttons: OK and CANCEL.
        /// The dialog will close when the user presses Enter or clicks OK, returning the text from the text field.
        /// If the user presses ESC or clicks CANCEL, the text field will be cleared and the dialog will close, returning an empty string.
        /// </remarks>
        public static string LaunchDialog(string prompt, string defaultText)
        {
            // Create a new dialog
            var dialog = new Dialog("Input", 50, 20) { ColorScheme = dialogColorScheme };
            // Create a new label
            var label = new Label($"{prompt}:")
            {
                X = 1,
                Y = 1,
            };
            // Create a new text field
            var textField = new TextField(defaultText)
            {
                X = 1,
                Y = 2,
                Width = 40,
            };
            // Set the text field to be the default button
            textField.SetFocus();
            textField.KeyPress += (keyEvent) =>
            {
                if (keyEvent.KeyEvent.Key == Key.Enter)
                {
                    Application.RequestStop();
                }
                if (keyEvent.KeyEvent.Key == Key.Esc)
                {
                    textField.Text = "";
                    Application.RequestStop();
                }
            };
            // Create a new button
            var okButton = new Button("OK")
            {
                X = 1,
                Y = 3,
            };
            okButton.Clicked += () =>
            {
                // Close the dialog
                Application.RequestStop();
            };
            var cancelButton = new Button("CANCEL")
            {
                X = Pos.Right(okButton) + 1,
                Y = 3,
            };
            cancelButton.Clicked += () =>
            {
                // Close the dialog
                textField.Text = "";
                Application.RequestStop();
            };
            // Add the label, text field, and button to the dialog
            dialog.Add(label, textField, okButton, cancelButton);
            // Add the dialog to the application  
            Application.Run(dialog);
            // Wait for the user to click the button    
            return textField.Text?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Sets the source of the list view to a list of files.
        /// </summary>
        /// <typeparam name="T">The type of the items in the list.</typeparam>
        /// <param name="files">The list of files to set as the source of the list view.</param>
        /// <remarks>
        /// This method updates the list view with the provided list of files.
        /// It allows the user to select a file from the list, which can then be used
        /// for further actions such as editing or printing.
        /// </remarks>
        public void SetListViewSource<T>(List<T> files)
        {
            listView.SetSource(files);
            //listView.SelectedItem = 0;
        }

        /// <summary>
        /// Launches a dialog to select a file from a list.
        /// </summary>
        /// <param name="prompt">The prompt message to display in the dialog.</param>
        /// <param name="files">The list of files to display in the dialog.</param>
        /// <returns>The selected file as a ConfigItem<string>, or null if no file was selected.</returns>
        /// <remarks>
        /// This method creates a dialog with a list view containing the provided files.
        /// The user can select a file from the list and click the SELECT button to confirm their choice.
        /// If the user cancels the dialog, the method returns null.
        /// The dialog will close when the user selects a file or clicks the CANCEL button.
        /// </remarks>
        public static ConfigItem<string>? LaunchListDialog(string prompt, List<ConfigItem<string>> files)
        {
            ConfigItem<string>? selectedFile = null;
            // Create a new dialog
            var dialog = new Dialog(prompt, 50, 20) { ColorScheme = dialogColorScheme };
            // Create a new list view
            var listView = new ListView(files)
            {
                X = 1,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill() - 2,
            };
            listView.OpenSelectedItem += (args) =>
            {
                // Get the selected item
                var selectedItem = listView.SelectedItem;
                if (selectedItem >= 0)
                {
                    // Get the selected file
                    selectedFile = listView.Source.ToList()[selectedItem] as ConfigItem<string>;
                    // Close the dialog
                    Application.RequestStop();
                }
            };
            // Create a new button
            var selectButton = new Button("SELECT")
            {
                X = 1,
                Y = Pos.Bottom(listView) + 1,
            };
            selectButton.Clicked += () =>
            {
                // Get the selected item
                var selectedItem = listView.SelectedItem;
                if (selectedItem >= 0)
                {
                    // Get the selected file
                    selectedFile = listView.Source.ToList()[selectedItem] as ConfigItem<string>;
                }
                // Close the dialog
                Application.RequestStop();
            };
            var cancelButton = new Button("CANCEL")
            {
                X = Pos.Right(selectButton) + 1,
                Y = Pos.Bottom(listView) + 1,
            };
            cancelButton.Clicked += () =>
            {
                // Close the dialog
                Application.RequestStop();
            };
            // Add the list view and button to the dialog
            dialog.Add(listView, selectButton, cancelButton);
            // Add the dialog to the application  
            Application.Run(dialog);
            return selectedFile ?? null;
        }

        /// <summary>
        /// Sets the text of the text view.
        /// </summary>
        /// <param name="text">The text to set in the text view.</param>
        /// <remarks>
        /// This method updates the text view with the provided text.
        /// It allows the user to view and edit the content of a selected label template.
        /// </remarks>
        public void SetTextView(string text)
        {
            textView.Text = text;
        }

        /// <summary>
        /// Launches a dialog to save a file.
        /// </summary>
        /// <param name="label">The label for the save dialog.</param>
        /// <param name="prompt">The prompt message to display in the dialog.</param>
        /// <returns>The file path selected by the user, or null if the dialog was canceled.</returns>
        /// <remarks>
        /// This method creates a dialog with a save button and a text field for the file name.
        /// The user can enter a file name and click the save button to confirm their choice.
        /// If the user cancels the dialog, the method returns null.
        /// The dialog will close when the user clicks the save button or cancels the dialog.
        /// </remarks>
        public static string? LaunchSaveDialog(string label, string prompt)
        {
            // Create a new dialog
            var dialog = new SaveDialog(label, prompt, [".txt", ".epl", ".conf"])
            {
                CanCreateDirectories = true,
                DirectoryPath = Configuration.ConfigPath,
                ColorScheme = dialogColorScheme,
            };
            Application.Run(dialog);
            // Wait for the user to click the button
            if (dialog.Canceled)
            {
                return null;
            }
            return dialog.FilePath.ToString();
        }

        /// <summary>
        /// Launches a dialog to open a file.
        /// </summary>
        /// <param name="label">The label for the open dialog.</param>
        /// <param name="prompt">The prompt message to display in the dialog.</param>
        /// <param name="defaultPath">The default path to start the dialog from.</param>
        /// <param name="canChooseFiles">Whether the user can choose files or directories.</param>
        /// <returns>The file path selected by the user, or null if the dialog was canceled.</returns>
        /// <remarks>
        /// This method creates a dialog with an open button and a text field for the file name.
        /// The user can select a file or directory and click the open button to confirm their choice.
        /// If the user cancels the dialog, the method returns null.
        /// The dialog will close when the user clicks the open button or cancels the dialog.
        /// </remarks>
        public static string? LaunchOpenDialog(string label, string prompt, string defaultPath = "", bool canChooseFiles = true)
        {
            // Create a new dialog
            var dialog = new OpenDialog(label, prompt, [".*", ".txt", ".epl", ".conf"])
            {
                CanChooseFiles = canChooseFiles,
                CanChooseDirectories = !canChooseFiles,
                DirectoryPath = string.IsNullOrEmpty(defaultPath) ? Configuration.TemplatesDirectory : defaultPath,
                ColorScheme = dialogColorScheme,
            };
            Application.Run(dialog);
            // Wait for the user to click the button
            if (dialog.Canceled)
            {
                return null;
            }
            return dialog.FilePath.ToString();
        }
        /// <summary>
        /// Displays an information message in a dialog.
        /// </summary>
        /// <param name="message">The message to display in the dialog.</param>
        public static void ShowInfo(string message)
            => MessageBox.Query("Info", message, "OK");

        /// <summary>
        /// Displays an error message in a dialog.
        /// </summary>
        /// <param name="message">The error message to display in the dialog.</param>
        public static void ShowError(string message)
            => MessageBox.ErrorQuery("Error", message, "OK");

        /// <summary>
        /// Displays a confirmation dialog with a message.
        /// </summary>
        /// <param name="message">The message to display in the confirmation dialog.</param>
        /// <returns>True if the user confirms, false otherwise.</returns>
        public static bool Confirm(string message)
            => MessageBox.Query("Confirm", message, "Yes", "No") == 0;
    }
}