namespace DoenaSoft.CopySeries.Xml
{
    using System;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [XmlRoot]
    public sealed class VideoInfo
    {
        [XmlElement]
        public Episode Episode;

        [XmlElement]
        public Video[] Video;

        [XmlElement]
        public Audio[] Audio;

        [XmlElement]
        public Subtitle[] Subtitle;

        [XmlAttribute]
        public UInt32 Duration { get; set; }

        [XmlIgnore]
        public Boolean DurationSpecified { get; set; }
    }

    [DebuggerDisplay("Series: {SeriesName}, Episode: {EpisodeName}")]
    public sealed class Episode
    {
        [XmlAttribute]
        public String SeriesName;

        [XmlAttribute]
        public String EpisodeNumber;

        [XmlAttribute]
        public String EpisodeName;

        public override string ToString()
            => ($"{SeriesName} {EpisodeNumber} {EpisodeName}");
    }

    public abstract class StreamBase
    {
        private String m_Language;

        [XmlAttribute]
        public String CodecName;

        [XmlAttribute]
        public String CodecLongName;

        [XmlAttribute]
        public String Language
        {
            get => m_Language == "und" ? null : m_Language;
            set => m_Language = value;
        }

        [XmlAttribute]
        public String Title;
    }

    [DebuggerDisplay("Codec: {CodecName}, Language: {Language}")]
    public sealed class Video : StreamBase
    {
        [XmlElement]
        public AspectRatio AspectRatio;
    }

    [DebuggerDisplay("{Width}:{Height}")]
    public sealed class AspectRatio
    {
        [XmlAttribute]
        public UInt16 Width;

        [XmlAttribute]
        public UInt16 Height;

        [XmlAttribute]
        public UInt16 CodedWidth;

        [XmlIgnore]
        public Boolean CodedWidthSpecified;

        [XmlAttribute]
        public UInt16 CodedHeight;

        [XmlIgnore]
        public Boolean CodedHeightSpecified;

        [XmlAttribute]
        public Decimal Ratio;

        [XmlIgnore]
        public Boolean RatioSpecified;
    }

    [DebuggerDisplay("Channels: {ChannelLayout}, Language: {Language}")]
    public sealed class Audio : StreamBase
    {
        [XmlAttribute]
        public UInt16 SampleRate;

        [XmlAttribute]
        public Byte Channels;

        [XmlAttribute]
        public String ChannelLayout;
    }

    [DebuggerDisplay("Language: {Language}")]
    public sealed class Subtitle : StreamBase
    { }
}