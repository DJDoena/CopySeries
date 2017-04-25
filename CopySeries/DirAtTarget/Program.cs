namespace DoenaSoft.CopySeries
{
    public static class Program
    {
        public static void Main()
        {
            DirAtTargetSettings settings = DirAtTargetSettings.Default;

            Dir.CreateFileList(settings.StickDrive, settings.TargetPath);
        }
    }
}