namespace DoenaSoft.CopySeries.Xml
{
    using System.Diagnostics;
    using System.Xml;
    using System.Xml.Serialization;

    [XmlRoot("doc")]
    public sealed class Doc
    {
        [XmlElement]
        public VideoInfo VideoInfo;

        [XmlAnyElement]
        public XmlElement[] Any;
    }

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
        public uint Duration { get; set; }

        [XmlIgnore]
        public bool DurationSpecified { get; set; }
    }

    [DebuggerDisplay("Series: {SeriesName}, Episode: {EpisodeName}")]
    public sealed class Episode
    {
        [XmlAttribute]
        public string SeriesName;

        [XmlAttribute]
        public string EpisodeNumber;

        [XmlAttribute]
        public string EpisodeName;

        public override string ToString()
            => ($"{SeriesName} {EpisodeNumber} {EpisodeName}");
    }

    public abstract class StreamBase
    {
        private string m_Language;

        [XmlAttribute]
        public string CodecName;

        [XmlAttribute]
        public string CodecLongName;

        [XmlAttribute]
        public string Language
        {
            get => (m_Language == "und" || m_Language == "null") ? null : m_Language;
            set => m_Language = value;
        }

        [XmlAttribute]
        public string Title;
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
        public ushort Width;

        [XmlAttribute]
        public ushort Height;

        [XmlAttribute]
        public ushort CodedWidth;

        [XmlIgnore]
        public bool CodedWidthSpecified;

        [XmlAttribute]
        public ushort CodedHeight;

        [XmlIgnore]
        public bool CodedHeightSpecified;

        [XmlAttribute]
        public decimal Ratio;

        [XmlIgnore]
        public bool RatioSpecified;
    }

    [DebuggerDisplay("Channels: {ChannelLayout}, Language: {Language}")]
    public sealed class Audio : StreamBase
    {
        [XmlAttribute]
        public ushort SampleRate;

        [XmlAttribute]
        public byte Channels;

        [XmlAttribute]
        public string ChannelLayout;
    }

    [DebuggerDisplay("Language: {Language}")]
    public sealed class Subtitle : StreamBase
    { }
}