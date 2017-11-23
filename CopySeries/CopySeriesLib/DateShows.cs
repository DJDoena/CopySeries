namespace DoenaSoft.CopySeries
{
    using System;
    using System.Xml.Serialization;

    [XmlRoot]
    public sealed class DateShows
    {
        [XmlArrayItem("ShortName")]
        public String[] ShortNameList;
    }
}