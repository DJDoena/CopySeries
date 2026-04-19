using DoenaSoft.MediaInfoHelper.DataObjects.VideoMetaXml;
using DoenaSoft.MediaInfoHelper.Writers;

namespace DoenaSoft.CopySeries;

public static class XmlWriter
{
    public static void Write(FileInfo fileInfo, VideoMeta instance)
        => MetaWriter.WriteVideoMetaDocument(instance, GetXmlFileName(fileInfo).FullName);

    public static FileInfo GetXmlFileName(FileInfo fileInfo)
    {
        var xmlFileName = fileInfo.Name + ".xml";

        var xmlFI = new FileInfo(Path.Combine(fileInfo.DirectoryName, xmlFileName));

        return xmlFI;
    }
}