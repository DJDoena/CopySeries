namespace DoenaSoft.CopySeries
{
    public static class Program
    {
        public static void Main()
        {
            var settings = DirAtTargetSettings.Default;

            Dir.CreateFileList(settings.StickDrive, settings.TargetPath);
        }
    }
}