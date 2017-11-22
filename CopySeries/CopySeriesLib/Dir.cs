using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries
{
    public static class Dir
    {
        public static void CreateFileList(String stickDrive
            , String targetDir)
        {
            IgnoreDirectories ignoreDirectories = Serializer<IgnoreDirectories>.Deserialize(stickDrive + "IgnoreDirectories.xml");

            Dictionary<String, Boolean> ignoreDirectoriesDict;
            if (ignoreDirectories.IgnoreDirectoryList != null)
            {
                ignoreDirectoriesDict = new Dictionary<String, Boolean>(ignoreDirectories.IgnoreDirectoryList.Length);

                foreach (String dir in ignoreDirectories.IgnoreDirectoryList)
                {
                    if (Directory.Exists(dir))
                    {
                        ignoreDirectoriesDict[dir] = true;
                    }
                }
            }
            else
            {
                ignoreDirectoriesDict = new Dictionary<String, Boolean>(0);
            }

            FileTypes fileTypes = Serializer<FileTypes>.Deserialize(stickDrive + "FileTypes.xml");

            List<String> allFiles = new List<String>(2500);

            if (fileTypes.FileTypeList?.Length > 0)
            {
                List<String> directories;

                directories = new List<String>(Directory.GetDirectories(targetDir, "*.*", SearchOption.AllDirectories));

                for (Int32 i = directories.Count - 1; i >= 0; i--)
                {
                    foreach (String key in ignoreDirectoriesDict.Keys)
                    {
                        if (directories[i].StartsWith(key))
                        {
                            directories.RemoveAt(i);

                            break;
                        }
                    }
                }

                foreach (String directory in directories)
                {
                    foreach (String fileType in fileTypes.FileTypeList)
                    {
                        allFiles.AddRange(Directory.GetFiles(directory, "*." + fileType, SearchOption.TopDirectoryOnly));
                    }
                }
            }

            allFiles.Sort();

            String dirFile = Path.Combine(stickDrive, "dir.txt");

            using (FileStream fs = new FileStream(dirFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding(1252)))
                {
                    foreach (String file in allFiles)
                    {
                        sw.WriteLine($"{file.Replace(targetDir, String.Empty)};{(new FileInfo(file)).Length}");
                    }
                }
            }
        }
    }
}