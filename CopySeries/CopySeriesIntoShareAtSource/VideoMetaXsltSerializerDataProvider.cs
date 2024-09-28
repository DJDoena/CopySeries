using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries;

internal sealed class VideoMetaXsltSerializerDataProvider : IXsltSerializerDataProvider
{
    private string _prefix;

    private string _suffix;

    public string GetPrefix()
        => _prefix ??= GetContent("XslPrefix.txt");

    public string GetSuffix()
        => _suffix ??= GetContent("XslSuffix.txt");

    private static string GetContent(string file)
    {
        using var sr = new StreamReader(file);
        var content = sr.ReadToEnd();

        return content;
    }
}