using System.Text;

namespace LabelsTG.Labels
{
    public class ConfigItem<T> : BaseItem
    {
        public string DefaultValue { get; set; }
        public bool IsFile { get; set; }
        public string DefaultContent { get; set; } = "";
        public string Content { get; set; } = "";

        private readonly Func<T> getter;
        private readonly Action<T> setter;
        public T Value
        {
            get => getter();
            set => setter(value);
        }

        public ConfigItem(string key, string defaultValue, string description, bool isFile, Func<T> getter, Action<T> setter, string defaultContent = "", string content = "")
            : base(key, description)
        {
            DefaultValue = defaultValue;
            IsFile = isFile;
            this.getter = getter;
            this.setter = setter;
            DefaultContent = defaultContent;
            Content = content;
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