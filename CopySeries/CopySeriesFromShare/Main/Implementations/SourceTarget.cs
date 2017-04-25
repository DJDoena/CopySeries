using DoenaSoft.AbstractionLayer.IOServices;

namespace DoenaSoft.CopySeries.Main.Implementations
{
    internal sealed class SourceTarget
    {
        internal IFileInfo SourceFile { get; private set; }

        internal IDirectoryInfo TargetFolder { get; set; }

        internal SourceTarget(IFileInfo sourceFile)
        {
            SourceFile = sourceFile;
        }
    }
}
