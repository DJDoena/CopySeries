namespace DoenaSoft.CopySeries.Main.Implementations
{
    internal sealed class FileEntryViewModel : IFileEntryViewModel
    {
        private string File { get; }

        public FileEntryViewModel(string file)
        {
            File = file;
        }

        public string FullName
            => (File);

        public string DisplayName
        {
            get
            {
                var file = File;

                var root = Properties.Settings.Default.SourcePath;

                file = file.Replace(root, string.Empty);

                return (file);
            }
        }
    }
}