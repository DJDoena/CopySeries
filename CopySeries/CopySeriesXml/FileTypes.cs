namespace DoenaSoft.CopySeries
{
    using System.Xml.Serialization;

    [XmlRoot]
    public class FileTypes
    {
        [XmlArrayItem("FileType")]
        public string[] FileTypeList;
    }
}