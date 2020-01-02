namespace DoenaSoft.CopySeries
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
    using DoenaSoft.MediaInfoHelper;

    internal static class XmlWriter
    {
        private static string _prefix;

        private static string _suffix;

        internal static void Write(FileInfo fileInfo, VideoInfo instance)
        {
            var xmlFI = GetXmlFileName(fileInfo);

            var xml = Serialize(instance);

            xml = "\t" + xml.Replace(Environment.NewLine, Environment.NewLine + "\t");

            using (var sw = new StreamWriter(xmlFI.FullName, false, Encoding.UTF8))
            {
                sw.WriteLine(GetPrefix());
                sw.WriteLine(xml);
                sw.Write(GetSuffix());
            }
        }

        internal static string GetPrefix()
        {
            if (_prefix == null)
            {
                _prefix = GetContent("XslPrefix.txt");
            }

            return _prefix;
        }

        internal static string GetSuffix()
        {
            if (_suffix == null)
            {
                _suffix = GetContent("XslSuffix.txt");
            }

            return _suffix;
        }

        internal static FileInfo GetXmlFileName(FileInfo fileInfo)
        {
            var xmlFileName = fileInfo.Name + ".xml";

            var xmlFI = new FileInfo(Path.Combine(fileInfo.DirectoryName, xmlFileName));

            return xmlFI;
        }

        private static string Serialize(VideoInfo instance)
        {
            using (var ms = new MemoryStream())
            {
                var encoding = Encoding.UTF8;

                var settings = new XmlWriterSettings()
                {
                    Encoding = encoding,
                    Indent = true,
                    OmitXmlDeclaration = true,
                    IndentChars = "\t",
                };

                using (var writer = System.Xml.XmlWriter.Create(ms, settings))
                {
                    Serializer<VideoInfo>.XmlSerializer.Serialize(writer, instance, new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }));

                    var xml = encoding.GetString(ms.ToArray());

                    return xml;
                }
            }
        }

        private static string GetContent(string file)
        {
            using (var sr = new StreamReader(file))
            {
                var content = sr.ReadToEnd();

                return content;
            }
        }
    }
}