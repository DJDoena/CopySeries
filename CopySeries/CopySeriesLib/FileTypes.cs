namespace DoenaSoft.CopySeries
{
    using System;
    using System.Xml.Serialization;

    [XmlRoot]
    public class FileTypes
    {
        [XmlArrayItem("FileType")]
        public String[] FileTypeList;
    }
}