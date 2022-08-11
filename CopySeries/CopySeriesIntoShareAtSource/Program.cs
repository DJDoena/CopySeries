namespace DoenaSoft.CopySeries
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using DoenaSoft.MediaInfoHelper;
    using NReco.VideoInfo;
    using ToolBox.Extensions;
    using FF = MediaInfoHelper.FFProbe;
    using Outlook = Microsoft.Office.Interop.Outlook;

    public static class Program
    {
        private static readonly string _targetDir;

        private static readonly string _namesDir;

        private static readonly string _toolsDir;

        private static readonly ulong _tenGibiByte;

        static Program()
        {
            _targetDir = CopySeriesIntoShareAtSourceSettings.Default.TargetDir;

            _namesDir = CopySeriesIntoShareAtSourceSettings.Default.StickDrive;

            _toolsDir = CopySeriesIntoShareAtSourceSettings.Default.ToolsDir;

            _tenGibiByte = 10L * ((ulong)(Math.Pow(2, 30)));
        }

        public static void Main(string[] args)
        {
            if (Process.GetProcessesByName("CopySeriesIntoShareAtSource").Length > 1)
            {
                return;
            }

            if (Process.GetProcessesByName("vlc").Length > 0)
            {
                if (MessageBox.Show("VLC is running. Continue?", string.Empty, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                {
                    return;
                }
            }

            try
            {
                var di = new DirectoryInfo(CopySeriesIntoShareAtSourceSettings.Default.SourceDir);

                var searchOption = WithoutSubFolder(args) ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories;

                if (Helper.CopySeriesIntoShare(di, searchOption, _targetDir, false, null, _namesDir, out var episodeList, out var recentFiles) == false)
                {
                    return;
                }

                EnrichAudioInfo(recentFiles.Files, episodeList);

                WriteEmail(episodeList);
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

        private static void EnrichAudioInfo(string[] files, List<EpisodeData> episodes)
        {
            for (var fileIndex = 0; fileIndex < files.Length; fileIndex++)
            {
                var episodeInfo = episodes[fileIndex];

                if (episodeInfo != null)
                {
                    EnrichAudioInfo(files[fileIndex], episodeInfo);
                }
            }
        }

        private static void EnrichAudioInfo(string file, EpisodeData episode)
        {
            var fi = new FileInfo(Path.Combine(_targetDir, file));

            var mediaInfo = GetMediaInfo(fi);

            if (mediaInfo != null)
            {
                var xmlInfo = MediaInfo2XmlConverter.Convert(mediaInfo, episode.OriginalLanguage);

                xmlInfo.Episode = new Episode()
                {
                    SeriesName = episode.DisplayName,
                    EpisodeNumber = episode.EpisodeID,
                    EpisodeName = episode.EpisodeName
                };

                XmlWriter.Write(fi, xmlInfo);
            }

            var audioStreams = mediaInfo?.streams?.Where(stream => stream.codec_type == "audio") ?? Enumerable.Empty<FF.Stream>();

            var audioLanguages = audioStreams.Select(stream => stream.tag.GetLanguage() ?? episode.OriginalLanguage).Distinct();

            audioLanguages.ForEach(language => episode.AddAudio(language));

            var subtitleStreams = mediaInfo?.streams?.Where(stream => stream.codec_type == "subtitle") ?? Enumerable.Empty<FF.Stream>();

            var subtitleLanguages = subtitleStreams.Select(stream => stream.tag.GetLanguage() ?? episode.OriginalLanguage).Distinct();

            subtitleLanguages.ForEach(language => episode.AddSubtitle(language));
        }

        private static FF.FFProbe GetMediaInfo(FileInfo fi)
        {
            try
            {
                var mediaInfo = (new FFProbe()).GetMediaInfo(fi.FullName);

                var xml = mediaInfo.Result.CreateNavigator().OuterXml;

                var ffprobe = Serializer<FF.FFProbe>.FromString(xml);

                return ffprobe;
            }
            catch
            {
                return null;
            }
        }

        private static void WriteEmail(List<EpisodeData> episodes)
        {
            episodes = episodes.Where(e => e != null).ToList();

            episodes.Sort();

            CalculatePadding(episodes, out int padSeriesName, out var padEpisodeID, out var padEpisodeName, out var padAddInfo);

            var mailTextBuilder = new StringBuilder();

            var shows = episodes.Select(e => e.DisplayName).Distinct().ToList();

            mailTextBuilder.AppendLine("Serien in diesem Update:");

            foreach (var show in shows)
            {
                mailTextBuilder.AppendLine(show);
            }

            mailTextBuilder.AppendLine();

            AddNewSeasonInfo(episodes, out var subject, out var addInfo, out var newSeries, out var newSeason);

            ulong fileSize = 0;

            foreach (var episode in episodes)
            {
                AppendEpisode(episode, padSeriesName, padEpisodeID, padEpisodeName, padAddInfo, mailTextBuilder);

                fileSize += episode.FileSize.InBytes;
            }

            AddSummarySize(padSeriesName, padEpisodeID, padEpisodeName, padAddInfo, fileSize, mailTextBuilder);

            var recipients = GetRecipients(newSeries, newSeason);

            var success = false;

            do
            {
                try
                {
                    CreateOutlookMail(recipients, subject, addInfo, mailTextBuilder.ToString());

                    success = true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine(ex.Message);

                    Thread.Sleep(30000);
                }
            } while (success == false);
        }

        private static void CalculatePadding(List<EpisodeData> episodes, out int padSeriesName, out int padEpisodeID, out int padEpisodeName, out int padAddInfo)
        {
            padSeriesName = 1;
            padEpisodeID = 1;
            padEpisodeName = 1;
            padAddInfo = 1;
            //padLanguages = 1;

            foreach (var episode in episodes)
            {
                if (episode.DisplayName.Length > padSeriesName)
                {
                    padSeriesName = episode.DisplayName.Length;
                }

                if (episode.EpisodeID.Length > padEpisodeID)
                {
                    padEpisodeID = episode.EpisodeID.Length;
                }

                if (episode.EpisodeName.Length > padEpisodeName)
                {
                    padEpisodeName = episode.EpisodeName.Length;
                }

                if (episode.AddInfo.Length > padAddInfo)
                {
                    padAddInfo = episode.AddInfo.Length;
                }

                //var languages = episode.GetAudio();

                //if (languages.Length > padLanguages)
                //{
                //    padLanguages = languages.Length;
                //}
            }
        }

        private static void AppendEpisode(EpisodeData episode, int padSeriesName, int padEpisodeID, int padEpisodeName, int padAddInfo, StringBuilder mailTextBuilder)
        {
            mailTextBuilder.Append(episode.DisplayName.PadRight(padSeriesName + 1));
            mailTextBuilder.Append(episode.EpisodeID.PadLeft(padEpisodeID));
            mailTextBuilder.Append(" \"");

            var episodeName = episode.EpisodeName + "\"";

            episodeName = episodeName.PadRight(padEpisodeName + 1);

            mailTextBuilder.Append(episodeName);
            mailTextBuilder.Append(" ");
            mailTextBuilder.Append(episode.AddInfo.PadRight(padAddInfo));
            mailTextBuilder.Append(" (");
            mailTextBuilder.Append(episode.FileSize.ToString(3));
            mailTextBuilder.AppendLine(")\t");

            var audio = episode.GetAudio();

            var subtitles = episode.GetSubtitles();


            if (!string.IsNullOrEmpty(audio))
            {
                mailTextBuilder.AppendLine($"\t\t\tAudio: {audio}");
            }

            if (!string.IsNullOrEmpty(subtitles))
            {
                mailTextBuilder.AppendLine($"\t\t\tSubtitles: {subtitles}");
            }
        }

        private static void AddSummarySize(int padSeriesName, int padSeasonID, int padEpisodeName, int padAddInfo, ulong fileSize, StringBuilder mailTextBuilder)
        {
            var padding = padSeriesName + padSeasonID + padEpisodeName + padAddInfo + 5;

            mailTextBuilder.AppendLine("".PadLeft(padding + 12, '-'));

            if (fileSize >= _tenGibiByte)
            {
                mailTextBuilder.Append("".PadLeft(padding - 1, ' '));
            }
            else
            {
                mailTextBuilder.Append("".PadLeft(padding, ' '));
            }

            mailTextBuilder.Append(" (");
            mailTextBuilder.Append((new FileSize(fileSize)).ToString());
            mailTextBuilder.AppendLine(")");
        }

        private static string CreateOutlookMail(string recipients, string subject, string addInfo, string mailText)
        {
            var outlook = new Outlook.Application();

            var mail = (Outlook.MailItem)(outlook.CreateItem(Outlook.OlItemType.olMailItem));

            mail.Subject = subject;

            mail.BodyFormat = Outlook.OlBodyFormat.olFormatHTML;

            var bodyTextBuilder = new StringBuilder("<pre>");

            if (string.IsNullOrEmpty(addInfo) == false)
            {
                bodyTextBuilder.Append(System.Web.HttpUtility.HtmlEncode(addInfo));
            }

            bodyTextBuilder.Append(System.Web.HttpUtility.HtmlEncode(mailText) + "</pre>");

            mail.HTMLBody = bodyTextBuilder.ToString();

            mail.BCC = recipients;

            mail.Display(false);

            Marshal.ReleaseComObject(mail);
            mail = null;

            Marshal.ReleaseComObject(outlook);
            outlook = null;

            return recipients;
        }

        private static string GetRecipients(bool newSeries, bool newSeason)
        {
            var recipients = MediaInfoHelper.Serializer<Recipients>.Deserialize(Path.Combine(_namesDir, "Recipients.xml"));

            if (recipients?.RecipientList?.Length > 0)
            {
                var bcc = GetBcc(recipients.RecipientList, newSeries, newSeason);

                return string.Join(";", bcc.ToArray());
            }

            return string.Empty;
        }

        private static IEnumerable<string> GetBcc(IEnumerable<Recipient> recipients, bool newSeries, bool newSeason)
        {
            var today = DateTime.Now.DayOfWeek;

            foreach (var recipient in recipients)
            {
                if ((recipient.NewSeriesSpecified) && (recipient.NewSeries) && (newSeries == false))
                {
                    continue;
                }

                if ((recipient.NewSeasonSpecified) && (recipient.NewSeason) && (newSeason == false))
                {
                    continue;
                }

                if ((recipient.DayOfWeekSpecified) && (today != recipient.DayOfWeek))
                {
                    continue;
                }

                yield return recipient.Value;
            }
        }

        private static bool WithoutSubFolder(string[] args)
            => (args?.Length > 0) && (args[0].ToLower() == "/withoutsubfolders");

        private static void AddNewSeasonInfo(List<EpisodeData> episodes, out string subject, out string addInfo, out bool newSeries, out bool newSeason)
        {
            newSeries = false;

            var newSeasons = episodes.Where(episode => episode.IsFirstOfSeason);

            var addInfoBuilder = new StringBuilder();

            newSeason = newSeasons.Any();

            if (newSeason)
            {
                var names = new HashSet<string>();

                newSeasons.Split(episode => episode.IsPilot, out var pilots, out var nonPilots);

                foreach (var pilot in pilots)
                {
                    newSeries = true;

                    AddInfo(addInfoBuilder, names, pilot, "Serie");
                }

                foreach (var nonPilot in nonPilots)
                {
                    AddInfo(addInfoBuilder, names, nonPilot, "Season");
                }
            }

            addInfo = addInfoBuilder.ToString();

            subject = "Share Update";

            if (newSeries)
            {
                subject += " (neue Serie)";
            }
            else if (newSeason)
            {
                subject += " (neue Season)";
            }
        }

        private static void AddInfo(StringBuilder addInfoBuilder, HashSet<string> names, EpisodeData episode, string text)
        {
            if (names.Contains(episode.SeriesName) == false)
            {
                addInfoBuilder.AppendLine($"Neue {text}: {episode.DisplayName}");

                if (!string.IsNullOrWhiteSpace(episode.Link))
                {
                    addInfoBuilder.AppendLine(episode.Link);
                }

                addInfoBuilder.AppendLine();

                names.Add(episode.SeriesName);
            }
        }
    }
}