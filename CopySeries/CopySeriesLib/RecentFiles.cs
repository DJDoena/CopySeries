namespace DoenaSoft.CopySeries
{
    using System.Xml.Serialization;

    [XmlRoot]
    public class RecentFiles
    {
        [XmlArrayItem("File")]
        public string[] Files;
    }
}