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
        private String m_SortName;

        private String m_DisplayName;

        private String m_LocalizedName;

        public String ShortName;

        public String LongName;

        public String SortName
        {
            get => (SortNameSpecified ? m_SortName : LongName);
            set => m_SortName = value;
        }

        [XmlIgnore]
        public Boolean SortNameSpecified
            => (String.IsNullOrEmpty(m_SortName) == false);

        public String DisplayName
        {
            get => (DisplayNameSpecified ? m_DisplayName : LongName);
            set => m_DisplayName = value;
        }

        [XmlIgnore]
        public Boolean DisplayNameSpecified
            => (String.IsNullOrEmpty(m_DisplayName) == false);

        public UInt16 Year;

        [XmlIgnore]
        public Boolean YearSpecified
            => (Year > 0);

        public String LocalizedName
        {
            get => (LocalizedNameSpecified ? m_LocalizedName : DisplayName);
            set => m_LocalizedName = value;
        }

        [XmlIgnore]
        public Boolean LocalizedNameSpecified
            => (String.IsNullOrEmpty(m_LocalizedName) == false);
    }
}