using System;
using System.Globalization;

namespace DoenaSoft.CopySeries
{

    public sealed class FileSize
    {
        public ulong Bytes { get; private set; }

        public FileSize(ulong bytes)
        {
            this.Bytes = bytes;
        }

        private static string FormatFileSize(ulong bytes, int padding)
        {
            string unit;
            if (CheckOrderOfMagnitude(bytes, 40, 1, out var rounded))
            {
                unit = "TiB";
            }
            else if (CheckOrderOfMagnitude(bytes, 30, 1, out rounded))
            {
                unit = "GiB";
            }
            else if (CheckOrderOfMagnitude(bytes, 20, 0, out rounded))
            {
                unit = "MiB";
            }
            else if (CheckOrderOfMagnitude(bytes, 10, 0, out rounded))
            {
                unit = "KiB";
            }
            else
            {
                rounded = bytes;

                unit = "Byte";
            }

            var formatted = $"{rounded.ToString(CultureInfo.GetCultureInfo("de-DE")).PadLeft(padding)} {unit}";

            return formatted;
        }

        private static bool CheckOrderOfMagnitude(decimal number, double exponent, int decimals, out decimal rounded)
        {
            var pow = (decimal)Math.Pow(2, exponent);

            var quotient = number / pow;

            rounded = Math.Round(quotient, decimals, MidpointRounding.AwayFromZero);

            return rounded >= 1;
        }

        public override string ToString()
            => this.ToString(0);

        public string ToString(int padding)
            => FormatFileSize(this.Bytes, padding);
    }
}