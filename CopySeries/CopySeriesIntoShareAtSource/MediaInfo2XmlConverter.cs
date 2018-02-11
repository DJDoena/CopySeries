namespace DoenaSoft.CopySeries
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using ToolBox.Extensions;

    internal static class MediaInfo2XmlConverter
    {
        static readonly CultureInfo CultureInfo;

        static MediaInfo2XmlConverter()
        {
            CultureInfo = CultureInfo.GetCultureInfo("en-US");
        }

        internal static String GetLanguage(this IEnumerable<Xml.Tag> tags)
            => tags?.FirstOrDefault(tag => tag.key == "language")?.value;

        internal static Xml.VideoInfo Convert(Xml.FFProbe ffprobe
            , String fileName)
        {
            Xml.VideoInfo info = new Xml.VideoInfo()
            {
                Duration = ConvertDuration(ffprobe.format?.duration),
                Video = ffprobe.streams?.Where(IsVideo).Select(GetXmlVideo).ToArray(),
                Audio = ffprobe.streams?.Where(IsAudio).Select(GetXmlAudio).ToArray(),
                Subtitle = ffprobe.streams?.Where(IsSubtile).Select(GetXmlSubtitle).ToArray(),
            };

            info.DurationSpecified = info.Duration > 0;

            if (info.Video?.Length == 0)
            {
                info.Video = null;
            }

            if (info.Audio?.Length == 0)
            {
                info.Audio = null;
            }

            if (info.Subtitle?.Length == 0)
            {
                info.Subtitle = null;
            }

            return (info);
        }

        private static Boolean IsSubtile(Xml.Stream stream)
            => stream?.codec_type == "subtitle";

        private static Xml.Subtitle GetXmlSubtitle(Xml.Stream stream)
            => new Xml.Subtitle()
            {
                CodecName = stream.codec_name,
                CodecLongName = stream.codec_long_name,
                Language = stream.tag.GetLanguage(),
                Title = stream.tag.GetTitle()
            };

        private static Boolean IsAudio(Xml.Stream stream)
            => stream?.codec_type == "audio";

        private static Xml.Audio GetXmlAudio(Xml.Stream stream)
            => new Xml.Audio()
            {
                CodecName = stream.codec_name,
                CodecLongName = stream.codec_long_name,
                SampleRate = stream.sample_rate,
                Channels = stream.channels,
                ChannelLayout = stream.channel_layout,
                Language = stream.tag.GetLanguage(),
                Title = stream.tag.GetTitle(),
            };

        private static Boolean IsVideo(Xml.Stream stream)
            => stream?.codec_type == "video";

        private static Xml.Video GetXmlVideo(Xml.Stream stream)
            => new Xml.Video()
            {
                CodecName = stream.codec_name,
                CodecLongName = stream.codec_long_name,
                AspectRatio = GetAspectRatio(stream),
                Language = stream.tag.GetLanguage(),
                Title = stream.tag.GetTitle(),
            };

        private static Xml.AspectRatio GetAspectRatio(Xml.Stream stream)
        {
            Xml.AspectRatio ratio = new Xml.AspectRatio
            {
                Width = stream.width,
                Height = stream.height,
                CodedWidth = stream.coded_width,
                CodedHeight = stream.coded_height,
                Ratio = GetAspectRatio(stream.display_aspect_ratio),
            };

            ratio.CodedWidthSpecified = ratio.CodedWidth != 0 && ratio.Width != ratio.CodedWidth;
            ratio.CodedHeightSpecified = ratio.CodedHeight != 0 && ratio.Height != ratio.CodedHeight;

            if ((ratio.Ratio == 0) && (ratio.Height > 0))
            {
                ratio.Ratio = CalulateRatio(ratio.Width, ratio.Height);
            }

            ratio.RatioSpecified = ratio.Ratio > 0;

            return (ratio);
        }

        private static UInt32 ConvertDuration(String duration)
        {
            if (duration.IsEmpty())
            {
                return (0);
            }
            else if (TryParseDouble(duration, out Double seconds))
            {
                return ((UInt32)(seconds));
            }
            else if (TryParseTimeSpan(duration, out TimeSpan timeSpan))
            {
                return ((UInt32)(timeSpan.TotalSeconds));
            }

            return (0);
        }

        private static Decimal GetAspectRatio(String aspectRatio)
        {
            if (aspectRatio.IsEmpty())
            {
                return (0);
            }

            String[] split = aspectRatio.Split(':');

            if (split.Length != 2)
            {
                return (0);
            }
            else if ((UInt32.TryParse(split[0], out UInt32 width)) && (UInt32.TryParse(split[1], out UInt32 height)) && (height > 0))
            {
                Decimal ratio = CalulateRatio(width, height);

                return (ratio);
            }

            return (0);
        }

        private static Decimal CalulateRatio(Decimal width, Decimal height)
            => Math.Round(width / height, 2, MidpointRounding.AwayFromZero);

        private static Boolean TryParseTimeSpan(String duration, out TimeSpan timeSpan)
        {
            Int32 indexOf = duration.IndexOf('.');

            String reduced = indexOf > 0 ? duration.Substring(0, indexOf) : duration;

            Boolean success = TimeSpan.TryParse(reduced, CultureInfo, out timeSpan);

            return (success);
        }

        private static Boolean TryParseDouble(String duration, out Double seconds)
            => Double.TryParse(duration, NumberStyles.AllowDecimalPoint, CultureInfo, out seconds);

        private static String GetTitle(this IEnumerable<Xml.Tag> tags)
            => tags?.FirstOrDefault(tag => tag.key == "title")?.value;
    }
}