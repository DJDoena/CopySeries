namespace DoenaSoft.CopySeries
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;
    using NReco.VideoInfo;
    using ToolBox.Extensions;
    using ToolBox.Generics;

    public static class Program
    {
        private const String CopySuggestionsFolder = "_CopySuggestions";

        private const String NewLine = "%0D%0A";

        private static readonly String TargetDir;

        private static readonly String StickDir;

        private static readonly String ToolsDir;

        static Program()
        {
            TargetDir = CopySeriesIntoShareAtSourceSettings.Default.TargetDir;

            StickDir = CopySeriesIntoShareAtSourceSettings.Default.StickDrive;

            ToolsDir = CopySeriesIntoShareAtSourceSettings.Default.ToolsDir;
        }

        public static void Main(String[] args)
        {
            if (Process.GetProcessesByName("CopySeriesIntoShareAtSource").Length > 1)
            {
                return;
            }

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


                if (Helper.CopySeriesIntoShare(di, searchOption, TargetDir, false, null, StickDir, out List<EpisodeData> episodeList, out RecentFiles recentFiles) == false)
                {
                    return;
                }

                CreateCopySuggestions(recentFiles);

                EnrichAudioInfo(recentFiles.Files, episodeList);

                WriteEmail(episodeList);

                Helper.CleanFolder("RecentFiles", "txt", 1, TargetDir, CopySuggestionsFolder);

                DirAtSource();
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

        private static void CreateCopySuggestions(RecentFiles recentFiles)
        {
            EnsureCopySuggestionsFolder();

            String copySuggestionsFile = Helper.GetNewFileName("RecentFiles", "txt", TargetDir, CopySuggestionsFolder);

            using (StreamWriter sw = new StreamWriter(copySuggestionsFile, false, Encoding.GetEncoding(1252)))
            {
                foreach (String fileName in recentFiles.Files)
                {
                    String file = Path.Combine(TargetDir, fileName);

                    sw.WriteLine(file);
                }
            }
        }

        private static void EnsureCopySuggestionsFolder()
        {
            DirectoryInfo di = new DirectoryInfo(Path.Combine(TargetDir, CopySuggestionsFolder));

            if (di.Exists == false)
            {
                di.Create();
            }
        }

        private static void EnrichAudioInfo(String[] files
            , List<EpisodeData> episodes)
        {
            for (Int32 i = 0; i < files.Length; i++)
            {
                EnrichAudioInfo(files[i], episodes[i]);
            }
        }

        private static void EnrichAudioInfo(String file
            , EpisodeData episode)
        {
            FileInfo fi = new FileInfo(Path.Combine(TargetDir, file));

            Xml.FFProbe mediaInfo = GetMediaInfo(fi);

            if (mediaInfo != null)
            {
                Xml.VideoInfo xmlInfo = MediaInfo2XmlConverter.Convert(mediaInfo, fi.Name);

                xmlInfo.Episode = new Xml.Episode()
                {
                    SeriesName = episode.DisplayName,
                    EpisodeNumber = episode.EpisodeID,
                    EpisodeName = episode.EpisodeName
                };

                XmlWriter.Write(fi, xmlInfo);
            }

            IEnumerable<Xml.Stream> streams = mediaInfo?.streams?.Where(stream => stream.codec_type == "audio") ?? Enumerable.Empty<Xml.Stream>();

            streams = streams.Where(LanguageIsNotUndefined);

            IEnumerable<String> languages = streams.Select(stream => stream.tag.GetLanguage()).Distinct();

            languages.ForEach(language => episode.AddLanguage(language));
        }

        private static Xml.FFProbe GetMediaInfo(FileInfo fi)
        {
            try
            {
                MediaInfo mediaInfo = (new FFProbe()).GetMediaInfo(fi.FullName);

                String xml = mediaInfo.Result.CreateNavigator().OuterXml;

                Xml.FFProbe ffprobe = Serializer<Xml.FFProbe>.FromString(xml);

                return (ffprobe);
            }
            catch
            {
                return (null);
            }
        }

        private static Boolean LanguageIsNotUndefined(Xml.Stream stream)
            => stream?.tag.GetLanguage() != "und";

        private static void WriteEmail(List<EpisodeData> episodes)
        {
            episodes.Sort();

            StringBuilder email = new StringBuilder();

            email.Append("mailto:?bcc=");

            Boolean newSeries = AddNewSeasonInfo(episodes, out String subject, out String addInfo);

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

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = email.ToString()
                }
            };

            process.Start();
        }

        private static String GetBcc(Boolean newSeries)
        {
            Recipients recipients = Serializer<Recipients>.Deserialize(Path.Combine(StickDir, "Recipients.xml"));

            if (recipients?.RecipientList?.Length > 0)
            {
                IEnumerable<String> bcc = GetBcc(recipients.RecipientList, newSeries);

                return (String.Join(";", bcc.ToArray()));
            }

            return (String.Empty);
        }

        private static IEnumerable<String> GetBcc(IEnumerable<Recipient> recipients
            , Boolean newSeries)
        {
            DayOfWeek today = DateTime.Now.DayOfWeek;

            foreach (Recipient recipient in recipients)
            {
                if ((recipient.DayOfWeekSpecified) && (today != recipient.DayOfWeek))
                {
                    continue;
                }

                if ((recipient.NewSeriesSpecified) && (recipient.NewSeries) && (newSeries == false))
                {
                    continue;
                }

                yield return (recipient.Value);
            }
        }

        private static Boolean WithoutSubFolder(String[] args)
            => ((args?.Length > 0) && (args[0].ToLower() == "/withoutsubfolders"));

        private static void DirAtSource()
        {
            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "DirAtSource.exe",
                    WorkingDirectory = ToolsDir
                }
            };

            process.Start();
            process.WaitForExit();
        }

        private static Boolean AddNewSeasonInfo(List<EpisodeData> episodes
         , out String subject
         , out String addInfo)
        {
            Boolean newSeries = false;

            IEnumerable<EpisodeData> newSeason = episodes.Where(episode => episode.IsFirstOfSeason);

            addInfo = String.Empty;

            subject = Uri.EscapeDataString("Share Update");

            if (newSeason.Any())
            {
                subject = Uri.EscapeDataString("Share Update (with Info)");

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