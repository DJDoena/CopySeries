using System.Diagnostics;
using System.Text;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries;

public static class Program
{
    private static string TargetDir { get; } = DirAtSourceSettings.Default.LocalDir;

    private static string DirFile { get; } = DirAtSourceSettings.Default.StickDrive + "dir@home.txt";

    private static string RemoteDirFile { get; } = DirAtSourceSettings.Default.StickDrive + "dir.txt";

    private static string FileTypesFile { get; } = DirAtSourceSettings.Default.StickDrive + "FileTypes.xml";

    private static string BeyondCompareFile { get; } = DirAtSourceSettings.Default.BeyondCompare;

    [STAThread]
    private static void Main(string[] args)
    {
        Console.WriteLine(typeof(Program).Assembly.GetName().Version);

        var dict = new Dictionary<string, List<string>>();

        var full = false;

        if ((args?.Length > 0) && (args[0] == "/full"))
        {
            full = true;
        }

        if (full == false)
        {
            using (var fs = new FileStream(RemoteDirFile, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var sr = new StreamReader(fs, Encoding.GetEncoding(1252)))
                {
                    while (sr.EndOfStream == false)
                    {
                        var line = sr.ReadLine();

                        var split = line.Split('\\');

                        if (split.Length > 3)
                        {
                            var key = split[0] + @"\" + split[1] + @"\" + split[2];

                            if (dict.TryGetValue(key, out var extensions) == false)
                            {
                                dict.Add(key, null);
                            }
                        }
                    }
                }
            }
        }

        var fileTypes = XmlSerializer<FileTypes>.Deserialize(FileTypesFile);

        List<string> allFiles;
        if (fileTypes.FileTypeList != null)
        {
            allFiles = new List<string>(2500);

            foreach (var fileType in fileTypes.FileTypeList)
            {
                allFiles.AddRange(Directory.GetFiles(TargetDir, "*." + fileType, SearchOption.AllDirectories));
            }
        }
        else
        {
            allFiles = new List<string>(0);
        }

        allFiles.Sort();

        using (var fs = new FileStream(DirFile, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1252)))
            {
                foreach (var file in allFiles)
                {
                    var shortFile = file.Replace(TargetDir, "");

                    if (shortFile.StartsWith(@"sonstiges\"))
                    {
                        continue;
                    }

                    if (shortFile.StartsWith(@"lustig\"))
                    {
                        continue;
                    }

                    if (shortFile.StartsWith(@"The Ultimate Pilot Collection\"))
                    {
                        continue;
                    }

                    if (full == false)
                    {
                        var split = shortFile.Split('\\');

                        if (split.Length > 3)
                        {
                            var key = split[0] + @"\" + split[1] + @"\" + split[2];

                            if (dict.TryGetValue(key, out var extensions) == false)
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }
                    }

                    sw.WriteLine($"{shortFile};{new FileInfo(file).Length}");
                }
            }
        }

        Process.Start(BeyondCompareFile, "\"" + RemoteDirFile + "\" \"" + DirFile + "\"");
    }
}