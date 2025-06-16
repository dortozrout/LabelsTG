namespace LabelsTG.Labels
{
    public class EplFile : BaseItem, IComparable
    {
        public string FileAddress { get; private set; }
        public string Template { get; set; }
        public string Body { get; set; }
        public bool Print { get; set; } = true;

        public EplFile(string name, string address, string template)
            : base(name, "EPL File")
        {
            FileAddress = address;
            Template = template;
        }

        public int CompareTo(object obj)
        {
            EplFile anotherEplFile = (EplFile)obj;
            return string.Compare(Key, anotherEplFile.Key);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (EplFile)obj;
            return Key == other.Key;
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public override string ToString()
        {
            return Key;
        }

        public EplFile Copy()
        {
            return new EplFile(Key, FileAddress, Template);
        }
    }
}