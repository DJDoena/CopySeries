namespace DoenaSoft.CopySeries
{
    using System.Diagnostics;
    using System.Xml.Serialization;

    [XmlRoot]
    public sealed class Names
    {
        public Name[] NameList;
    }

    [DebuggerDisplay("ShortName: {ShortName}, DisplayName: {DisplayName}")]
    public sealed class Name
    {
        private string _sortName;

        private string _displayName;

        private string _localizedName;

        public string ShortName { get; set; }

        public string LongName { get; set; }

        public string SortName
        {
            get => this.SortNameSpecified ? _sortName : this.LongName;
            set => _sortName = value;
        }

        [XmlIgnore]
        public bool SortNameSpecified => !string.IsNullOrEmpty(_sortName);

        public string DisplayName
        {
            get => this.DisplayNameSpecified ? _displayName : this.LongName;
            set => _displayName = value;
        }

        [XmlIgnore]
        public bool DisplayNameSpecified => !string.IsNullOrEmpty(_displayName);

        public ushort Year { get; set; }

        [XmlIgnore]
        public bool YearSpecified => this.Year > 0;

        public string LocalizedName
        {
            get => this.LocalizedNameSpecified ? _localizedName : this.DisplayName;
            set => _localizedName = value;
        }

        [XmlIgnore]
        public bool LocalizedNameSpecified => !string.IsNullOrEmpty(_localizedName);

        public string OriginalLanguage { get; set; }

        public string Link { get; set; }

        public string EpisodeNamesLink { get; set; }

        public override int GetHashCode() => this.ShortName?.GetHashCode() ?? 0;

        public override bool Equals(object obj)
        {
            if (!(obj is Name other))
            {
                return false;
            }
            else
            {
                return this.ShortName == other.ShortName;
            }
        }

        public static bool operator ==(Name left, Name right)
        {
            if (ReferenceEquals(left, right))
            {
                return true;
            }
            else if (!ReferenceEquals(left, null))
            {
                return left.Equals(right);
            }
            else
            {
                //left is null, but right is not null
                return false;
            }
        }

        public static bool operator !=(Name left, Name right) => !(left == right);
    }
}