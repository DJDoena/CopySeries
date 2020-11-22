namespace DoenaSoft.CopySeries
{
    using System;
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
        private string m_SortName;

        private string m_DisplayName;

        private string m_LocalizedName;

        public string ShortName;

        public string LongName;

        public string SortName
        {
            get => (SortNameSpecified ? m_SortName : LongName);
            set => m_SortName = value;
        }

        [XmlIgnore]
        public Boolean SortNameSpecified
            => (string.IsNullOrEmpty(m_SortName) == false);

        public string DisplayName
        {
            get => (DisplayNameSpecified ? m_DisplayName : LongName);
            set => m_DisplayName = value;
        }

        [XmlIgnore]
        public Boolean DisplayNameSpecified
            => (string.IsNullOrEmpty(m_DisplayName) == false);

        public ushort Year;

        [XmlIgnore]
        public bool YearSpecified
            => (Year > 0);

        public string LocalizedName
        {
            get => (LocalizedNameSpecified ? m_LocalizedName : DisplayName);
            set => m_LocalizedName = value;
        }

        [XmlIgnore]
        public bool LocalizedNameSpecified
            => (string.IsNullOrEmpty(m_LocalizedName) == false);

        public string OriginalLanguage;

        public string Link;

        public string EpisodeNamesLink;
    }
}