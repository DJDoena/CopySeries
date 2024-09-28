namespace DoenaSoft.CopySeries.Main.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using AbstractionLayer.IOServices;
    using AbstractionLayer.UIServices;
    using ToolBox.Generics;

    internal sealed class MainModel : IMainModel
    {
        #region Fields

        private Nullable<ulong> m_Size;

        #endregion

        #region Properties

        private IUIServices UIServices { get; }

        private IIOServices IOServices { get; }

        private HashSet<string> HashedEntries { get; }

        private string TargetLocation { get; set; }

        private string Overwrite { get; set; }

        private CancellationToken CancellationToken { get; set; }

        #endregion

        #region Constructor

        public MainModel(IUIServices uiServices
            , IIOServices ioServices)
        {
            UIServices = uiServices;
            IOServices = ioServices;

            HashedEntries = new HashSet<string>();
        }

        #endregion

        #region IMainModel

        #region Properties

        public IEnumerable<string> Entries
            => (HashedEntries);

        public bool PreserveSubFolders
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

        public bool IgnoreResolutionFolders
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

        public ulong Size
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

        public ulong ProgressValue { get; private set; }

        #endregion

        #region Events

        public event EventHandler<EventArgs<TimeSpan>> CopyPaused;

        public event EventHandler SizeChanged;

        public event EventHandler ProgressChanged;

        #endregion

        #region Methods

        public void AddEntry(string entry)
        {
            HashedEntries.Add(entry);

            ResetSize();
        }

        public void ClearEntries()
        {
            HashedEntries.Clear();

            ResetSize();
        }

        public void RemoveEntry(string entry)
        {
            HashedEntries.Remove(entry);

            ResetSize();
        }

        public void ReadXml(string fileName)
        {
            try
            {
                using (var fs = IOServices.GetFileStream(fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                {
                    var recentFiles = XmlSerializer<RecentFiles>.Deserialize(fs);

                    foreach (var file in recentFiles.Files)
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

        public void ApplyFilter(IEnumerable<string> filter
            , bool noSubs
            , bool onlyHDs
            , bool onlySDs)
        {
            var entries = Entries.ToList();

            foreach (var entry in entries)
            {
                if ((entry.Contains(" 1x01")) || (entry.Contains(" 1x00")))
                {
                    continue;
                }

                var isValid = CheckFilter(entry, filter);

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

        public void Copy(string targetLocation
            , string overwrite
            , CancellationToken cancellationToken)
        {
            TargetLocation = targetLocation;
            Overwrite = overwrite;
            CancellationToken = cancellationToken;

            ProgressValue = 0;

            var fileInfos = new List<SourceTarget>();

            foreach (var entry in Entries)
            {
                if (IOServices.Folder.Exists(entry))
                {
                    var files = IOServices.Folder.GetFileNames(entry, searchOption: System.IO.SearchOption.AllDirectories);

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

            var driveInfo = IOServices.GetDriveInfo(IOServices.GetFolderInfo(TargetLocation).Root.Name.Substring(0, 1));

            if (driveInfo.AvailableFreeSpace <= Size)
            {
                var spaceSize = new FileSize(driveInfo.AvailableFreeSpace);

                var bytesSize = new FileSize(Size);

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
            var taskCancelled = false;

            try
            {
                foreach (var fileInfo in fileInfos)
                {
                    if (CancellationToken.IsCancellationRequested)
                    {
                        UIServices.ShowMessageBox("The Copy process was aborted.", string.Empty, Buttons.OK, Icon.Warning);

                        taskCancelled = true;

                        return;
                    }

                    if (PreserveSubFolders)
                    {
                        EnsureSubFolders(fileInfo);
                    }
                    else
                    {
                        fileInfo.TargetFolder = IOServices.GetFolderInfo(TargetLocation);
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
                    UIServices.ShowMessageBox("Copy Finished.", string.Empty, Buttons.OK, Icon.Information);
                }
            }
        }

        private bool CommenceCopy(List<SourceTarget> fileInfos)
        {
            foreach (var fileInfo in fileInfos)
            {
                if (CancellationToken.IsCancellationRequested)
                {
                    UIServices.ShowMessageBox("The Copy process was aborted.", string.Empty, Buttons.OK, Icon.Warning);

                    return (true);
                }

                var path = IOServices.Path.Combine(fileInfo.TargetFolder.FullName, fileInfo.SourceFile.Name);

                var targetFileInfo = IOServices.GetFileInfo(path);

                var result = GetOverwriteResult(fileInfo, targetFileInfo);

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
                        var startTicks = DateTime.Now.Ticks;

                        if (UIServices.ShowMessageBox(ex.Message + "\nContinue?", "Continue?", Buttons.YesNo, Icon.Question) == Result.Yes)
                        {
                            var endTicks = DateTime.Now.Ticks;

                            var span = new TimeSpan(endTicks - startTicks);

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
            var result = Result.Yes;

            if (target.Exists)
            {
                result = Result.No;

                if (Overwrite == OverwriteOptionConstants.Ask)
                {
                    var startTicks = DateTime.Now.Ticks;

                    result = UIServices.ShowMessageBox("Overwrite \"" + target.FullName + "\"\nfrom \"" + source.SourceFile.FullName + "\"?", "Overwrite?", Buttons.YesNoCancel, Icon.Question);

                    var endTicks = DateTime.Now.Ticks;

                    var span = new TimeSpan(endTicks - startTicks);

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
            var directoryName = fileInfo.SourceFile.FolderName;

            string newPath;
            if (directoryName.StartsWith(Properties.Settings.Default.SourcePath))
            {
                newPath = directoryName.Replace(Properties.Settings.Default.SourcePath, string.Empty);
            }
            else
            {
                int indexOfFirstBackSlash;

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

            var path = IOServices.Path.Combine(TargetLocation, newPath);

            fileInfo.TargetFolder = IOServices.GetFolderInfo(path);

            if (fileInfo.TargetFolder.Exists == false)
            {
                fileInfo.TargetFolder.Create();
            }
        }

        #endregion

        private static bool CheckFilter(string entry
            , IEnumerable<string> filter)
        {
            var isValid = true;

            if (filter.Any())
            {
                isValid = false;

                foreach (var series in filter)
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

        private bool CheckExtension(string entry
            , bool noSubs
            , bool onlyHDs
            , bool onlySDs)
        {
            var isValid = true;

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

        private static bool IsSD(string entry)
        {
            const string SDMkv = ".480.mkv";
            const string SDMp4 = ".480.mp4";

            var isSD = ((entry.EndsWith(SDMkv)) || (entry.EndsWith(SDMp4)));

            return (isSD);
        }

        private static bool IsNotSD(string entry)
            => (IsSD(entry) == false);

        private bool CheckForPartner(string currentFile
            , bool toHD)
        {
            var valid = true;

            var fi = IOServices.GetFileInfo(currentFile);

            var fileWithoutExtension = fi.Name.Substring(0, fi.Name.LastIndexOf("."));

            if ((fileWithoutExtension.EndsWith(".480")) || (fileWithoutExtension.EndsWith(".720")) || (fileWithoutExtension.EndsWith(".1080")))
            {
                fileWithoutExtension = fileWithoutExtension.Substring(0, fileWithoutExtension.LastIndexOf("."));
            }

            var partners = toHD ? GetHDExtensions() : GetSDExtensions();

            foreach (var partner in partners)
            {
                var folder = fi.FolderName + @"\..\" + (toHD ? "HD" : "SD");

                var fileInQuestion = folder + @"\" + fileWithoutExtension + partner;

                if (IOServices.File.Exists(fileInQuestion))
                {
                    valid = false;

                    break;
                }
            }

            return (valid);
        }

        private static IEnumerable<string> GetSDExtensions()
        {
            yield return (".480.mkv");
            yield return (".480.mp4");
            yield return (".mp4");
            yield return (".avi");
            yield return (".flv");
        }

        private static IEnumerable<string> GetHDExtensions()
        {
            yield return (".1080.mkv");
            yield return (".720.mkv");
            yield return (".1080.mp4");
            yield return (".720.mp4");
            yield return (".mkv");
        }

        #endregion

        #region CalculateSize

        private ulong CalculateSize()
        {
            ulong size = 0;

            foreach (var entry in Entries)
            {
                GetEntrySize(entry, ref size);
            }

            return (size);
        }

        private void GetEntrySize(string entry
            , ref ulong size)
        {
            if (IOServices.Folder.Exists(entry))
            {
                GetFolderSize(entry, ref size);
            }
            else if (IOServices.File.Exists(entry))
            {
                GetFileSize(entry, ref size);
            }
        }

        private void GetFolderSize(string folder
            , ref ulong size)
        {
            var files = IOServices.Folder.GetFileNames(folder, searchOption: System.IO.SearchOption.AllDirectories);

            foreach (var file in files)
            {
                GetFileSize(file, ref size);
            }
        }

        private void GetFileSize(string file
            , ref ulong bytes)
        {
            var fi = IOServices.GetFileInfo(file);

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