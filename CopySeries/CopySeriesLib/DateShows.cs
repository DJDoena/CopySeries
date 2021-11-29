namespace DoenaSoft.CopySeries
{
    using System.Xml.Serialization;

    [XmlRoot]
    public sealed class DateShows
    {
        [XmlArrayItem("ShortName")]
        public string[] ShortNameList;
    }
}