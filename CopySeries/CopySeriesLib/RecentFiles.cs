using System;
using System.Xml.Serialization;

namespace DoenaSoft.CopySeries
{
    [XmlRoot]
    public class RecentFiles
    {
        [XmlArrayItem("File")]
        public String[] Files;
    }
}