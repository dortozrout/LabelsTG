namespace LabelsTG.Labels
{
    public abstract class BaseItem(string key, string description)
    {
        public string Key { get; set; } = key;
        public string Description { get; set; } = description;

        public override string ToString()
        {
            return $"{Key}: {Description}";
        }
    }
}
