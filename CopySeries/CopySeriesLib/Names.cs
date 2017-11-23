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

        public String ShortName;

        public String LongName;

        public String SortName
        {
            get
            {
                return (String.IsNullOrEmpty(m_SortName) ? LongName : m_SortName);
            }
            set
            {
                m_SortName = value;
            }
        }

        public String DisplayName
        {
            get
            {
                return (String.IsNullOrEmpty(m_DisplayName) ? LongName : m_DisplayName);
            }
            set
            {
                m_DisplayName = value;
            }
        }
    }
}