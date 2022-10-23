namespace DoenaSoft.CopySeries
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using ToolBox.Extensions;
    using ToolBox.Generics;
    using Outlook = Microsoft.Office.Interop.Outlook;

    public static class Program
    {
        private static string StickDrive { get; }

        private static string SourceDir { get; }

        private static string TargetDir { get; }

        private static string RemoteDir { get; }

        private static string CompleteSeriesFile { get; }

        private static ulong TenGibiByte { get; }

        private static string DirFile { get; }

        static Program()
        {
            StickDrive = CopySeriesIntoShareAtTargetSettings.Default.StickDrive;

            SourceDir = Path.Combine(StickDrive, "Series");

            TargetDir = CopySeriesIntoShareAtTargetSettings.Default.TargetPath;

            RemoteDir = CopySeriesIntoShareAtTargetSettings.Default.SourcePath;

            CompleteSeriesFile = "CompleteSeriesList.txt";

            TenGibiByte = 10L * ((UInt64)(Math.Pow(2, 30)));

            DirFile = Path.Combine(StickDrive, "dir.txt");
        }

        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                var di = ((args == null) || (args.Length == 0))
                    ? (new DirectoryInfo(SourceDir))
                    : (new DirectoryInfo(args[0]));

                while (File.Exists(DirFile) == false)
                {
                    Console.WriteLine(DirFile + " not available.");
                    Console.WriteLine();
                    Console.WriteLine("Press <Enter> to retry.");
                    Console.ReadLine();
                }
                if (Helper.CopySeriesIntoShare(di, SearchOption.AllDirectories, TargetDir, true, RemoteDir, StickDrive, out List<EpisodeData> episodes, out RecentFiles recentFiles))
                {
                    const string SubDir = "_RecentFiles";

                    Serializer<RecentFiles>.Serialize(Path.Combine(TargetDir, SubDir, "RecentFiles.xml"), recentFiles);

                    var newFileName = Helper.GetNewFileName("RecentFiles", "xml", TargetDir, SubDir);

                    Serializer<RecentFiles>.Serialize(newFileName, recentFiles);

                    Helper.CleanFolder("RecentFiles", "xml", 8, TargetDir, SubDir);

                    Dir.CreateFileList(StickDrive, TargetDir);

                    File.Copy(Path.Combine(StickDrive, CompleteSeriesFile), Path.Combine(TargetDir, CompleteSeriesFile), true);

                    WriteEmail(episodes, (new FileInfo(newFileName)).Name);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Console.WriteLine();
                Console.WriteLine("Press <Enter> to exit.");
                Console.ReadLine();
            }
        }

        private static void WriteEmail(List<EpisodeData> episodes, string fileName)
        {
            episodes.Sort();

            CalculatePadding(episodes, out int padSeriesName, out int padEpisodeID, out int padEpisodeName, out int padAddInfo);

            var email = new StringBuilder();

            email.AppendLine();

            ulong fileSize = 0;

            foreach (var episode in episodes)
            {
                AppendEpisode(episode, padSeriesName, padEpisodeID, padEpisodeName, padAddInfo, email);

                fileSize += episode.FileSize.InBytes;
            }

            AddSummarySize(padSeriesName, padEpisodeID, padEpisodeName, padAddInfo, fileSize, email);

            var emailText = email.ToString();

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine(emailText);
            Console.WriteLine(GetPlainTextAppendix(fileName));

            var success = false;
            do
            {
                try
                {
                    var bcc = TryCreateOutlookMail(episodes, fileName, emailText);

                    Console.WriteLine();
                    Console.WriteLine(bcc);

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

        private static string TryCreateOutlookMail(List<EpisodeData> episodes, string fileName, string emailText)
        {
            var outlook = new Outlook.Application();

            var mail = (Outlook.MailItem)(outlook.CreateItem(Outlook.OlItemType.olMailItem));

            var newSeries = AddNewSeasonInfo(episodes, mail, out string addInfo);

            mail.BodyFormat = Outlook.OlBodyFormat.olFormatHTML;

            var bodyText = "<pre>";

            if (string.IsNullOrEmpty(addInfo) == false)
            {
                bodyText += addInfo;
            }

            bodyText += System.Web.HttpUtility.HtmlEncode(emailText) + "</pre>" + GetHtmlAppendix(fileName);

            mail.HTMLBody = bodyText;

            var bcc = GetBcc(newSeries);

            mail.BCC = bcc;

            mail.Display(false);

            Marshal.ReleaseComObject(mail);
            mail = null;

            Marshal.ReleaseComObject(outlook);
            outlook = null;

            return bcc;
        }

        private static bool AddNewSeasonInfo(List<EpisodeData> episodes, Outlook.MailItem mail, out string addInfo)
        {
            var newSeries = false;

            var newSeason = episodes.Where(episode => episode.IsFirstOfSeason);

            addInfo = string.Empty;

            if (newSeason.Any())
            {
                var names = new HashSet<string>();

                newSeason.Split(episode => episode.IsPilot, out IEnumerable<EpisodeData> pilots, out IEnumerable<EpisodeData> nonPilots);

                foreach (var pilot in pilots)
                {
                    newSeries = true;

                    AddInfo(ref addInfo, names, pilot, "Series");
                }

                foreach (var nonPilot in nonPilots)
                {
                    AddInfo(ref addInfo, names, nonPilot, "Season");
                }
            }

            mail.Subject = "Share Update";

            if (newSeries)
            {
                mail.Subject += " (neue Serie)";
            }
            else if (newSeason.Any())
            {
                mail.Subject += " (neue Season)";
            }

            return newSeries;
        }

        private static void AddSummarySize(int padSeriesName, int padSeasonID, int padEpisodeName, int padAddInfo, ulong fileSize, StringBuilder email)
        {
            var padding = padSeriesName + padSeasonID + padEpisodeName + padAddInfo + 5;

            email.AppendLine("".PadLeft(padding + 12, '-'));

            if (fileSize >= TenGibiByte)
            {
                email.Append("".PadLeft(padding - 1, ' '));
            }
            else
            {
                email.Append("".PadLeft(padding, ' '));
            }

            email.Append(" (");
            email.Append((new FileSize(fileSize)).ToString());
            email.AppendLine(")");
        }

        private static string GetBcc(bool newSeries)
        {
            var recipients = Serializer<Recipients>.Deserialize(Path.Combine(TargetDir, "Recipients.xml"));

            var bcc = new StringBuilder();

            if (recipients?.RecipientList?.Length > 0)
            {
                var today = DateTime.Now.DayOfWeek;

                foreach (var recipient in recipients.RecipientList)
                {
                    if (string.IsNullOrEmpty(recipient.Flags))
                    {
                        continue;
                    }
                    else
                    {
                        var flags = recipient.Flags.Split(',');

                        if (!flags.Any(f => f == "TVShows"))
                        {
                            continue;
                        }
                        else if (!newSeries && flags.Any(f => f == "NewSeries"))
                        {
                            continue;
                        }
                    }

                    bcc.Append(";");
                    bcc.Append(recipient.Value);
                }
            }

            return bcc.ToString();
        }

        private static void AppendEpisode(EpisodeData data, int padSeriesName, int padEpisodeID, int padEpisodeName, int padAddInfo, StringBuilder email)
        {
            email.Append(data.DisplayName.PadRight(padSeriesName + 1));
            email.Append(data.EpisodeID.PadLeft(padEpisodeID));
            email.Append(" \"");

            var episodeName = data.EpisodeName + "\"";

            episodeName = episodeName.PadRight(padEpisodeName + 1);

            email.Append(episodeName);
            email.Append(" ");
            email.Append(data.AddInfo.PadRight(padAddInfo));
            email.Append(" (");
            email.Append(data.FileSize.ToString(3));
            email.AppendLine(")\t");
        }

        private static void CalculatePadding(List<EpisodeData> list, out int padSeriesName, out int padEpisodeID, out int padEpisodeName, out int padAddInfo)
        {
            padSeriesName = 1;
            padEpisodeID = 1;
            padEpisodeName = 1;
            padAddInfo = 1;

            foreach (EpisodeData data in list)
            {
                if (data.DisplayName.Length > padSeriesName)
                {
                    padSeriesName = data.DisplayName.Length;
                }

                if (data.EpisodeID.Length > padEpisodeID)
                {
                    padEpisodeID = data.EpisodeID.Length;
                }

                if (data.EpisodeName.Length > padEpisodeName)
                {
                    padEpisodeName = data.EpisodeName.Length;
                }

                if (data.AddInfo.Length > padAddInfo)
                {
                    padAddInfo = data.AddInfo.Length;
                }
            }
        }

        private static void AddInfo(ref string addInfo, HashSet<string> names, EpisodeData episode, string text)
        {
            if (names.Contains(episode.SeriesName) == false)
            {
                addInfo += $"New  {text}: {episode.DisplayName}{Environment.NewLine}{Environment.NewLine}";

                names.Add(episode.SeriesName);
            }
        }

        private static string GetPlainTextAppendix(string newFileName)
        {
            var email = new StringBuilder();

            email.AppendLine(@"<" + RemoteDir + "CopySeriesFromShare.exe>");
            email.AppendLine();
            email.AppendLine(@"<" + Path.Combine(RemoteDir, "_RecentFiles", newFileName) + ">");
            email.AppendLine();
            email.AppendLine(@"<" + RemoteDir + CompleteSeriesFile + ">");
            email.AppendLine();
            email.AppendLine(@"<" + RemoteDir + ">");

            return email.ToString();
        }

        private static string GetHtmlAppendix(string newFileName)
        {
            var email = new StringBuilder();

            email.AppendLine("<p style=\"font-family: monospace;\">");

            AppendFooter(email, RemoteDir + "CopySeriesFromShare.exe");

            AppendFooter(email, Path.Combine(RemoteDir, "_RecentFiles", newFileName));

            AppendFooter(email, RemoteDir + CompleteSeriesFile);

            AppendFooter(email, RemoteDir);

            return email.ToString();
        }

        private static void AppendFooter(StringBuilder email, string path)
        {
            email.AppendLine("&lt;<a href=\"" + path + "\">" + path + "</a>&gt;<br /><br />");
            email.AppendLine();
        }
    }
}