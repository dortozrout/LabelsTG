using System.Text;

namespace LabelsTG.Labels
{
    public class ConfigItem<T>(string key, string defaultValue, string description, bool isFile, Func<T> getter, Action<T> setter, string defaultContent = "", string content = "") : BaseItem(key, description)
    {
        public string DefaultValue { get; set; } = defaultValue;
        public bool IsFile { get; set; } = isFile;
        public string DefaultContent { get; set; } = defaultContent;
        public string Content { get; set; } = content;

        private readonly Func<T> getter = getter;
        private readonly Action<T> setter = setter;
        public T Value
        {
            get => getter();
            set => setter(value);
        }

        public override string ToString()
        {
            if (typeof(T) == typeof(Encoding))
            {
                var encoding = Value as Encoding;
                return $"{Key}: {encoding?.WebName ?? "Unknown Encoding"}";
            }
            return $"{Key}: {Value}";
        }
    }
}