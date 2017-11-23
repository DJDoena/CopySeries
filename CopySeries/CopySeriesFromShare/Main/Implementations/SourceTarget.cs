namespace DoenaSoft.CopySeries.Main.Implementations
{
    using AbstractionLayer.IOServices;

    internal sealed class SourceTarget
    {
        internal IFileInfo SourceFile { get; private set; }

        internal IFolderInfo TargetFolder { get; set; }

        internal SourceTarget(IFileInfo sourceFile)
        {
            SourceFile = sourceFile;
        }
    }
}
