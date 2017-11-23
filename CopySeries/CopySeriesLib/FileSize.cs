namespace DoenaSoft.CopySeries
{
    using System;
    using System.Globalization;

    public class FileSize
    {
        private String FileSizeFormatted { get; }

        private Int64 FileSizeInBytes { get; }

        public Int64 InBytes
            => (FileSizeInBytes);

        public FileSize(Int64 fileSize)
        {
            FileSizeInBytes = fileSize;

            FileSizeFormatted = FormatFileSize(fileSize, 0);
        }

        private static String FormatFileSize(Int64 fileSize
            , Int32 numberPadding)
        {
            String bytesPower;
            Decimal roundBytes;
            if (CheckOrderOfMagnitude(fileSize, 40, 1, out roundBytes))
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

            String formatted = roundBytes.ToString(CultureInfo.GetCultureInfo("de-DE")).PadLeft(numberPadding) + bytesPower;

            return (formatted);
        }

        private static Boolean CheckOrderOfMagnitude(Decimal fileSize
            , Double exponent
            , Int32 decimals
            , out Decimal roundBytes)
        {
            Decimal pow = (Decimal)(Math.Pow(2, exponent));

            Decimal quotient = fileSize / pow;

            roundBytes = Math.Round(quotient, decimals, MidpointRounding.AwayFromZero);

            return (roundBytes >= 1);
        }

        public override String ToString()
            => (FileSizeFormatted);

        public String ToString(Int32 numberPadding)
            => (FormatFileSize(FileSizeInBytes, numberPadding));
    }
}