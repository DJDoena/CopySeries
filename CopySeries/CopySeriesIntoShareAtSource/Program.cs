using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries
{
    public class Program
    {
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

                WriteEmail(episodeList);

                Helper.CleanFolder("RecentFiles", "txt", 1, targetDir, "_CopySuggestions");

                Process process = new Process();

                process.StartInfo = new ProcessStartInfo();
                process.StartInfo.FileName = "DirAtSource.exe";

                process.Start();

                process.WaitForExit();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }

        private static void WriteEmail(List<EpisodeData> episodes)
        {
            episodes.Sort();

            StringBuilder email = new StringBuilder();

            email.Append("mailto:?bcc=");

            Boolean newSeries = episodes.Any(episode => episode.IsPilot);

            String bcc = GetBcc(newSeries);

            email.Append(bcc);
            email.Append("&subject=Share Update (Home)&body=");

            foreach (EpisodeData episode in episodes)
            {
                String name = episode.ToString().Replace("\"", "%22");

                email.Append(name);
                email.Append("%0D%0A");
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

            if (bcc.Length > 0)
            {
                bcc.Remove(bcc.Length - 1, 1);
            }

            return (bcc.ToString());
        }

        private static Boolean WithoutSubFolder(String[] args)
            => ((args?.Length > 0) && (args[0].ToLower() == "/withoutsubfolders"));
    }
}