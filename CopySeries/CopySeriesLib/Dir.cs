namespace DoenaSoft.CopySeries
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using ToolBox.Generics;

    public static class Dir
    {
        public static void CreateFileList(string stickDrive, string targetDir)
        {
            var ignoreDirectories = Serializer<IgnoreDirectories>.Deserialize(stickDrive + "IgnoreDirectories.xml");

            Dictionary<string, bool> ignoreDirectoriesDict;
            if (ignoreDirectories.IgnoreDirectoryList != null)
            {
                ignoreDirectoriesDict = new Dictionary<string, bool>(ignoreDirectories.IgnoreDirectoryList.Length);

                foreach (var dir in ignoreDirectories.IgnoreDirectoryList)
                {
                    if (Directory.Exists(dir))
                    {
                        ignoreDirectoriesDict[dir] = true;
                    }
                }
            }
            else
            {
                ignoreDirectoriesDict = new Dictionary<string, bool>(0);
            }

            var fileTypes = Serializer<FileTypes>.Deserialize(stickDrive + "FileTypes.xml");

            var allFiles = new List<string>(2500);

            if (fileTypes.FileTypeList?.Length > 0)
            {
                var directories = new List<string>(Directory.GetDirectories(targetDir, "*.*", SearchOption.AllDirectories));

                for (var directoryIndex = directories.Count - 1; directoryIndex >= 0; directoryIndex--)
                {
                    foreach (var key in ignoreDirectoriesDict.Keys)
                    {
                        if (directories[directoryIndex].StartsWith(key))
                        {
                            directories.RemoveAt(directoryIndex);

                            break;
                        }
                    }
                }

                foreach (var directory in directories)
                {
                    foreach (var fileType in fileTypes.FileTypeList)
                    {
                        allFiles.AddRange(Directory.GetFiles(directory, "*." + fileType, SearchOption.TopDirectoryOnly));
                    }
                }
            }

            allFiles.Sort();

            var dirFile = Path.Combine(stickDrive, "dir.txt");

            using (var fs = new FileStream(dirFile, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (var sw = new StreamWriter(fs, Encoding.GetEncoding(1252)))
                {
                    foreach (var file in allFiles)
                    {
                        sw.WriteLine($"{file.Replace(targetDir, string.Empty)};{(new FileInfo(file)).Length}");
                    }
                }
            }
        }
    }
}