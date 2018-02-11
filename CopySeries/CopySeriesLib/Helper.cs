namespace DoenaSoft.CopySeries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using ToolBox.Generics;

    public static class Helper
    {
        private static Regex NameRegex { get; }

        private static Dictionary<String, Name> Names { get; set; }

        static Helper()
        {
            const String SeriesName = "(?'SeriesName'[A-Za-z0-9&-_]+?)";
            const String SeasonNumber = "(?'SeasonNumber'[0-9]+?)";
            const String EpisodeNumber = "(?'EpisodeNumber'[0-9a-z]+)";
            const String EpisodeName = "(?'EpisodeName'.+?)";

            NameRegex = new Regex("^" + SeriesName + " " + SeasonNumber + "x" + EpisodeNumber + @" \[ " + EpisodeName + @" \]\.");

            Names = null;
        }

        public static Boolean CopySeriesIntoShare(DirectoryInfo sourceDir
            , SearchOption searchOption
            , String targetDir
            , Boolean copy
            , String remoteDir
            , String namesDir
            , out List<EpisodeData> episodeList
            , out RecentFiles recentFiles)
        {
            Int64 bytes;
            Boolean abort;
            List<FileInfo> fis;
            do
            {
                Dictionary<String, Boolean> mismatches = new Dictionary<String, Boolean>();

                fis = new List<FileInfo>(sourceDir.GetFiles("*.*", searchOption));

                for (Int32 i = fis.Count - 1; i >= 0; i--)
                {
                    String name = fis[i].Name.ToLower();

                    if ((name == "thumbs.db") || (name.EndsWith(".lnk")) || (name.EndsWith(".title")))
                    {
                        fis.RemoveAt(i);

                        continue;
                    }
                }

                fis.Sort((left, right) => (left.Name.CompareTo(right.Name)));

                bytes = 0;

                abort = false;

                foreach (FileInfo fi in fis)
                {
                    bytes += fi.Length;

                    Match match = NameRegex.Match(fi.Name);

                    if (match.Success)
                    {
                        String seriesName = match.Groups["SeriesName"].Value;

                        String seasonNumber = match.Groups["SeasonNumber"].Value;

                        if ((GetName(seriesName, namesDir, mismatches, out Name name)) && (GetResolution(fi, out String resolution)))
                        {
                            CheckSeries(targetDir, mismatches, name.LongName, seasonNumber, resolution, ref abort);
                        }
                        else
                        {
                            abort = true;
                        }
                    }
                    else
                    {
                        String output = $"No Match: {fi.Name}";

                        if (mismatches.ContainsKey(output) == false)
                        {
                            mismatches.Add(output, true);

                            Console.WriteLine(output);
                        }

                        abort = true;
                    }
                }

                if (abort)
                {
                    Names = null;

                    Console.WriteLine();
                    Console.WriteLine("Press <Enter> to retry.");

                    Console.ReadLine();
                }
            } while (abort == true);

            DriveInfo driveInfo = new DriveInfo(targetDir.Substring(0, 1));

            if (driveInfo.AvailableFreeSpace <= bytes)
            {
                FileSize bytesSize = new FileSize(bytes);

                FileSize spaceSize = new FileSize(driveInfo.AvailableFreeSpace);

                Console.WriteLine($"Drive if full!{Environment.NewLine}Available: {spaceSize}{Environment.NewLine}Needed: {bytesSize}");

                episodeList = null;

                recentFiles = null;

                return (false);
            }

            recentFiles = new RecentFiles()
            {
                Files = new String[fis.Count]
            };

            episodeList = new List<EpisodeData>(fis.Count);

            DateShows dateShows = Serializer<DateShows>.Deserialize(Path.Combine(namesDir, "DateShows.xml"));

            IEnumerable<String> dateShowShortNames = dateShows.ShortNameList ?? Enumerable.Empty<String>();

            for (Int32 i = 0; i < fis.Count; i++)
            {
                FileInfo fi = fis[i];

                Match match = NameRegex.Match(fi.Name);

                String seriesName = match.Groups["SeriesName"].Value;

                String seasonNumber = match.Groups["SeasonNumber"].Value;

                GetName(seriesName, namesDir, null, out Name name);

                GetResolution(fi, out String resolution);

                String targetFile = $@"{targetDir}{name.LongName}\Season {seasonNumber}\{resolution}\{fi.Name}";

                if (File.Exists(targetFile))
                {
                    File.SetAttributes(targetFile, FileAttributes.Normal);

                    File.Delete(targetFile);
                }

                if (copy)
                {
                    File.Copy(fi.FullName, targetFile, true);
                }
                else
                {
                    if (File.Exists(targetFile))
                    {
                        File.Delete(targetFile);
                    }

                    File.Move(fi.FullName, targetFile);
                }

                File.SetAttributes(targetFile, FileAttributes.Archive | FileAttributes.ReadOnly);

                recentFiles.Files[i] = targetFile.Replace(targetDir, remoteDir);

                EpisodeData episodeData = GetEpisodeData(name, seasonNumber, fi, match, dateShowShortNames.Contains(name.ShortName));

                episodeList.Add(episodeData);

                Console.WriteLine(episodeData.ToString());
            }

            return (true);
        }

        private static EpisodeData GetEpisodeData(Name name
            , String seasonNumber
            , FileInfo fi
            , Match match
            , Boolean isDateShow)
        {
            String addInfo = GetAddInfo(fi);

            String episodeNumber = match.Groups["EpisodeNumber"].Value;

            String titleFile = fi.FullName + ".title";

            String episodeName = File.Exists(titleFile) ? GetEpisodeName(titleFile) : match.Groups["EpisodeName"].Value;

            FileSize fileSize = new FileSize(fi.Length);

            EpisodeData episodeData = new EpisodeData(name, seasonNumber, episodeNumber, isDateShow, episodeName, addInfo, fileSize);

            return (episodeData);
        }

        private static String GetEpisodeName(String titleFile)
        {
            String title;
            using (StreamReader sr = new StreamReader(titleFile, Encoding.UTF8))
            {
                title = sr.ReadLine();
            }

            File.Delete(titleFile);

            return (title);
        }

        private static Boolean GetResolution(FileInfo fi
            , out String resolution)
        {
            if ((fi.Name.EndsWith(".480.mkv")) || (fi.Name.EndsWith(".480.mp4")))
            {
                resolution = "SD";
            }
            else if ((fi.Name.EndsWith(".720.mkv")) || (fi.Name.EndsWith(".1080.mkv"))
                || (fi.Name.EndsWith(".720.mp4")) || (fi.Name.EndsWith(".1080.mp4"))
                || (fi.Name.EndsWith(".720.nfo")) || (fi.Name.EndsWith(".1080.nfo")))
            {
                resolution = "HD";
            }
            else if ((fi.Name.EndsWith(".mkv")) || (fi.Name.EndsWith(".mp4")))
            {
                Console.WriteLine($"{fi.Extension} is not a valid file extension: {fi.Name}");

                resolution = null;

                return (false);
            }
            else
            {
                resolution = "SD";
            }

            return (true);
        }

        public static String GetNewFileName(String fileName
           , String extension
           , String baseTargetDir
           , String subTargetDir)
        {
            DateTime today = DateTime.Now;

            String appendix = $"{today.Year}_{today.Month:00}_{today.Day:00}";

            String newFileName = $"{fileName}.{appendix}.{extension}";

            UInt16 index = 1;

            while (File.Exists(Path.Combine(baseTargetDir, subTargetDir, newFileName)))
            {
                index++;

                newFileName = $"{fileName}.{appendix}#{index}.{extension}";
            }

            newFileName = Path.Combine(baseTargetDir, subTargetDir, newFileName);

            return (newFileName);
        }

        public static void CleanFolder(String fileNamePattern
            , String extensionPattern
            , Int32 weekOffset
            , String baseTargetDir
            , String subTargetDir)
        {
            DateTime dateTime = DateTime.Now.AddDays(weekOffset * 7 * -1);

            DirectoryInfo di = new DirectoryInfo(Path.Combine(baseTargetDir, subTargetDir));

            List<FileInfo> fis = new List<FileInfo>(di.GetFiles(fileNamePattern + ".*." + extensionPattern, SearchOption.TopDirectoryOnly));

            foreach (FileInfo fi in fis)
            {
                if (fi.LastWriteTime < dateTime)
                {
                    fi.Delete();
                }
            }
        }

        private static String GetAddInfo(FileInfo fi)
        {
            String addinfo;
            if (fi.Extension == ".srt")
            {
                addinfo = GetSubtitleAddInfo(fi);
            }
            if (fi.Extension == ".nfo")
            {
                addinfo = GetNfoAddInfo(fi);
            }
            else if ((fi.Extension == ".mkv") || (fi.Extension == ".mp4"))
            {
                addinfo = GetResultionExtension(fi);
            }
            else
            {
                addinfo = fi.Extension.Substring(1);
            }

            return (addinfo);
        }

        private static String GetResultionExtension(FileInfo fi)
        {
            String addInfo;
            if ((fi.Name.EndsWith(".480.mkv")) || (fi.Name.EndsWith(".480.mp4")))
            {
                addInfo = "SD"; //".480" + fi.Extension;
            }
            else if ((fi.Name.EndsWith(".720.mkv")) || (fi.Name.EndsWith(".720.mp4")))
            {
                addInfo = "HD"; //".720" + fi.Extension;
            }
            else if ((fi.Name.EndsWith(".1080.mkv")) || (fi.Name.EndsWith(".1080.mp4")))
            {
                addInfo = "HD";//".1080" + fi.Extension;
            }
            else
            {
                addInfo = fi.Extension.Substring(1);
            }

            return (addInfo);
        }

        private static String GetSubtitleAddInfo(FileInfo fi)
        {
            String addInfo = "Subtitle, ";

            String newFileName = fi.Name.Substring(0, fi.Name.Length - 4);

            FileInfo newFileInfo = new FileInfo(newFileName);

            addInfo += GetResultionExtension(newFileInfo);

            return (addInfo);
        }

        private static String GetNfoAddInfo(FileInfo fi)
        {
            String addInfo = "NFO, ";

            String newFileName = fi.Name.Substring(0, fi.Name.Length - 4);

            FileInfo newFileInfo = new FileInfo(newFileName);

            addInfo += GetResultionExtension(newFileInfo);

            return (addInfo);
        }

        private static void CheckSeries(String targetDir
            , Dictionary<String, Boolean> mismatches
            , String seriesName
            , String seasonNumber
            , String resolution
            , ref Boolean abort)
        {
            String folderInQuestion = $"{targetDir}{seriesName}";

            if (Directory.Exists(folderInQuestion))
            {
                CheckSeason(folderInQuestion, seasonNumber, resolution, mismatches, ref abort);
            }
            else
            {
                String output = $"No Match: {folderInQuestion}";

                if (mismatches.ContainsKey(output) == false)
                {
                    Console.WriteLine(output);

                    Console.Write("Create? ");

                    String input = Console.ReadLine();

                    input = input.ToLower();

                    if ((input == "y") || (input == "yes"))
                    {
                        Directory.CreateDirectory(folderInQuestion);

                        CheckSeason(folderInQuestion, seasonNumber, resolution, mismatches, ref abort);
                    }
                    else
                    {
                        mismatches.Add(output, true);

                        abort = true;
                    }
                }
                else
                {
                    abort = true;
                }
            }
        }

        private static void CheckSeason(String targetDir
            , String seasonNumber
            , String resolution
            , Dictionary<String, Boolean> mismatches
            , ref Boolean abort)
        {
            String folderInQuestion = $@"{targetDir}\Season {seasonNumber}";

            if (Directory.Exists(folderInQuestion))
            {
                CheckResolution(folderInQuestion, resolution, mismatches, ref abort);
            }
            else
            {
                String output = $"No Match: {folderInQuestion}";

                if (mismatches.ContainsKey(output) == false)
                {
                    String input;

                    Console.WriteLine(output);

                    Console.Write("Create? ");

                    input = Console.ReadLine();
                    input = input.ToLower();

                    if ((input == "y") || (input == "yes"))
                    {
                        Directory.CreateDirectory(folderInQuestion);

                        CheckResolution(folderInQuestion, resolution, mismatches, ref abort);
                    }
                    else
                    {
                        mismatches.Add(output, true);

                        abort = true;
                    }
                }
                else
                {
                    abort = true;
                }
            }
        }

        private static void CheckResolution(String targetDir
            , String resolution
            , Dictionary<String, Boolean> mismatches
            , ref Boolean abort)
        {
            String folderInQuestion = $@"{targetDir}\{resolution}";

            if (Directory.Exists(folderInQuestion) == false)
            {
                String output = $"No Match: {folderInQuestion}";

                if (mismatches.ContainsKey(output) == false)
                {
                    Console.WriteLine(output);

                    Console.Write("Create? ");

                    String input = Console.ReadLine().ToLower();

                    if ((input == "y") || (input == "yes"))
                    {
                        Directory.CreateDirectory(folderInQuestion);
                    }
                    else
                    {
                        mismatches.Add(output, true);

                        abort = true;
                    }
                }
                else
                {
                    abort = true;
                }
            }
        }

        private static Boolean GetName(String shortName
            , String targetDir
            , Dictionary<String, Boolean> mismatches
            , out Name name)
        {
            if (Names == null)
            {
                Names nameList = Serializer<Names>.Deserialize(Path.Combine(targetDir, "Names.xml"));

                Names = new Dictionary<String, Name>();

                if (nameList.NameList?.Length > 0)
                {
                    foreach (Name item in nameList.NameList)
                    {
                        Names.Add(item.ShortName, item);
                    }
                }
            }

            Boolean success = Names.TryGetValue(shortName, out name);

            if (success == false)
            {
                String output = $"No Match: {shortName}";

                if (mismatches.ContainsKey(output) == false)
                {
                    mismatches.Add(output, true);

                    Console.WriteLine(output);
                }
            }

            return (success);
        }
    }
}