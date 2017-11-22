using System;

namespace DoenaSoft.CopySeries.Main.Implementations
{
    internal sealed class FileEntryViewModel : IFileEntryViewModel
    {
        private String File { get; }

        public FileEntryViewModel(String file)
        {
            File = file;
        }

        public String FullName
            => (File);

        public String DisplayName
        {
            get
            {
                String file = File;

                String root = Properties.Settings.Default.SourcePath;

                file = file.Replace(root, String.Empty);

                return (file);
            }
        }
    }
}