namespace DoenaSoft.CopySeries
{
    using System.Xml.Serialization;

    [XmlRoot]
    public class IgnoreDirectories
    {
        [XmlArrayItem("IgnoreDirectory")]
        public string[] IgnoreDirectoryList;
    }
}