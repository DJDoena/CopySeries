namespace DoenaSoft.CopySeries.Json
{
    using System;
    using System.Collections.Generic;

    public class Properties
    {
        public Int32 container_type { get; set; }
        public String date_local { get; set; }
        public String date_utc { get; set; }
        public Int64 duration { get; set; }
        public Boolean is_providing_timecodes { get; set; }
        public String muxing_application { get; set; }
        public String segment_uid { get; set; }
        public String writing_application { get; set; }
    }

    public class Container
    {
        public Properties properties { get; set; }
        public Boolean recognized { get; set; }
        public Boolean supported { get; set; }
        public String type { get; set; }
    }

    public class TrackProperties
    {
        public String codec_id { get; set; }
        public String codec_private_data { get; set; }
        public Int32 codec_private_length { get; set; }
        public Int32 default_duration { get; set; }
        public Boolean default_track { get; set; }
        public String display_dimensions { get; set; }
        public Boolean enabled_track { get; set; }
        public Boolean forced_track { get; set; }
        public String language { get; set; }
        public Int64 minimum_timestamp { get; set; }
        public Int32 number { get; set; }
        public String packetizer { get; set; }
        public String pixel_dimensions { get; set; }
        public Decimal uid { get; set; }
        public Nullable<Int32> audio_channels { get; set; }
        public Nullable<Int32> audio_sampling_frequency { get; set; }
    }

    public class Track
    {
        public String codec { get; set; }
        public Int32 id { get; set; }
        public TrackProperties properties { get; set; }
        public String type { get; set; }
    }

    public class RootObject
    {
        public List<Object> attachments { get; set; }
        public List<Object> chapters { get; set; }
        public Container container { get; set; }
        public List<Object> errors { get; set; }
        public String file_name { get; set; }
        public List<Object> global_tags { get; set; }
        public Int32 identification_format_version { get; set; }
        public List<Object> track_tags { get; set; }
        public List<Track> tracks { get; set; }
        public List<Object> warnings { get; set; }
    }
}
