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
        [XmlText]
        public string Value;

        [XmlAttribute]
        public string Flags;
    }
}