#nullable enable
namespace LabelsTG.Labels;
// Definice EventArgs pro vstupní požadavek
public class InputRequestEventArgs(string prompt, string defaultValue) : EventArgs
{
    public string Prompt { get; } = prompt;
    public string DefaultValue { get; } = defaultValue;
    public string? Result { get; set; }
}