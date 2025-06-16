namespace LabelsTG.Labels
{
    public abstract class BaseItem
    {
        public string Key { get; set; }
        public string Description { get; set; }

        public BaseItem(string key, string description)
        {
            Key = key;
            Description = description;
        }

        public override string ToString()
        {
            return $"{Key}: {Description}";
        }
    }
}
