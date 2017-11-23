namespace DoenaSoft.CopySeries
{
    using System;
    using System.Xml.Serialization;

    [XmlRoot]
    public class RecentFiles
    {
        [XmlArrayItem("File")]
        public String[] Files;
    }
}