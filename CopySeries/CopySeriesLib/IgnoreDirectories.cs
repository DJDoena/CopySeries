using System;
using System.Xml.Serialization;

namespace DoenaSoft.CopySeries
{
    [XmlRoot]
    public class IgnoreDirectories
    {
        [XmlArrayItem("IgnoreDirectory")]
        public String[] IgnoreDirectoryList;
    }
}