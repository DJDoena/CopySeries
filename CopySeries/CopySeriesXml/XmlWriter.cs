using DoenaSoft.MediaInfoHelper.DataObjects.VideoMetaXml;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries;

public static class XmlWriter
{
    public static void Write(FileInfo fileInfo, VideoMeta instance)
        => (new XsltSerializer<VideoMeta>(new VideoMetaXsltSerializerDataProvider())).Serialize(fileInfo.FullName, instance);

    public static FileInfo GetXmlFileName(FileInfo fileInfo)
    {
        var xmlFileName = fileInfo.Name + ".xml";

        var xmlFI = new FileInfo(Path.Combine(fileInfo.DirectoryName, xmlFileName));

        return xmlFI;
    }
}