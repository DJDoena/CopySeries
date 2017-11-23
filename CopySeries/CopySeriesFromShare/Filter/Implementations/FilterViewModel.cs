namespace DoenaSoft.CopySeries.Filter.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Windows.Input;
    using CopySeries.Implementations;
    using ToolBox.Commands;
    using ToolBox.Extensions;

    internal sealed class FilterViewModel : IFilterViewModel
    {
        #region Fields

        private Boolean m_NoSubs;

        private Boolean m_OnlyHDs;

        private Boolean m_OnlySDs;

        private String m_Filter;

        #endregion

        #region Properties

        private IWindowFactory WindowFactory { get; }

        #endregion

        #region Constructor

        public FilterViewModel(IWindowFactory windowFactory)
        {
            WindowFactory = windowFactory;

            m_NoSubs = Properties.Settings.Default.NoSubs;
            m_OnlyHDs = Properties.Settings.Default.OnlyHD;
            m_OnlySDs = Properties.Settings.Default.OnlySD;

            Filter = Properties.Settings.Default.Filter.Replace(";", Environment.NewLine).Trim();
        }

        #endregion

        #region IFilterViewModel

        public Boolean NoSubs
        {
            get
            {
                return (m_NoSubs);
            }
            set
            {
                if (value != m_NoSubs)
                {
                    m_NoSubs = value;

                    RaisePropertyChanged(nameof(NoSubs));
                }
            }
        }

        public Boolean OnlyHDs
        {
            get
            {
                return (m_OnlyHDs);
            }
            set
            {
                if (value != m_OnlyHDs)
                {
                    m_OnlyHDs = value;

                    if (value)
                    {
                        OnlySDs = false;
                    }

                    RaisePropertyChanged(nameof(OnlyHDs));
                }
            }
        }

        public Boolean OnlySDs
        {
            get
            {
                return (m_OnlySDs);
            }
            set
            {
                if (value != m_OnlySDs)
                {
                    m_OnlySDs = value;

                    if (value)
                    {
                        OnlyHDs = false;
                    }

                    RaisePropertyChanged(nameof(OnlySDs));
                }
            }
        }

        public String Filter
        {
            get
            {
                return (m_Filter);
            }
            set
            {
                if (value != m_Filter)
                {
                    m_Filter = value;

                    RaisePropertyChanged(nameof(Filter));
                }
            }
        }

        public ICommand SelectFoldersCommand
            => (new RelayCommand(SelectFolders));

        public ICommand AcceptCommand
            => (new ParameterizedRelayCommand(Accept));

        public ICommand CancelCommand
            => (new ParameterizedRelayCommand(Cancel));

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Methods

        private void SelectFolders()
        {
            IEnumerable<String> selectedShows;
            if (WindowFactory.OpenSelectFoldersWindow(out selectedShows))
            {
                Filter = Transform(Environment.NewLine, selectedShows);
            }
        }

        private void Accept(Object parameter)
        {
            Properties.Settings.Default.NoSubs = NoSubs;
            Properties.Settings.Default.OnlyHD = OnlyHDs;
            Properties.Settings.Default.OnlySD = OnlySDs;

            Properties.Settings.Default.Filter = Transform(";");

            ICloseable closeable = (ICloseable)parameter;

            closeable.DialogResult = true;

            closeable.Close();
        }

        private void Cancel(Object parameter)
        {
            ICloseable closeable = (ICloseable)parameter;

            closeable.DialogResult = false;

            closeable.Close();
        }


        private void RaisePropertyChanged(String attribute)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(attribute));
        }

        private String Transform(String separator
            , IEnumerable<String> additionalShows = null)
        {
            IEnumerable<String> selectedShows = Split(Filter);

            if (additionalShows != null)
            {
                selectedShows = selectedShows.Union(additionalShows);
            }

            selectedShows = Clean(selectedShows);

            String filter = String.Join(separator, selectedShows.ToArray());

            return (filter);
        }

        private static String[] Split(String filter)
            => (filter.Split('\r', '\n'));

        private IEnumerable<String> Clean(IEnumerable<String> input)
        {
            IEnumerable<String> output = Enumerable.Empty<String>();

            if (input.HasItems())
            {
                input = input.Where(show => show.Trim() != String.Empty);

                HashSet<String> hashed = new HashSet<String>(input);

                List<String> sorted = new List<String>(hashed);

                sorted.Sort(SortHelper.CompareSeries);

                output = sorted;
            }

            return (output);
        }

        #endregion
    }
}