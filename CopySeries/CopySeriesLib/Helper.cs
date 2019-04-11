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

        private static Dictionary<string, Name> Names { get; set; }

        static Helper()
        {
            const string SeriesName = "(?'SeriesName'[A-Za-z0-9&-_]+?)";
            const string SeasonNumber = "(?'SeasonNumber'[0-9]+?)";
            const string EpisodeNumber = "(?'EpisodeNumber'[0-9a-z]+)";
            const string EpisodeName = "(?'EpisodeName'.+?)";

            NameRegex = new Regex("^" + SeriesName + " " + SeasonNumber + "x" + EpisodeNumber + @" \[ " + EpisodeName + @" \]\.");

            Names = null;
        }

        public static bool CopySeriesIntoShare(DirectoryInfo sourceDir, SearchOption searchOption, string targetDir, bool copy, string remoteDir, string namesDir, out List<EpisodeData> episodeList, out RecentFiles recentFiles)
        {
            var dateShows = Serializer<DateShows>.Deserialize(Path.Combine(namesDir, "DateShows.xml"));

            var dateShowShortNames = dateShows.ShortNameList ?? Enumerable.Empty<string>();

            ulong bytes;
            bool abort;
            List<FileInfo> fis;
            do
            {
                var mismatches = new Dictionary<string, bool>();

                fis = new List<FileInfo>(sourceDir.GetFiles("*.*", searchOption));

                for (var fileIndex = fis.Count - 1; fileIndex >= 0; fileIndex--)
                {
                    string name = fis[fileIndex].Name.ToLower();

                    if ((name == "thumbs.db") || (name.EndsWith(".lnk")) || (name.EndsWith(".title")))
                    {
                        fis.RemoveAt(fileIndex);

                        continue;
                    }
                }

                fis.Sort((left, right) => left.Name.CompareTo(right.Name));

                bytes = 0;

                abort = false;

                foreach (var fi in fis)
                {
                    bytes += (ulong)(fi.Length);

                    var match = NameRegex.Match(fi.Name);

                    if (match.Success)
                    {
                        var seriesName = match.Groups["SeriesName"].Value;

                        var seasonNumber = match.Groups["SeasonNumber"].Value;

                        if ((GetName(seriesName, namesDir, mismatches, out var name)) && (GetResolution(fi, out var resolution)))
                        {
                            CheckSeries(targetDir, name, seasonNumber, resolution, mismatches, ref abort);

                            CheckTitle(fi, match.Groups["EpisodeName"].Value, dateShowShortNames.Contains(name.ShortName), mismatches, ref abort);
                        }
                        else
                        {
                            abort = true;
                        }
                    }
                    else
                    {
                        var output = $"NameRegex fail: {fi.Name}";

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

            var driveInfo = new DriveInfo(targetDir.Substring(0, 1));

            var availableFreeSpace = (ulong)(driveInfo.AvailableFreeSpace);

            if (availableFreeSpace <= bytes)
            {
                var bytesSize = new FileSize(bytes);

                var spaceSize = new FileSize(availableFreeSpace);

                Console.WriteLine($"Drive if full!{Environment.NewLine}Available: {spaceSize}{Environment.NewLine}Needed: {bytesSize}");

                episodeList = null;

                recentFiles = null;

                return false;
            }

            recentFiles = new RecentFiles()
            {
                Files = new string[fis.Count]
            };

            episodeList = new List<EpisodeData>(fis.Count);

            for (var fileIndex = 0; fileIndex < fis.Count; fileIndex++)
            {
                var fi = fis[fileIndex];

                var match = NameRegex.Match(fi.Name);

                var seriesName = match.Groups["SeriesName"].Value;

                var seasonNumber = match.Groups["SeasonNumber"].Value;

                GetName(seriesName, namesDir, null, out var name);

                GetResolution(fi, out var resolution);

                var targetFile = $@"{targetDir}{name.LongName}\Season {seasonNumber}\{resolution}\{fi.Name}";

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

                recentFiles.Files[fileIndex] = targetFile.Replace(targetDir, remoteDir);

                var episodeData = GetEpisodeData(name, seasonNumber, fi, match, dateShowShortNames.Contains(name.ShortName));

                episodeList.Add(episodeData);

                Console.WriteLine(episodeData.ToString());
            }

            return (true);
        }

        private static void CheckTitle(FileInfo fi, string episodeName, bool isDateShow, Dictionary<string, bool> mismatches, ref bool abort)
        {
            if (isDateShow == false)
            {
                return;
            }

            var titleFile = fi.FullName + ".title";

            if (File.Exists(titleFile))
            {
                episodeName = GetEpisodeName(titleFile, false);
            }

            if (DateTime.TryParseExact(episodeName.Substring(0, 10), "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out var date) == false)
            {
                var output = $"Title DateTime fail: {episodeName}";

                if (mismatches.ContainsKey(output) == false)
                {
                    mismatches.Add(output, true);

                    Console.WriteLine(output);
                }

                abort = true;
            }
        }

        private static EpisodeData GetEpisodeData(Name name, string seasonNumber, FileInfo fi, Match match, bool isDateShow)
        {
            var addInfo = GetAddInfo(fi);

            var episodeNumber = match.Groups["EpisodeNumber"].Value;

            var titleFile = fi.FullName + ".title";

            var episodeName = File.Exists(titleFile) ? GetEpisodeName(titleFile) : match.Groups["EpisodeName"].Value;

            var fileSize = new FileSize((ulong)(fi.Length));

            var episodeData = new EpisodeData(name, seasonNumber, episodeNumber, isDateShow, episodeName, addInfo, fileSize);

            return episodeData;
        }

        private static string GetEpisodeName(string titleFile, bool delete = true)
        {
            string title;
            using (StreamReader sr = new StreamReader(titleFile, Encoding.UTF8))
            {
                title = sr.ReadLine();
            }

            if (delete)
            {
                File.Delete(titleFile);
            }

            return title;
        }

        private static bool GetResolution(FileInfo fi, out string resolution)
        {
            if ((fi.Name.EndsWith(".480.mkv")) || (fi.Name.EndsWith(".480.mp4")) || (fi.Name.EndsWith(".480.avi")))
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
                Console.WriteLine($"{fi.Name} does not contain a resolution: {fi.Extension}");

                resolution = null;

                return (false);
            }
            else
            {
                resolution = "SD";
            }

            return true;
        }

        public static string GetNewFileName(string fileName, string extension, string baseTargetDir, string subTargetDir)
        {
            var today = DateTime.Now;

            var appendix = $"{today.Year}_{today.Month:00}_{today.Day:00}";

            var newFileName = $"{fileName}.{appendix}.{extension}";

            ushort index = 1;

            while (File.Exists(Path.Combine(baseTargetDir, subTargetDir, newFileName)))
            {
                index++;

                newFileName = $"{fileName}.{appendix}#{index}.{extension}";
            }

            newFileName = Path.Combine(baseTargetDir, subTargetDir, newFileName);

            return newFileName;
        }

        public static void CleanFolder(string fileNamePattern, string extensionPattern, int weekOffset, string baseTargetDir, string subTargetDir)
        {
            var dateTime = DateTime.Now.AddDays(weekOffset * 7 * -1);

            var di = new DirectoryInfo(Path.Combine(baseTargetDir, subTargetDir));

            var fis = new List<FileInfo>(di.GetFiles(fileNamePattern + ".*." + extensionPattern, SearchOption.TopDirectoryOnly));

            foreach (var fi in fis)
            {
                if (fi.LastWriteTime < dateTime)
                {
                    fi.Delete();
                }
            }
        }

        private static string GetAddInfo(FileInfo fi)
        {
            string addinfo;
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

            return addinfo;
        }

        private static string GetResultionExtension(FileInfo fi)
        {
            string addInfo;
            if ((fi.Name.EndsWith(".480.mkv")) || (fi.Name.EndsWith(".480.mp4")) || (fi.Name.EndsWith(".480.avi")))
            {
                addInfo = "SD"; //".480" + fi.Extension;
            }
            else if ((fi.Name.EndsWith(".720.mkv")) || (fi.Name.EndsWith(".720.mp4")))
            {
                addInfo = "HD"; //".720" + fi.Extension;
            }
            else if ((fi.Name.EndsWith(".1080.mkv")) || (fi.Name.EndsWith(".1080.mp4")))
            {
                addInfo = "FullHD"; //".1080" + fi.Extension;
            }
            else
            {
                addInfo = fi.Extension.Substring(1);
            }

            return addInfo;
        }

        private static string GetSubtitleAddInfo(FileInfo fi)
        {
            var addInfo = "Subtitle, ";

            var newFileName = fi.Name.Substring(0, fi.Name.Length - 4);

            var newFileInfo = new FileInfo(newFileName);

            addInfo += GetResultionExtension(newFileInfo);

            return addInfo;
        }

        private static string GetNfoAddInfo(FileInfo fi)
        {
            var addInfo = "NFO, ";

            var newFileName = fi.Name.Substring(0, fi.Name.Length - 4);

            var newFileInfo = new FileInfo(newFileName);

            addInfo += GetResultionExtension(newFileInfo);

            return (addInfo);
        }

        private static void CheckSeries(string targetDir, Name name, string seasonNumber, string resolution, Dictionary<string, bool> mismatches, ref bool abort)
        {
            var folderInQuestion = $"{targetDir}{name.LongName}";

            if (Directory.Exists(folderInQuestion))
            {
                CheckNfo(folderInQuestion, name.Link);

                CheckSeason(folderInQuestion, seasonNumber, resolution, mismatches, ref abort);
            }
            else
            {
                var output = $"Folder missing fail: {folderInQuestion}";

                if (mismatches.ContainsKey(output) == false)
                {
                    Console.WriteLine(output);

                    Console.Write("Create? ");

                    var input = Console.ReadLine();

                    input = input.ToLower();

                    if ((input == "y") || (input == "yes"))
                    {
                        Directory.CreateDirectory(folderInQuestion);

                        CheckNfo(folderInQuestion, name.Link);

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

        private static void CheckNfo(string seriesFolder, string link)
        {
            if (string.IsNullOrEmpty(link))
            {
                return;
            }

            var fileName = Path.Combine(seriesFolder, "tvshow.nfo");

            if (File.Exists(fileName))
            {
                return;
            }

            using (var sw = new StreamWriter(fileName, false, Encoding.UTF8))
            {
                sw.WriteLine(link);
            }
        }

        private static void CheckSeason(string targetDir, string seasonNumber, string resolution, Dictionary<string, bool> mismatches, ref bool abort)
        {
            var folderInQuestion = $@"{targetDir}\Season {seasonNumber}";

            if (Directory.Exists(folderInQuestion))
            {
                CheckResolution(folderInQuestion, resolution, mismatches, ref abort);
            }
            else
            {
                var output = $"Folder missing fail: {folderInQuestion}";

                if (mismatches.ContainsKey(output) == false)
                {
                    Console.WriteLine(output);

                    Console.Write("Create? ");

                    var input = Console.ReadLine();
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

        private static void CheckResolution(string targetDir, string resolution, Dictionary<string, bool> mismatches, ref bool abort)
        {
            var folderInQuestion = $@"{targetDir}\{resolution}";

            if (Directory.Exists(folderInQuestion) == false)
            {
                var output = $"Folder missing fail: {folderInQuestion}";

                if (mismatches.ContainsKey(output) == false)
                {
                    Console.WriteLine(output);

                    Console.Write("Create? ");

                    var input = Console.ReadLine().ToLower();

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

        private static bool GetName(string shortName, string targetDir, Dictionary<string, bool> mismatches, out Name name)
        {
            if (Names == null)
            {
                var nameList = Serializer<Names>.Deserialize(Path.Combine(targetDir, "Names.xml"));

                Names = new Dictionary<string, Name>();

                if (nameList.NameList?.Length > 0)
                {
                    foreach (var item in nameList.NameList)
                    {
                        Names.Add(item.ShortName, item);
                    }
                }

                nameList = new Names()
                {
                    NameList = Names.Values.OrderBy(n => n, Comparer<Name>.Create((left, right) => left.SortName.CompareTo(right.SortName))).ToArray(),
                };

                Serializer<Names>.Serialize(Path.Combine(targetDir, "Names.xml"), nameList);
            }

            bool success = Names.TryGetValue(shortName, out name);

            if (success == false)
            {
                var output = $"Names.xml fail: {shortName}";

                if (mismatches.ContainsKey(output) == false)
                {
                    mismatches.Add(output, true);

                    Console.WriteLine(output);
                }
            }

            return success;
        }
    }
}