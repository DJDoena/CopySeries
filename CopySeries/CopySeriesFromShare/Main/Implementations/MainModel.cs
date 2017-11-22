using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DoenaSoft.AbstractionLayer.IOServices;
using DoenaSoft.AbstractionLayer.UIServices;
using DoenaSoft.ToolBox.Generics;

namespace DoenaSoft.CopySeries.Main.Implementations
{
    internal sealed class MainModel : IMainModel
    {
        #region Fields

        private Nullable<Int64> m_Size;

        #endregion

        #region Properties

        private IUIServices UIServices { get; }

        private IIOServices IOServices { get; }

        private HashSet<String> HashedEntries { get; }

        private String TargetLocation { get; set; }

        private String Overwrite { get; set; }

        private CancellationToken CancellationToken { get; set; }

        #endregion

        #region Constructor

        public MainModel(IUIServices uiServices
            , IIOServices ioServices)
        {
            UIServices = uiServices;
            IOServices = ioServices;

            HashedEntries = new HashSet<String>();
        }

        #endregion

        #region IMainModel

        #region Properties

        public IEnumerable<String> Entries
            => (HashedEntries);

        public Boolean PreserveSubFolders
        {
            get
            {
                return (Properties.Settings.Default.PreserveFolderStructure);
            }
            set
            {
                Properties.Settings.Default.PreserveFolderStructure = value;
            }
        }

        public Boolean IgnoreResolutionFolders
        {
            get
            {
                return (Properties.Settings.Default.IgnoreResolutionFolders);
            }
            set
            {
                Properties.Settings.Default.IgnoreResolutionFolders = value;
            }
        }

        public Int64 Size
        {
            get
            {
                if (m_Size.HasValue == false)
                {
                    m_Size = CalculateSize();
                }

                return (m_Size.Value);
            }
        }

        public Int64 ProgressValue { get; private set; }

        #endregion

        #region Events

        public event EventHandler<EventArgs<TimeSpan>> CopyPaused;

        public event EventHandler SizeChanged;

        public event EventHandler ProgressChanged;

        #endregion

        #region Methods

        public void AddEntry(String entry)
        {
            HashedEntries.Add(entry);

            ResetSize();
        }

        public void ClearEntries()
        {
            HashedEntries.Clear();

            ResetSize();
        }

        public void RemoveEntry(String entry)
        {
            HashedEntries.Remove(entry);

            ResetSize();
        }

