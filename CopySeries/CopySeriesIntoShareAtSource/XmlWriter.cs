namespace DoenaSoft.CopySeries
{
    using System;
    using System.IO;
    using ToolBox.Generics;

    internal static class XmlWriter
    {
        internal static void Write(FileInfo fileInfo, Xml.VideoInfo xml)
        {
            FileInfo xmlFI = GetXmlFileName(fileInfo);

            Serializer<Xml.VideoInfo>.Serialize(xmlFI.FullName, xml);
        }

        internal static FileInfo GetXmlFileName(FileInfo fileInfo)
        {
            String xmlFileName = fileInfo.Name + ".xml";

            FileInfo xmlFI = new FileInfo(Path.Combine(fileInfo.DirectoryName, xmlFileName));

            return (xmlFI);
        }
    }
}