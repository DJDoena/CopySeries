namespace DoenaSoft.CopySeries
{
    using System;
    using System.Xml.Serialization;

    [XmlRoot]
    public class IgnoreDirectories
    {
        [XmlArrayItem("IgnoreDirectory")]
        public String[] IgnoreDirectoryList;
    }
}