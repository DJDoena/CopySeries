using System;
using System.Xml.Serialization;

namespace DoenaSoft.CopySeries
{
    [XmlRoot]
    public class FileTypes
    {
        [XmlArrayItem("FileType")]
        public String[] FileTypeList;
    }
}