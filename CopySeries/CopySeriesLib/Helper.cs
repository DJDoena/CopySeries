﻿using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries;

public static class Helper
{
    public static Regex NameRegex { get; }

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
        var dateShows = XmlSerializer<DateShows>.Deserialize(Path.Combine(namesDir, "DateShows.xml"));

        var dateShowShortNames = dateShows.ShortNameList ?? Enumerable.Empty<string>();

        var targetDrive = new DriveInfo(targetDir.Substring(0, 1));

        ulong bytes;
        ulong targetBytes;
        bool abort;
        List<FileInfo> fis;
        do
        {
            var mismatches = new Dictionary<string, bool>();

            fis = new List<FileInfo>(sourceDir.GetFiles("*.*", searchOption));

            for (var fileIndex = fis.Count - 1; fileIndex >= 0; fileIndex--)
            {
                var name = fis[fileIndex].Name.ToLower();

                if ((name == "thumbs.db") || name.EndsWith(".lnk") || name.EndsWith(".title"))
                {
                    fis.RemoveAt(fileIndex);

                    continue;
                }
            }

            fis.Sort((left, right) => left.Name.CompareTo(right.Name));

            bytes = 0;

            targetBytes = 0;

            abort = false;

            foreach (var fi in fis)
            {
                var fileLength = (ulong)fi.Length;

                bytes += fileLength;

                var sourceDrive = new DriveInfo(fi.FullName.Substring(0, 1));

                if (sourceDrive.Name != targetDrive.Name)
                {
                    targetBytes += fileLength;
                }

                var match = NameRegex.Match(fi.Name);

                if (match.Success)
                {
                    var seriesName = match.Groups["SeriesName"].Value;

                    var seasonNumber = match.Groups["SeasonNumber"].Value;

                    if (GetName(seriesName, namesDir, mismatches, out var name) && GetResolution(fi, out var resolution))
                    {
                        CheckSeries(targetDir, name, seasonNumber, resolution, mismatches, fi.Name, ref abort);

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

        var availableFreeSpace = (ulong)targetDrive.AvailableFreeSpace;

        if (availableFreeSpace <= targetBytes)
        {
            var bytesSize = new FileSize(targetBytes);

            var spaceSize = new FileSize(availableFreeSpace);

            Console.WriteLine($"Drive if full!{Environment.NewLine}Available: {spaceSize}{Environment.NewLine}Needed: {bytesSize}");

            episodeList = null;

            recentFiles = null;

            return false;
        }

        recentFiles = new RecentFiles()
        {
            Files = new string[fis.Count],
        };

        episodeList = new List<EpisodeData>(fis.Count);

        var episodeNames = new HashSet<string>();
        for (var fileIndex = 0; fileIndex < fis.Count; fileIndex++)
        {
            var fileName = fis[fileIndex];

            if (fileName.Extension != ".mkv")
            {
                continue;
            }

            var match = NameRegex.Match(fileName.Name);

            var episodeName = match.Groups["EpisodeName"].Value;

            if (!episodeNames.Add(episodeName))
            {
                Console.WriteLine($"Episode name '{episodeName}' seems to be a duplicate. Continue?");

                var duplicateContinue = Console.ReadLine().Trim().ToLower();

                if (duplicateContinue != "y")
                {
                    return false;
                }
            }
        }

        for (var fileIndex = 0; fileIndex < fis.Count; fileIndex++)
        {
            var fileName = fis[fileIndex];

            var match = NameRegex.Match(fileName.Name);

            var seriesName = match.Groups["SeriesName"].Value;

            var seasonNumber = match.Groups["SeasonNumber"].Value;

            GetName(seriesName, namesDir, null, out var name);

            GetResolution(fileName, out var resolution);

            var seasonFolderName = fileName.Name.Contains("].de")
                ? "Staffel"
                : "Season";

            var targetFile = $@"{targetDir}{name.LongName}\{seasonFolderName} {seasonNumber}\{resolution}\{fileName.Name}";

            if (File.Exists(targetFile))
            {
                File.SetAttributes(targetFile, FileAttributes.Normal);

                File.Delete(targetFile);
            }

            if (copy)
            {
                File.Copy(fileName.FullName, targetFile, true);
            }
            else
            {
                if (File.Exists(targetFile))
                {
                    File.Delete(targetFile);
                }

                File.Move(fileName.FullName, targetFile);
            }

            File.SetAttributes(targetFile, FileAttributes.Archive);

            recentFiles.Files[fileIndex] = targetFile.Replace(targetDir, remoteDir);

            if (!targetFile.EndsWith(".nfo")
                && !targetFile.EndsWith(".srt")
                && !targetFile.EndsWith(".sub")
                && !targetFile.EndsWith(".idx"))
            {
                var episodeData = GetEpisodeData(name, seasonNumber, fileName, match, dateShowShortNames.Contains(name.ShortName));

                episodeList.Add(episodeData);

                Console.WriteLine(episodeData.ToString());
            }
            else
            {
                episodeList.Add(null);
            }
        }

        return true;
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

        var fileSize = new FileSize((ulong)fi.Length);

        var episodeData = new EpisodeData(name, seasonNumber, episodeNumber, isDateShow, episodeName, addInfo, fileSize);

        return episodeData;
    }

    private static string GetEpisodeName(string titleFile, bool delete = true)
    {
        string title;
        using (var sr = new StreamReader(titleFile, Encoding.UTF8))
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
        if (fi.Name.Contains(".480."))
        {
            resolution = "SD";
        }
        else if (fi.Name.Contains(".720.") || fi.Name.Contains(".1080."))
        {
            resolution = "HD";
        }
        else if (fi.Name.EndsWith(".mkv") || fi.Name.EndsWith(".mp4"))
        {
            Console.WriteLine($"{fi.Name} does not contain a resolution: {fi.Extension}");

            resolution = null;

            return false;
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
        else if (fi.Extension == ".nfo")
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
        if (fi.Name.EndsWith(".480.mkv") || fi.Name.EndsWith(".480.mp4") || fi.Name.EndsWith(".480.avi"))
        {
            addInfo = "SD"; //".480" + fi.Extension;
        }
        else if (fi.Name.EndsWith(".720.mkv") || fi.Name.EndsWith(".720.mp4"))
        {
            addInfo = "HD"; //".720" + fi.Extension;
        }
        else if (fi.Name.EndsWith(".1080.mkv") || fi.Name.EndsWith(".1080.mp4"))
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

        return addInfo;
    }

    private static void CheckSeries(string targetDir, Name name, string seasonNumber, string resolution, Dictionary<string, bool> mismatches, string fileName, ref bool abort)
    {
        var folderInQuestion = $"{targetDir}{name.LongName}";

        if (Directory.Exists(folderInQuestion))
        {
            CheckNfo(folderInQuestion, name);

            CheckSeason(folderInQuestion, seasonNumber, resolution, mismatches, fileName, ref abort, false);
        }
        else
        {
            var output = $"Folder missing fail: {folderInQuestion}";

            if (mismatches.ContainsKey(output) == false)
            {
                Console.WriteLine(output);

                Console.Write("Create? ");

                var input = Console.ReadLine().ToLower();

                var createEnabled = input == "y" || input == "yes";

                if (createEnabled)
                {
                    Directory.CreateDirectory(folderInQuestion);

                    CheckNfo(folderInQuestion, name);

                    CheckSeason(folderInQuestion, seasonNumber, resolution, mismatches, fileName, ref abort, createEnabled);
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

    private static void CheckNfo(string seriesFolder, Name name)
    {
        var fileName = Path.Combine(seriesFolder, "tvshow.nfo");

        if (File.Exists(fileName))
        {
            return;
        }

        string tvdbId = null;

        if (!string.IsNullOrEmpty(name.Link))
        {
            var tvdbUriBuilder = new UriBuilder(name.Link);

            var tvdbQuery = HttpUtility.ParseQueryString(tvdbUriBuilder.Query);

            tvdbId = tvdbQuery["id"];
        }

        var homeId = name.ShortName;

        UniqueId[] uniqueIds;
        if (string.IsNullOrEmpty(tvdbId))
        {
            uniqueIds = new[]
            {
                new UniqueId()
                {
                    type = "home",
                    @default = true,
                    Value = homeId,
                },
            };
        }
        else
        {
            uniqueIds = new[]
            {
                new UniqueId()
                {
                    type = "tvdb",
                    @default = true,
                    Value = tvdbId,
                },
                new UniqueId()
                {
                    type = "home",
                    @default = false,
                    Value = homeId,
                },
            };
        }

        var kodi = new KodiTVShow()
        {
            title = name.LocalizedNameSpecified ? name.LocalizedName : name.DisplayName,
            originaltitle = name.LocalizedNameSpecified ? name.DisplayName : null,
            premiered = 0,
            premieredSpecified = false,
            sorttitle = name.SortName,
            uniqueid = uniqueIds,
        };

        XmlSerializer<KodiTVShow>.Serialize(fileName, kodi);

        File.SetAttributes(fileName, FileAttributes.Archive);
    }

    private static void CheckSeason(string targetDir, string seasonNumber, string resolution, Dictionary<string, bool> mismatches, string fileName, ref bool abort, bool createEnabled)
    {
        var seasonFolderName = fileName.Contains("].de")
            ? "Staffel"
            : "Season";

        //System.Diagnostics.Debugger.Launch();

        var folderInQuestion = $@"{targetDir}\{seasonFolderName} {seasonNumber}";

        if (Directory.Exists(folderInQuestion))
        {
            CheckResolution(folderInQuestion, resolution, mismatches, ref abort, createEnabled);
        }
        else
        {
            var output = $"Folder missing fail: {folderInQuestion}";

            if (mismatches.ContainsKey(output) == false)
            {
                if (!createEnabled)
                {
                    Console.WriteLine(output);

                    Console.Write("Create? ");

                    var input = Console.ReadLine().ToLower();

                    createEnabled = input == "y" || input == "yes";
                }

                if (createEnabled)
                {
                    Directory.CreateDirectory(folderInQuestion);

                    CheckResolution(folderInQuestion, resolution, mismatches, ref abort, createEnabled);
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

    private static void CheckResolution(string targetDir, string resolution, Dictionary<string, bool> mismatches, ref bool abort, bool createEnabled)
    {
        var folderInQuestion = $@"{targetDir}\{resolution}";

        if (Directory.Exists(folderInQuestion) == false)
        {
            var output = $"Folder missing fail: {folderInQuestion}";

            if (mismatches.ContainsKey(output) == false)
            {
                if (!createEnabled)
                {
                    Console.WriteLine(output);

                    Console.Write("Create? ");

                    var input = Console.ReadLine().ToLower();

                    createEnabled = input == "y" || input == "yes";
                }

                if (createEnabled)
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
            var nameList = XmlSerializer<Names>.Deserialize(Path.Combine(targetDir, "Names.xml"));

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

            XmlSerializer<Names>.Serialize(Path.Combine(targetDir, "Names.xml"), nameList);
        }

        var success = Names.TryGetValue(shortName, out name);

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