        public void ReadXml(String fileName)
        {
            try
            {
                using (System.IO.Stream fs = IOServices.GetFileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    RecentFiles recentFiles = Serializer<RecentFiles>.Deserialize(fs);

                    foreach (String file in recentFiles.Files)
                    {
                        if (IOServices.File.Exists(file))
                        {
                            HashedEntries.Add(file);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UIServices.ShowMessageBox(ex.Message, "Error", Buttons.OK, Icon.Error);
            }
        }

        public void ApplyFilter(IEnumerable<String> filter
            , Boolean noSubs
            , Boolean onlyHDs
            , Boolean onlySDs)
        {
            List<String> entries = Entries.ToList();

            foreach (String entry in entries)
            {
                if ((entry.Contains(" 1x01")) || (entry.Contains(" 1x00")))
                {
                    continue;
                }

                Boolean isValid = CheckFilter(entry, filter);

                if (isValid)
                {
                    isValid = CheckExtension(entry, noSubs, onlyHDs, onlySDs);
                }

                if (isValid == false)
                {
                    RemoveEntry(entry);
                }
            }
        }

        public void Copy(String targetLocation
            , String overwrite
            , CancellationToken cancellationToken)
        {
            TargetLocation = targetLocation;
            Overwrite = overwrite;
            CancellationToken = cancellationToken;

            ProgressValue = 0;

            List<SourceTarget> fileInfos = new List<SourceTarget>();

            foreach (String entry in Entries)
            {
                if (IOServices.Directory.Exists(entry))
                {
                    String[] files = IOServices.Directory.GetFiles(entry, searchOption: System.IO.SearchOption.AllDirectories);

                    fileInfos.AddRange(files.Select(file => new SourceTarget(IOServices.GetFileInfo(file))));
                }
                else if (IOServices.File.Exists(entry))
                {
                    fileInfos.Add(new SourceTarget(IOServices.GetFileInfo(entry)));
                }
                else
                {
                    UIServices.ShowMessageBox("Something is weird about\n" + entry, "?!?", Buttons.OK, Icon.Error);

                    return;
                }
            }

            ResetSize();

            IDriveInfo driveInfo = IOServices.GetDriveInfo(IOServices.GetDirectoryInfo(TargetLocation).Root.Name.Substring(0, 1));

            if (driveInfo.AvailableFreeSpace <= Size)
            {
                FileSize spaceSize = new FileSize(driveInfo.AvailableFreeSpace);

                FileSize bytesSize = new FileSize(Size);

                UIServices.ShowMessageBox($"Target is Full!{Environment.NewLine}Available: {spaceSize}{Environment.NewLine}Needed: {bytesSize}", "Target Full", Buttons.OK, Icon.Warning);

                return;
            }

            Copy(fileInfos);

            ProgressValue = 0;

            App.WasCopied = true;
        }

        #endregion

        #endregion

        #region Methods

        #region Copy

        private void Copy(List<SourceTarget> fileInfos)
        {
            Boolean taskCancelled = false;

            try
            {
                foreach (SourceTarget fileInfo in fileInfos)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        UIServices.ShowMessageBox("The Copy process was aborted.", String.Empty, Buttons.OK, Icon.Warning);

                        taskCancelled = true;

                        return;
                    }

                    if (PreserveSubFolders)
                    {
                        EnsureSubFolders(fileInfo);
                    }
                    else
                    {
                        fileInfo.TargetFolder = IOServices.GetDirectoryInfo(TargetLocation);
                    }
                }

                taskCancelled = CommenceCopy(fileInfos);
            }
            catch (System.IO.IOException ioEx)
            {
                UIServices.ShowMessageBox(ioEx.Message, "?!?", Buttons.OK, Icon.Error);
            }
            catch (Exception ex)
            {
                UIServices.ShowMessageBox(ex.Message, "?!?", Buttons.OK, Icon.Error);
            }
            finally
            {
                ProgressChanged?.Invoke(this, EventArgs.Empty);

                if (taskCancelled == false)
                {
                    UIServices.ShowMessageBox("Copy Finished.", String.Empty, Buttons.OK, Icon.Information);
                }
            }
        }

        private Boolean CommenceCopy(List<SourceTarget> fileInfos)
        {
            foreach (SourceTarget fileInfo in fileInfos)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    UIServices.ShowMessageBox("The Copy process was aborted.", String.Empty, Buttons.OK, Icon.Warning);

                    return (true);
                }

                String path = IOServices.Path.Combine(fileInfo.TargetFolder.FullName, fileInfo.SourceFile.Name);

                IFileInfo targetFileInfo = IOServices.GetFileInfo(path);

                Result result = GetOverwriteResult(fileInfo, targetFileInfo);

                if (result == Result.Cancel)
                {
                    return (true);
                }
                else if (result == Result.No)
                {
                    m_Size -= fileInfo.SourceFile.Length;

                    SizeChanged?.Invoke(this, EventArgs.Empty);

                    continue;
                }
                else if (result == Result.Yes)
                {
                    try
                    {
                        if (IOServices.File.Exists(targetFileInfo.FullName))
                        {
                            IOServices.File.SetAttributes(targetFileInfo.FullName, System.IO.FileAttributes.Normal | System.IO.FileAttributes.Archive);
                        }

                        IOServices.File.Copy(fileInfo.SourceFile.FullName, targetFileInfo.FullName, true);
                    }
                    catch (Exception ex)
                    {
                        Int64 startTicks = DateTime.Now.Ticks;

                        if (UIServices.ShowMessageBox(ex.Message + "\nContinue?", "Continue?", Buttons.YesNo, Icon.Question) == Result.Yes)
                        {
                            Int64 endTicks = DateTime.Now.Ticks;

                            TimeSpan span = new TimeSpan(endTicks - startTicks);

                            CopyPaused?.Invoke(this, new EventArgs<TimeSpan>(span));

                            continue;
                        }
                        else
                        {
                            return (true);
                        }
                    }

                    ProgressValue += fileInfo.SourceFile.Length;

                    ProgressChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            return (false);
        }

        private Result GetOverwriteResult(SourceTarget source
            , IFileInfo target)
        {
            Result result = Result.Yes;

            if (target.Exists)
            {
                result = Result.No;

                if (Overwrite == OverwriteOptionConstants.Ask)
                {
                    Int64 startTicks = DateTime.Now.Ticks;

                    result = UIServices.ShowMessageBox("Overwrite \"" + target.FullName + "\"\nfrom \"" + source.SourceFile.FullName + "\"?", "Overwrite?", Buttons.YesNoCancel, Icon.Question);

                    Int64 endTicks = DateTime.Now.Ticks;

                    TimeSpan span = new TimeSpan(endTicks - startTicks);

                    CopyPaused?.Invoke(this, new EventArgs<TimeSpan>(span));
                }
                else if (Overwrite == OverwriteOptionConstants.Always)
                {
                    result = Result.Yes;
                }
            }

            return (result);
        }

        private void EnsureSubFolders(SourceTarget fileInfo)
        {
            String directoryName = fileInfo.SourceFile.DirectoryName;

            String newPath;
            if (directoryName.StartsWith(Properties.Settings.Default.SourcePath))
            {
                newPath = directoryName.Replace(Properties.Settings.Default.SourcePath, String.Empty);
            }
            else
            {
                Int32 indexOfFirstBackSlash;

                indexOfFirstBackSlash = directoryName.IndexOf(@"\");

                newPath = directoryName.Substring(indexOfFirstBackSlash + 1);
            }

            if (IgnoreResolutionFolders)
            {
                if ((newPath.EndsWith(@"\SD")) || (newPath.EndsWith(@"\HD")))
                {
                    newPath = newPath.Substring(0, newPath.Length - 3);
                }
            }

            String path = IOServices.Path.Combine(TargetLocation, newPath);

            fileInfo.TargetFolder = IOServices.GetDirectoryInfo(path);

            if (fileInfo.TargetFolder.Exists == false)
            {
                fileInfo.TargetFolder.Create();
            }
        }

        #endregion

        private static Boolean CheckFilter(String entry
            , IEnumerable<String> filter)
        {
            Boolean isValid = true;

            if (filter.Any())
            {
                isValid = false;

                foreach (String series in filter)
                {
                    if (entry.Contains(@"\" + series + @"\"))
                    {
                        isValid = true;

                        break;
                    }
                }
            }

            return (isValid);
        }

        #region CheckExtension

        private Boolean CheckExtension(String entry
            , Boolean noSubs
            , Boolean onlyHDs
            , Boolean onlySDs)
        {
            Boolean isValid = true;

            if ((noSubs) && (entry.EndsWith(".srt")))
            {
                isValid = false;
            }
            else if ((entry.EndsWith(".mkv")) || (entry.EndsWith(".mp4")))
            {
                if (onlySDs)
                {
                    if (IsNotSD(entry))
                    {
                        isValid = CheckForPartner(entry, false);
                    }
                }
                else if (onlyHDs)
                {
                    if (IsSD(entry))
                    {
                        isValid = CheckForPartner(entry, true);
                    }
                }
            }
            else if ((onlyHDs) && (entry.EndsWith(".srt") == false))
            {
                isValid = CheckForPartner(entry, true);
            }

            return (isValid);
        }

        private static Boolean IsSD(String entry)
        {
            const String SDMkv = ".480.mkv";
            const String SDMp4 = ".480.mp4";

            Boolean isSD = ((entry.EndsWith(SDMkv)) || (entry.EndsWith(SDMp4)));

            return (isSD);
        }

        private static Boolean IsNotSD(String entry)
            => (IsSD(entry) == false);

        private Boolean CheckForPartner(String currentFile
            , Boolean toHD)
        {
            Boolean valid = true;

            IFileInfo fi = IOServices.GetFileInfo(currentFile);

            String fileWithoutExtension = fi.Name.Substring(0, fi.Name.LastIndexOf("."));

            if ((fileWithoutExtension.EndsWith(".480")) || (fileWithoutExtension.EndsWith(".720")) || (fileWithoutExtension.EndsWith(".1080")))
            {
                fileWithoutExtension = fileWithoutExtension.Substring(0, fileWithoutExtension.LastIndexOf("."));
            }

            IEnumerable<String> partners = toHD ? GetHDExtensions() : GetSDExtensions();

            foreach (String partner in partners)
            {
                String folder = fi.DirectoryName + @"\..\" + (toHD ? "HD" : "SD");

                String fileInQuestion = folder + @"\" + fileWithoutExtension + partner;

                if (IOServices.File.Exists(fileInQuestion))
                {
                    valid = false;

                    break;
                }
            }

            return (valid);
        }

        private static IEnumerable<String> GetSDExtensions()
        {
            yield return (".480.mkv");
            yield return (".480.mp4");
            yield return (".mp4");
            yield return (".avi");
            yield return (".flv");
        }

        private static IEnumerable<String> GetHDExtensions()
        {
            yield return (".1080.mkv");
            yield return (".720.mkv");
            yield return (".1080.mp4");
            yield return (".720.mp4");
            yield return (".mkv");
        }

        #endregion

        #region CalculateSize

        private Int64 CalculateSize()
        {
            Int64 size = 0;

            foreach (String entry in Entries)
            {
                GetEntrySize(entry, ref size);
            }

            return (size);
        }

        private void GetEntrySize(String entry
            , ref Int64 size)
        {
            if (IOServices.Directory.Exists(entry))
            {
                GetFolderSize(entry, ref size);
            }
            else if (IOServices.File.Exists(entry))
            {
                GetFileSize(entry, ref size);
            }
        }

        private void GetFolderSize(String folder
            , ref Int64 size)
        {
            String[] files = IOServices.Directory.GetFiles(folder, searchOption: System.IO.SearchOption.AllDirectories);

            foreach (String file in files)
            {
                GetFileSize(file, ref size);
            }
        }

        private void GetFileSize(String file
            , ref Int64 bytes)
        {
            IFileInfo fi = IOServices.GetFileInfo(file);

            bytes += fi.Length;
        }

        #endregion

        private void ResetSize()
        {
            m_Size = null;

            SizeChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion
    }
}