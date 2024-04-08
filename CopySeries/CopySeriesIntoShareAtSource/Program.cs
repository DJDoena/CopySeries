using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using DoenaSoft.MediaInfoHelper.DataObjects.FFProbeMetaXml;
using DoenaSoft.MediaInfoHelper.DataObjects.VideoMetaXml;
using DoenaSoft.MediaInfoHelper.Helpers;
using DoenaSoft.MediaInfoHelper.Reader;
using DoenaSoft.MediaInfoHelper.Readers;
using DoenaSoft.ToolBox.Extensions;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace DoenaSoft.CopySeries
{
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
            Console.WriteLine(typeof(Program).Assembly.GetName().Version);

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

            var mediaInfo = FFProbeReader.TryGetFFProbe(fi, out var additionalSubtitleMediaInfos);

            VideoMeta xmlInfo = null;
            if (mediaInfo != null)
            {
                xmlInfo = GetXmlInfo(episode, mediaInfo, additionalSubtitleMediaInfos);

                xmlInfo.Episode = new Episode()
                {
                    SeriesName = episode.DisplayName,
                    EpisodeNumber = episode.EpisodeID,
                    EpisodeName = episode.EpisodeName
                };

                XmlWriter.Write(fi, xmlInfo);
            }

            var audioLanguages = (xmlInfo?.Audio ?? Enumerable.Empty<Audio>()).Select(a => a.Language).Distinct();

            audioLanguages.ForEach(language => episode.AddAudio(language));

            var subtitleLanguages = (xmlInfo?.Subtitle ?? Enumerable.Empty<Subtitle>()).Select(s => s.Language).Distinct();

            subtitleLanguages.ForEach(language => episode.AddSubtitle(language));
        }

        private static VideoMeta GetXmlInfo(EpisodeData episode, FFProbeMeta mediaInfo, List<FFProbeMeta> additionalSubtitleMediaInfos)
        {
            var xmlInfo = FFProbeMetaConverter.Convert(mediaInfo, episode.OriginalLanguage);

            if (additionalSubtitleMediaInfos?.Count > 0)
            {
                var subtitles = xmlInfo.Subtitle == null
                      ? new List<Subtitle>()
                      : new List<Subtitle>(xmlInfo.Subtitle);

                var additionalSubtitleXmlInfos = additionalSubtitleMediaInfos.Select(mi => FFProbeMetaConverter.Convert(mi, episode.OriginalLanguage)).ToList();

                foreach (var subtitleXmlInfos in additionalSubtitleXmlInfos)
                {
                    if (subtitleXmlInfos?.Subtitle?.Length > 0)
                    {
                        subtitles.AddRange(subtitleXmlInfos.Subtitle);
                    }
                }

                xmlInfo.Subtitle = subtitles.ToArray();
            }

            if (xmlInfo.Audio?.Any() == true)
            {
                xmlInfo.Audio = xmlInfo.Audio
                    .Where(a => !string.IsNullOrWhiteSpace(a?.Language))
                    .Select(FixLanguage)
                    .ToArray();
            }

            if (xmlInfo.Subtitle?.Any() == true)
            {
                xmlInfo.Subtitle = xmlInfo.Subtitle
                    .Where(s => !string.IsNullOrWhiteSpace(s?.Language))
                    .Select(FixLanguage)
                    .ToArray();
            }

            return xmlInfo;
        }

        private static Audio FixLanguage(Audio audio)
        {
            audio.Language = LanguageExtensions.StandardizeLanguage(audio.Language);

            return audio;
        }

        private static Subtitle FixLanguage(Subtitle subtitle)
        {
            subtitle.Language = LanguageExtensions.StandardizeLanguage(subtitle.Language);

            return subtitle;
        }

        private static void WriteEmail(List<EpisodeData> episodes)
        {
            episodes = episodes.Where(e => e != null).ToList();

            episodes.Sort();

            CalculatePadding(episodes, out var padSeriesName, out var padEpisodeID, out var padEpisodeName, out var padAddInfo);

            var mailTextBuilder = new StringBuilder();

            var shows = episodes.Select(e => e.DisplayName).Distinct().ToList();

            mailTextBuilder.AppendLine("Serien in diesem Update:");

            foreach (var show in shows)
            {
                mailTextBuilder.AppendLine(show);
            }

            mailTextBuilder.AppendLine();
            mailTextBuilder.AppendLine();

            AddNewSeasonInfo(episodes, out var subject, out var addInfo, out var newSeries, out var newSeason);

            if (!string.IsNullOrEmpty(addInfo))
            {
                mailTextBuilder.AppendLine(addInfo);
            }

            ulong fileSize = 0;

            foreach (var episode in episodes)
            {
                AppendEpisode(episode, padSeriesName, padEpisodeID, padEpisodeName, padAddInfo, mailTextBuilder);

                fileSize += episode.FileSize.Bytes;
            }

            AddSummarySize(padSeriesName, padEpisodeID, padEpisodeName, padAddInfo, fileSize, mailTextBuilder);

            var recipients = GetRecipients(newSeries, newSeason);

            var success = false;

            do
            {
                try
                {
                    CreateOutlookMail(recipients, subject, mailTextBuilder.ToString());

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
                mailTextBuilder.Append(string.Empty.PadRight(padSeriesName + 1));
                mailTextBuilder.Append(string.Empty.PadLeft(padEpisodeID));
                mailTextBuilder.AppendLine($" Audio:     {audio}");
            }

            if (!string.IsNullOrEmpty(subtitles))
            {
                mailTextBuilder.Append(string.Empty.PadRight(padSeriesName + 1));
                mailTextBuilder.Append(string.Empty.PadLeft(padEpisodeID));
                mailTextBuilder.AppendLine($" Subtitles: {subtitles}");
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

        private static string CreateOutlookMail(string recipients, string subject, string mailText)
        {
            var outlook = new Outlook.Application();

            var mail = (Outlook.MailItem)(outlook.CreateItem(Outlook.OlItemType.olMailItem));

            mail.Subject = subject;

            mail.BodyFormat = Outlook.OlBodyFormat.olFormatHTML;

            var bodyTextBuilder = new StringBuilder("<pre>");

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
            var recipients = XmlSerializer<Recipients>.Deserialize(Path.Combine(_namesDir, "Recipients.xml"));

            if (recipients?.RecipientList?.Length > 0)
            {
                var bcc = recipients.RecipientList.GetBcc(newSeries, newSeason);

                return string.Join(";", bcc.ToArray());
            }

            return string.Empty;
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