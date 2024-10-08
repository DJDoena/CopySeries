﻿using System.Text;
using DoenaSoft.MediaInfoHelper.Helpers;

namespace DoenaSoft.CopySeries;

public sealed class EpisodeData : IComparable<EpisodeData>
{
    private Name Name { get; }

    private string SeasonNumber { get; }

    private string EpisodeNumber { get; }

    private bool IsDateShow { get; }

    public string EpisodeID { get; }

    public string EpisodeName { get; }

    public string AddInfo { get; }

    public FileSize FileSize { get; }

    public string SeriesName => this.Name.LongName;

    private string SortName => this.Name.SortName;

    public string DisplayName => this.Name.DisplayName;

    public bool IsFirstOfSeason => this.IsDateShow == false && this.EpisodeNumber.StartsWith("01");

    public bool IsPilot => (this.IsDateShow == false) && (this.SeasonNumber == "1") && this.IsFirstOfSeason;

    public string OriginalLanguage => this.Name.OriginalLanguage;

    public string Link => this.Name.Link;

    private List<string> Audio { get; }

    private List<string> Subtitles { get; }

    public EpisodeData(Name name, string seasonNumber, string episodeNumber, bool isDateShow, string episodeName, string addInfo, FileSize fileSize)
    {
        this.Name = name;

        this.SeasonNumber = seasonNumber;

        this.EpisodeNumber = episodeNumber;

        this.IsDateShow = isDateShow;

        this.EpisodeID = this.GetEpisodeId(episodeName);

        this.EpisodeName = this.GetEpisodeName(episodeName);

        this.AddInfo = addInfo;

        this.FileSize = fileSize;

        this.Audio = new List<string>();

        this.Subtitles = new List<string>();
    }

    #region IComparable<EpisodeData>

    int IComparable<EpisodeData>.CompareTo(EpisodeData other)
    {
        if (other == null)
        {
            return 1;
        }

        var compare = this.SortName.CompareTo(other.SortName);

        if (compare == 0)
        {
            compare = this.SeasonNumber.PadLeft(2, '0').CompareTo(other.SeasonNumber.PadLeft(2, '0'));
        }

        if (compare == 0)
        {
            compare = this.EpisodeNumber.CompareTo(other.EpisodeNumber);
        }

        if (compare == 0)
        {
            compare = this.AddInfo.CompareTo(other.AddInfo);
        }

        return compare;
    }

    #endregion

    private string GetEpisodeName(string episodeName) => this.IsDateShow ? episodeName.Substring(11) : episodeName;

    private string GetEpisodeId(string episodeName) => this.IsDateShow ? this.GetDateShowId(episodeName) : this.GetSeasonShowId();

    private string GetDateShowId(string episodeName) => DateTime.Parse(episodeName.Substring(0, 10)).ToShortDateString();

    private string GetSeasonShowId() => $"{this.SeasonNumber}x{this.EpisodeNumber}";

    public override string ToString()
    {
        var sb = new StringBuilder();

        sb.Append(this.DisplayName);
        sb.Append(" ");
        sb.Append(this.EpisodeID);
        sb.Append(" \"");
        sb.Append(this.EpisodeName);
        sb.Append("\"");
        sb.Append(" ");
        sb.Append(this.AddInfo);
        sb.Append(" (");
        sb.Append(this.FileSize.ToString());
        sb.Append(")");

        if (this.Audio.Count > 0)
        {
            sb.Append(": ");

            sb.Append(this.GetAudio());
        }

        return sb.ToString();
    }

    public void AddAudio(string language)
    {
        if (!string.IsNullOrWhiteSpace(language))
        {
            this.Audio.Add(language);
        }
    }

    public string GetAudio()
    {
        if (this.Audio.Count > 0)
        {
            var audio = this.Audio
                .StandardizeLanguage()
                .OrderBy(LanguageExtensions.GetLanguageWeight);

            return string.Join(", ", audio);
        }

        return string.Empty;
    }

    public void AddSubtitle(string language)
    {
        if (!string.IsNullOrWhiteSpace(language))
        {
            this.Subtitles.Add(language);
        }
    }

    public string GetSubtitles()
    {
        if (this.Subtitles.Count > 0)
        {
            var filtered = this.Subtitles
                .StandardizeLanguage()
                .Where(s => s == "en" || s == "de" || s == "es" || s == "ar")
                .OrderBy(LanguageExtensions.GetLanguageWeight);

            return string.Join(", ", filtered);
        }

        return string.Empty;
    }
}