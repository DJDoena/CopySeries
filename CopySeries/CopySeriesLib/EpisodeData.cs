using System;
using System.Collections.Generic;
using System.Text;

namespace DoenaSoft.CopySeries
{
    public sealed class EpisodeData : IComparable<EpisodeData>
    {
        private Name Name { get; }

        private String SeasonNumber { get; }

        private String EpisodeNumber { get; }

        private Boolean IsDateShow { get; }

        public String EpisodeID { get; }

        public String EpisodeName { get; }

        public String AddInfo { get; }

        public FileSize FileSize { get; }

        public String SeriesName
            => (Name.LongName);

        private String SortName
            => (Name.SortName);

        public String DisplayName
            => (Name.DisplayName);

        public Boolean IsFirstOfSeason
            => ((IsDateShow == false) && (EpisodeNumber.StartsWith("01")));

        public Boolean IsPilot
            => ((IsDateShow == false) && (SeasonNumber == "1") && (IsFirstOfSeason));

        private List<String> Languages { get; }

        public EpisodeData(Name name
            , String seasonNumber
            , String episodeNumber
            , Boolean isDateShow
            , String episodeName
            , String addInfo
            , FileSize fileSize)
        {
            Name = name;

            SeasonNumber = seasonNumber;

            EpisodeNumber = episodeNumber;

            IsDateShow = isDateShow;

            EpisodeID = GetEpisodeID();

            EpisodeName = episodeName;

            AddInfo = addInfo;

            FileSize = fileSize;

            Languages = new List<String>();
        }

        #region IComparable<EpisodeData>

        Int32 IComparable<EpisodeData>.CompareTo(EpisodeData other)
        {
            if (other == null)
            {
                return (1);
            }

            Int32 compare = SortName.CompareTo(other.SortName);

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

            return (compare);
        }

        #endregion

        private String GetEpisodeID()
            => (IsDateShow ? GetDateShowID() : GetSeasonShowID());

        private String GetDateShowID()
            => ($"{EpisodeNumber}.{SeasonNumber.PadLeft(2, '0')}.");

        private String GetSeasonShowID()
            => ($"{SeasonNumber}x{EpisodeNumber}");

        public override String ToString()
        {
            StringBuilder sb = new StringBuilder();

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

            if (Languages.Count > 0)
            {
                sb.Append(": ");

                sb.Append(String.Join(", ", Languages));
            }

            return (sb.ToString());
        }

        public void AddLanguage(String language)
        {
            Languages.Add(language);
        }
    }
}