namespace LabelsTG.Labels
{
    public class EplFile(string name, string address, string template) : BaseItem(name, "EPL File"), IComparable
    {
        public string FileAddress { get; private set; } = address;
        public string Template { get; set; } = template;
        public string Body { get; set; }
        public bool Print { get; set; } = true;

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