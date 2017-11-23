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
        private static String StickDrive { get; }

        private static String SourceDir { get; }

        private static String TargetDir { get; }

        private static String RemoteDir { get; }

        private static String CompleteSeriesFile { get; }

        private static Int64 TenGibiByte { get; }

        private static String DirFile { get; }

        static Program()
        {
            StickDrive = CopySeriesIntoShareAtTargetSettings.Default.StickDrive;

            SourceDir = Path.Combine(StickDrive, "Series");

            TargetDir = CopySeriesIntoShareAtTargetSettings.Default.TargetPath;

            RemoteDir = CopySeriesIntoShareAtTargetSettings.Default.SourcePath;

            CompleteSeriesFile = "CompleteSeriesList.txt";

            TenGibiByte = 10L * ((Int64)(Math.Pow(2, 30)));

            DirFile = Path.Combine(StickDrive, "dir.txt");
        }

        [STAThread]
        public static void Main(String[] args)
        {
            try
            {
                DirectoryInfo di = ((args == null) || (args.Length == 0)) ? (new DirectoryInfo(SourceDir)) : (new DirectoryInfo(args[0]));

                while (File.Exists(DirFile) == false)
                {
                    Console.WriteLine(DirFile + " not available.");
                    Console.WriteLine();
                    Console.WriteLine("Press <Enter> to retry.");
                    Console.ReadLine();
                }

                List<EpisodeData> episodes;
                RecentFiles recentFiles;
                if (Helper.CopySeriesIntoShare(di, SearchOption.AllDirectories, TargetDir, true, RemoteDir, StickDrive, out episodes, out recentFiles))
                {
                    const String SubDir = "_RecentFiles";

                    Serializer<RecentFiles>.Serialize(Path.Combine(TargetDir, SubDir, "RecentFiles.xml"), recentFiles);

                    String newFileName = Helper.GetNewFileName("RecentFiles", "xml", TargetDir, SubDir);

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

        private static void WriteEmail(List<EpisodeData> episodes
            , String fileName)
        {
            episodes.Sort();

            Int32 padSeriesName;
            Int32 padEpisodeID;
            Int32 padEpisodeName;
            Int32 padAddInfo;
            CalculatePadding(episodes, out padSeriesName, out padEpisodeID, out padEpisodeName, out padAddInfo);

            StringBuilder email = new StringBuilder();

            email.AppendLine();

            Int64 fileSize = 0;

            foreach (EpisodeData episode in episodes)
            {
                AppendEpisode(episode, padSeriesName, padEpisodeID, padEpisodeName, padAddInfo, email);

                fileSize += episode.FileSize.InBytes;
            }

            AddSummarySize(padSeriesName, padEpisodeID, padEpisodeName, padAddInfo, fileSize, email);

            String emailText = email.ToString();

            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine(emailText);
            Console.WriteLine(GetPlainTextAppendix(fileName));

            Boolean success = false;

            do
            {
                try
                {
                    String bcc = TryCreateOutlookMail(episodes, fileName, emailText);

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

        private static String TryCreateOutlookMail(List<EpisodeData> episodes
            , String fileName
            , String emailText)
        {
            Outlook.Application outlook = new Outlook.Application();

            Outlook.MailItem mail = (Outlook.MailItem)(outlook.CreateItem(Outlook.OlItemType.olMailItem));

            String addInfo;
            Boolean newSeries = AddNewSeasonInfo(episodes, mail, out addInfo);

            mail.BodyFormat = Outlook.OlBodyFormat.olFormatHTML;

            String bodyText = "<pre>";

            if (String.IsNullOrEmpty(addInfo) == false)
            {
                bodyText += addInfo;
            }

            bodyText += System.Web.HttpUtility.HtmlEncode(emailText) + "</pre>" + GetHtmlAppendix(fileName);

            mail.HTMLBody = bodyText;

            String bcc = GetBcc(newSeries);

            mail.BCC = bcc;

            mail.Display(false);

            Marshal.ReleaseComObject(mail);
            mail = null;

            Marshal.ReleaseComObject(outlook);
            outlook = null;

            return (bcc);
        }

        private static Boolean AddNewSeasonInfo(List<EpisodeData> episodes
            , Outlook.MailItem mail
            , out String addInfo)
        {
            Boolean newSeries = false;

            IEnumerable<EpisodeData> newSeason = episodes.Where(episode => episode.IsFirstOfSeason);

            addInfo = String.Empty;

            if (newSeason.Any())
            {
                HashSet<String> names;

                mail.Subject = "Share Update (with Info)";

                names = new HashSet<String>();

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
            else
            {
                mail.Subject = "Share Update";
            }

            return (newSeries);
        }

        private static void AddSummarySize(Int32 padSeriesName
            , Int32 padSeasonID
            , Int32 padEpisodeName
            , Int32 padAddInfo
            , Int64 fileSize
            , StringBuilder email)
        {
            Int32 padding = padSeriesName + padSeasonID + padEpisodeName + padAddInfo + 5;

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

        private static String GetBcc(Boolean newSeries)
        {
            Recipients recipients = Serializer<Recipients>.Deserialize(Path.Combine(TargetDir, "Recipients.xml"));

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

        private static void AppendEpisode(EpisodeData data
            , Int32 padSeriesName
            , Int32 padEpisodeID
            , Int32 padEpisodeName
            , Int32 padAddInfo
            , StringBuilder email)
        {
            email.Append(data.DisplayName.PadRight(padSeriesName + 1));
            email.Append(data.EpisodeID.PadLeft(padEpisodeID));
            email.Append(" \"");

            String episodeName = data.EpisodeName + "\"";

            episodeName = episodeName.PadRight(padEpisodeName + 1);

            email.Append(episodeName);
            email.Append(" ");
            email.Append(data.AddInfo.PadRight(padAddInfo));
            email.Append(" (");
            email.Append(data.FileSize.ToString(3));
            email.AppendLine(")\t");
        }

        private static void CalculatePadding(List<EpisodeData> list
            , out Int32 padSeriesName
            , out Int32 padEpisodeID
            , out Int32 padEpisodeName
            , out Int32 padAddInfo)
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

        private static String GetPlainTextAppendix(String newFileName)
        {
            StringBuilder email = new StringBuilder();

            email.AppendLine(@"<" + RemoteDir + "CopySeriesFromShare.exe>");
            email.AppendLine();
            email.AppendLine(@"<" + Path.Combine(RemoteDir, "_RecentFiles", newFileName) + ">");
            email.AppendLine();
            email.AppendLine(@"<" + RemoteDir + CompleteSeriesFile + ">");
            email.AppendLine();
            email.AppendLine(@"<" + RemoteDir + ">");

            return (email.ToString());
        }

        private static String GetHtmlAppendix(String newFileName)
        {
            StringBuilder email = new StringBuilder();

            email.AppendLine("<p style=\"font-family: monospace;\">");

            AppendFooter(email, RemoteDir + "CopySeriesFromShare.exe");

            AppendFooter(email, Path.Combine(RemoteDir, "_RecentFiles", newFileName));

            AppendFooter(email, RemoteDir + CompleteSeriesFile);

            AppendFooter(email, RemoteDir);

            return (email.ToString());
        }

        private static void AppendFooter(StringBuilder email
            , String path)
        {
            email.AppendLine("&lt;<a href=\"" + path + "\">" + path + "</a>&gt;<br /><br />");
            email.AppendLine();
        }
    }
}