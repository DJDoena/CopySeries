namespace DoenaSoft.CopySeries
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web.Script.Serialization;
    using System.Windows.Forms;
    using ToolBox.Extensions;
    using ToolBox.Generics;

    public class Program
    {
        private static String JsonFile = Path.Combine(Path.GetTempPath(), "info.json");

        private const String NewLine = "%0D%0A";

        public static void Main(String[] args)
        {
            if (Process.GetProcessesByName("vlc").Length > 0)
            {
                if (MessageBox.Show("VLC is running. Continue?", String.Empty, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
            }

            try
            {
                DirectoryInfo di = new DirectoryInfo(CopySeriesIntoShareAtSourceSettings.Default.SourceDir);

                SearchOption searchOption = WithoutSubFolder(args) ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

                String targetDir = CopySeriesIntoShareAtSourceSettings.Default.TargetDir;

                String stickDrive = CopySeriesIntoShareAtSourceSettings.Default.StickDrive;

                List<EpisodeData> episodeList;
                RecentFiles recentFiles;
                if (Helper.CopySeriesIntoShare(di, searchOption, targetDir, false, null, stickDrive, out episodeList, out recentFiles) == false)
                {
                    return;
                }

                String copySuggestionsFile = Helper.GetNewFileName("RecentFiles", "txt", targetDir, "_CopySuggestions");

                using (StreamWriter sw = new StreamWriter(copySuggestionsFile, false, Encoding.GetEncoding(1252)))
                {
                    foreach (String fileName in recentFiles.Files)
                    {
                        String file = Path.Combine(targetDir, fileName);

                        sw.WriteLine(file);
                    }
                }

                EnrichAudioInfo(targetDir, recentFiles.Files, episodeList);

                WriteEmail(episodeList);

                Helper.CleanFolder("RecentFiles", "txt", 1, targetDir, "_CopySuggestions");

                DirAtSource(targetDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("Press <Enter> to exit.");
                Console.ReadLine();
            }
        }

        private static void EnrichAudioInfo(String targetDir
            , String[] files
            , List<EpisodeData> episodes)
        {
            for (Int32 i = 0; i < files.Length; i++)
            {
                EnrichAudioInfo(targetDir, files[i], episodes[i]);
            }
        }

        private static void EnrichAudioInfo(String targetDir
            , String file
            , EpisodeData episode)
        {
            Json.RootObject info = GetJson(targetDir, file);

            IEnumerable<Json.Track> tracks = info?.tracks?.Where(t => t.type == "audio") ?? Enumerable.Empty<Json.Track>();

            tracks = tracks.Where(LanguageIsNotUndefined);

            IEnumerable<String> languages = tracks.Select(track => track.properties.language).Distinct();

            foreach (String language in languages)
            {
                episode.AddLanguage(language);
            }
        }

        private static Boolean LanguageIsNotUndefined(Json.Track track)
            => ((track?.properties?.language != null) && (track.properties.language != "und"));

        private static Json.RootObject GetJson(String targetDir
            , String file)
        {
            CreateJson(targetDir, file);

            Json.RootObject info = ReadJson();

            File.Delete(JsonFile);

            return (info);
        }

        private static Json.RootObject ReadJson()
        {
            using (StreamReader sr = new StreamReader(JsonFile))
            {
                String json = sr.ReadToEnd();

                JavaScriptSerializer jss = new JavaScriptSerializer();

                Json.RootObject info = jss.Deserialize<Json.RootObject>(json);

                return (info);
            }
        }

        private static void CreateJson(String targetDir
            , String file)
        {
            String arguments = "-r " + JsonFile + " -J \"" + Path.Combine(targetDir, file) + "\"";

            Process p = new Process();

            p.StartInfo = new ProcessStartInfo();

            p.StartInfo.FileName = "mkvmerge.exe";
            p.StartInfo.Arguments = arguments;
            p.StartInfo.WorkingDirectory = targetDir;

            p.Start();

            p.WaitForExit();
        }

        private static void WriteEmail(List<EpisodeData> episodes)
        {
            episodes.Sort();

            StringBuilder email = new StringBuilder();

            email.Append("mailto:?bcc=");

            String subject;
            String addInfo;
            Boolean newSeries = AddNewSeasonInfo(episodes, out subject, out addInfo);

            String bcc = GetBcc(newSeries);

            email.Append(bcc);
            email.Append("&subject=");
            email.Append(subject);
            email.Append("&body=");

            Console.WriteLine();

            if (String.IsNullOrEmpty(addInfo) == false)
            {
                email.Append(Uri.EscapeDataString(addInfo));
                email.Append(NewLine);

                Console.Write(addInfo);
                Console.WriteLine();
            }

            String previous = null;

            foreach (EpisodeData episode in episodes)
            {
                String name = episode.ToString();

                if ((previous != null) && (previous != episode.SeriesName))
                {
                    email.Append(NewLine);

                    Console.WriteLine();
                }

                email.Append(Uri.EscapeDataString(name));
                email.Append(NewLine);

                Console.Write(name);
                Console.WriteLine();

                previous = episode.SeriesName;
            }

            String emailText = email.ToString();

            Process process = new Process();

            process.StartInfo = new ProcessStartInfo();
            process.StartInfo.FileName = emailText;

            process.Start();
        }

        private static String GetBcc(Boolean newSeries)
        {
            Recipients recipients = Serializer<Recipients>.Deserialize(Path.Combine(CopySeriesIntoShareAtSourceSettings.Default.TargetDir, "Recipients.xml"));

            StringBuilder bcc = new StringBuilder();

            if (recipients?.RecipientList?.Length > 0)
            {
                DayOfWeek today = DateTime.Now.DayOfWeek;

                foreach (Recipient recipient in recipients.RecipientList)
                {
                    if ((recipient.DayOfWeekSpecified) && (today != recipient.DayOfWeek))
                    {
                        continue;
                    }

                    if ((recipient.NewSeriesSpecified) && (recipient.NewSeries) && (newSeries == false))
                    {
                        continue;
                    }

                    bcc.Append(";");
                    bcc.Append(recipient.Value);
                }
            }

            return (bcc.ToString());
        }

        private static Boolean WithoutSubFolder(String[] args)
            => ((args?.Length > 0) && (args[0].ToLower() == "/withoutsubfolders"));

        private static void DirAtSource(String targetDir)
        {
            Process p = new Process();

            p.StartInfo = new ProcessStartInfo();
            p.StartInfo.FileName = "DirAtSource.exe";
            p.StartInfo.WorkingDirectory = targetDir;

            p.Start();

            p.WaitForExit();
        }

        private static Boolean AddNewSeasonInfo(List<EpisodeData> episodes
         , out String subject
         , out String addInfo)
        {
            Boolean newSeries = false;

            IEnumerable<EpisodeData> newSeason = episodes.Where(episode => episode.IsFirstOfSeason);

            addInfo = String.Empty;

            subject = "Share Update";

            if (newSeason.Any())
            {
                subject = "Share Update (with Info)";

                HashSet<String> names = new HashSet<String>();

                IEnumerable<EpisodeData> pilots;
                IEnumerable<EpisodeData> nonPilots;
                newSeason.Split(episode => episode.IsPilot, out pilots, out nonPilots);

                foreach (EpisodeData pilot in pilots)
                {
                    newSeries = true;

                    AddInfo(ref addInfo, names, pilot, "Series");
                }

                foreach (EpisodeData nonPilot in nonPilots)
                {
                    AddInfo(ref addInfo, names, nonPilot, "Season");
                }
            }

            return (newSeries);
        }

        private static void AddInfo(ref String addInfo
            , HashSet<String> names
            , EpisodeData episode
            , String text)
        {
            if (names.Contains(episode.SeriesName) == false)
            {
                addInfo += $"New  {text}: {episode.DisplayName}{Environment.NewLine}{Environment.NewLine}";

                names.Add(episode.SeriesName);
            }
        }
    }
}