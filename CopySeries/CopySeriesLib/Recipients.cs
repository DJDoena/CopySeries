namespace DoenaSoft.CopySeries
{
    using System;
    using System.Diagnostics;
    using System.Xml.Serialization;

    [XmlRoot]
    public sealed class Recipients
    {
        [XmlArrayItem("Recipient")]
        public Recipient[] RecipientList;
    }

    [DebuggerDisplay("{Value}")]
    public sealed class Recipient
    {
        [XmlAttribute]
        public DayOfWeek DayOfWeek;

        [XmlAttribute]
        public Boolean DayOfWeekSpecified;

        [XmlText]
        public String Value;

        [XmlAttribute]
        public Boolean NewSeries;

        [XmlIgnore]
        public Boolean NewSeriesSpecified;

        [XmlAttribute]
        public Boolean NewSeason;

        [XmlIgnore]
        public Boolean NewSeasonSpecified;
    }
}