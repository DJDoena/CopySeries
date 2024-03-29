﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DoenaSoft.MediaInfoHelper.Helpers;
using DoenaSoft.ToolBox.Extensions;

namespace DoenaSoft.CopySeries
{
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

        public string SeriesName => Name.LongName;

        private string SortName => Name.SortName;

        public string DisplayName => Name.DisplayName;

        public bool IsFirstOfSeason => IsDateShow == false && EpisodeNumber.StartsWith("01");

        public bool IsPilot => (IsDateShow == false) && (SeasonNumber == "1") && IsFirstOfSeason;

        public string OriginalLanguage => Name.OriginalLanguage;

        public string Link => Name.Link;

        private List<string> Audio { get; }

        private List<string> Subtitles { get; }

        public EpisodeData(Name name, string seasonNumber, string episodeNumber, bool isDateShow, string episodeName, string addInfo, FileSize fileSize)
        {
            Name = name;

            SeasonNumber = seasonNumber;

            EpisodeNumber = episodeNumber;

            IsDateShow = isDateShow;

            EpisodeID = GetEpisodeId(episodeName);

            EpisodeName = GetEpisodeName(episodeName);

            AddInfo = addInfo;

            FileSize = fileSize;

            Audio = new List<string>();

            Subtitles = new List<string>();
        }

        #region IComparable<EpisodeData>

        int IComparable<EpisodeData>.CompareTo(EpisodeData other)
        {
            if (other == null)
            {
                return 1;
            }

            var compare = SortName.CompareTo(other.SortName);

            if (compare == 0)
            {
                compare = SeasonNumber.PadLeft(2, '0').CompareTo(other.SeasonNumber.PadLeft(2, '0'));
            }

            if (compare == 0)
            {
                compare = EpisodeNumber.CompareTo(other.EpisodeNumber);
            }

            if (compare == 0)
            {
                compare = AddInfo.CompareTo(other.AddInfo);
            }

            return compare;
        }

        #endregion

        private string GetEpisodeName(string episodeName) => IsDateShow ? episodeName.Substring(11) : episodeName;

        private string GetEpisodeId(string episodeName) => IsDateShow ? GetDateShowId(episodeName) : GetSeasonShowId();

        private string GetDateShowId(string episodeName) => DateTime.Parse(episodeName.Substring(0, 10)).ToShortDateString();

        private string GetSeasonShowId() => $"{SeasonNumber}x{EpisodeNumber}";

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(DisplayName);
            sb.Append(" ");
            sb.Append(EpisodeID);
            sb.Append(" \"");
            sb.Append(EpisodeName);
            sb.Append("\"");
            sb.Append(" ");
            sb.Append(AddInfo);
            sb.Append(" (");
            sb.Append(FileSize.ToString());
            sb.Append(")");

            if (Audio.Count > 0)
            {
                sb.Append(": ");

                sb.Append(GetAudio());
            }

            return sb.ToString();
        }

        public void AddAudio(string language)
        {
            if (language.IsNotEmpty())
            {
                Audio.Add(language);
            }
        }

        public string GetAudio()
        {
            if (Audio.Count > 0)
            {
                var audio = Audio
                    .StandardizeLanguage()
                    .OrderBy(LanguageExtensions.GetLanguageWeight);

                return string.Join(", ", audio);
            }

            return string.Empty;
        }

        public void AddSubtitle(string language)
        {
            if (language.IsNotEmpty())
            {
                Subtitles.Add(language);
            }
        }

        public string GetSubtitles()
        {
            if (Subtitles.Count > 0)
            {
                var filtered = Subtitles
                    .StandardizeLanguage()
                    .Where(s => s == "en" || s == "de" || s == "es" || s == "ar")
                    .OrderBy(LanguageExtensions.GetLanguageWeight);

                return string.Join(", ", filtered);
            }

            return string.Empty;
        }
    }
}