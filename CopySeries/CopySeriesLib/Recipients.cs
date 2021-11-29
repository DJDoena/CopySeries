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
        public bool DayOfWeekSpecified;

        [XmlText]
        public string Value;

        [XmlAttribute]
        public bool NewSeries;

        [XmlIgnore]
        public bool NewSeriesSpecified;

        [XmlAttribute]
        public bool NewSeason;

        [XmlIgnore]
        public bool NewSeasonSpecified;
    }
}