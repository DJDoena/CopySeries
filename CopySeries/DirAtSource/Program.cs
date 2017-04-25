using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries
{
    public static class Program
    {
        private static readonly String TargetDir = DirAtSourceSettings.Default.LocalDir;

        private static readonly String DirFile = DirAtSourceSettings.Default.StickDrive + "dir@home.txt";

        private static readonly String RemoteDirFile = DirAtSourceSettings.Default.StickDrive + "dir.txt";

        private static readonly String FileTypesFile = DirAtSourceSettings.Default.StickDrive + "FileTypes.xml";

        private static readonly String BeyondCompareFile = DirAtSourceSettings.Default.BeyondCompare;

        [STAThread]
        static void Main(String[] args)
        {
            Dictionary<String, List<String>> dict = new Dictionary<String, List<String>>();

            Boolean full = false;

            if ((args?.Length > 0) && (args[0] == "/full"))
            {
                full = true;
            }

            if (full == false)
            {
                using (FileStream fs = new FileStream(RemoteDirFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding(1252)))
                    {
                        while (sr.EndOfStream == false)
                        {
                            String line = sr.ReadLine();

                            String[] split = line.Split('\\');

                            if (split.Length > 3)
                            {
                                String key = split[0] + @"\" + split[1] + @"\" + split[2];

                                List<String> extensions;
                                if (dict.TryGetValue(key, out extensions) == false)
                                {
                                    dict.Add(key, null);
                                }
                            }
                        }
                    }
                }
            }

            FileTypes fileTypes = Serializer<FileTypes>.Deserialize(FileTypesFile);

            List<String> allFiles;
            if (fileTypes.FileTypeList != null)
            {
                allFiles = new List<String>(2500);

                foreach (String fileType in fileTypes.FileTypeList)
                {
                    allFiles.AddRange(Directory.GetFiles(TargetDir, "*." + fileType, SearchOption.AllDirectories));
                }
            }
            else
            {
                allFiles = new List<String>(0);
            }

            allFiles.Sort();

            using (FileStream fs = new FileStream(DirFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding(1252)))
                {
                    foreach (String file in allFiles)
                    {
                        String shortFile = file.Replace(TargetDir, "");

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
                            String[] split = shortFile.Split('\\');

                            if (split.Length > 3)
                            {
                                List<String> extensions;
                                String key = split[0] + @"\" + split[1] + @"\" + split[2];

                                if (dict.TryGetValue(key, out extensions) == false)
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }

                        sw.WriteLine(shortFile);
                    }
                }
            }

            Process.Start(BeyondCompareFile, "\"" + RemoteDirFile + "\" \"" + DirFile + "\"");
        }
    }
}