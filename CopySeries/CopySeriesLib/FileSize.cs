namespace DoenaSoft.CopySeries
{
    using System;
    using System.Globalization;

    public class FileSize
    {
        private string FileSizeFormatted { get; }

        private ulong FileSizeInBytes { get; }

        public ulong InBytes => FileSizeInBytes;

        public FileSize(ulong fileSize)
        {
            FileSizeInBytes = fileSize;

            FileSizeFormatted = FormatFileSize(fileSize, 0);
        }

        private static string FormatFileSize(ulong fileSize, int numberPadding)
        {
            string bytesPower;
            if (CheckOrderOfMagnitude(fileSize, 40, 1, out var roundBytes))
            {
                bytesPower = " TiB";
            }
            else if (CheckOrderOfMagnitude(fileSize, 30, 1, out roundBytes))
            {
                bytesPower = " GiB";
            }
            else if (CheckOrderOfMagnitude(fileSize, 20, 0, out roundBytes))
            {
                bytesPower = " MiB";
            }
            else if (CheckOrderOfMagnitude(fileSize, 10, 0, out roundBytes))
            {
                bytesPower = " KiB";
            }
            else
            {
                roundBytes = fileSize;

                bytesPower = " Byte";
            }

            string formatted = roundBytes.ToString(CultureInfo.GetCultureInfo("de-DE")).PadLeft(numberPadding) + bytesPower;

            return formatted;
        }

        private static bool CheckOrderOfMagnitude(decimal fileSize, double exponent, int decimals, out decimal roundBytes)
        {
            decimal pow = (decimal)(Math.Pow(2, exponent));

            decimal quotient = fileSize / pow;

            roundBytes = Math.Round(quotient, decimals, MidpointRounding.AwayFromZero);

            return roundBytes >= 1;
        }

        public override string ToString() => FileSizeFormatted;

        public string ToString(int numberPadding) => FormatFileSize(FileSizeInBytes, numberPadding);
    }
}