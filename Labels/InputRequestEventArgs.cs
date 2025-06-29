#nullable enable
namespace LabelsTG.Labels;
// Definice EventArgs pro vstupní požadavek
public class InputRequestEventArgs : EventArgs
{
    public string Prompt { get; }
    public string DefaultValue { get; }
    public string? Result { get; set; }

    public InputRequestEventArgs(string prompt, string defaultValue)
    {
        Prompt = prompt;
        DefaultValue = defaultValue;
    }
}

// Event pro požadavek na vstup

// public event EventHandler<InputRequestEventArgs>? InputRequested;
// private T HandleInput<T>(EplFile eplFile, string prompt, string defaultValue)
// {
//     string? input = null;

//     if (InputRequested != null)
//     {
//         var args = new InputRequestEventArgs(prompt, defaultValue);
//         InputRequested(this, args);
//         input = args.Result;
//     }
//     else
//     {
//         input = View.LaunchDialog(prompt, defaultValue);
//     }

//     if (string.IsNullOrEmpty(input) || input == "0")
//     {
//         continueProcessing = false;
//         eplFile.Print = false;
//         return default;
//     }
//     if (typeof(T) == typeof(int))
//     {
//         if (int.TryParse(input, out int intValue))
//             return (T)(object)intValue;
//         else
//             return (T)(object)0;
//     }
//     else if (typeof(T) == typeof(DateTime))
//     {
//         if (DateTime.TryParse(input, out DateTime dateTimeValue))
//             return (T)(object)dateTimeValue;
//         else
//             return (T)(object)default(DateTime);
//     }
//     else if (typeof(T) == typeof(string))
//     {
//         return (T)(object)input;
//     }
//     else
//     {
//         throw new InvalidOperationException($"Unsupported type: {typeof(T)}");
//     }
// }
// ...existing code...