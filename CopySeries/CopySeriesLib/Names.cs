namespace DoenaSoft.CopySeries
{
    using System.Diagnostics;
    using System.Xml.Serialization;

    [XmlRoot]
    public class Names
    {
        public Name[] NameList;
    }

    [DebuggerDisplay("ShortName: {ShortName}, DisplayName: {DisplayName}")]
    public class Name
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
        public bool YearSpecified => Year > 0;

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
    }
}