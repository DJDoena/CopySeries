using System;
using System.Xml.Serialization;

namespace DoenaSoft.CopySeries
{
    [XmlRoot]
    public sealed class DateShows
    {
        [XmlArrayItem("ShortName")]
        public String[] ShortNameList;
    }
}