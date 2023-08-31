using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using DoenaSoft.AbstractionLayer.Commands;
using DoenaSoft.CopySeries.Implementations;
using DoenaSoft.ToolBox.Extensions;

namespace DoenaSoft.CopySeries.Filter.Implementations
{
    internal sealed class FilterViewModel : IFilterViewModel
    {
        #region Fields

        private bool m_NoSubs;

        private bool m_OnlyHDs;

        private bool m_OnlySDs;

        private string m_Filter;

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

        public bool NoSubs
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

        public bool OnlyHDs
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

        public bool OnlySDs
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

        public string Filter
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
            IEnumerable<string> selectedShows;
            if (WindowFactory.OpenSelectFoldersWindow(out selectedShows))
            {
                Filter = Transform(Environment.NewLine, selectedShows);
            }
        }

        private void Accept(object parameter)
        {
            Properties.Settings.Default.NoSubs = NoSubs;
            Properties.Settings.Default.OnlyHD = OnlyHDs;
            Properties.Settings.Default.OnlySD = OnlySDs;

            Properties.Settings.Default.Filter = Transform(";");

            var closeable = (ICloseable)parameter;

            closeable.DialogResult = true;

            closeable.Close();
        }

        private void Cancel(object parameter)
        {
            var closeable = (ICloseable)parameter;

            closeable.DialogResult = false;

            closeable.Close();
        }


        private void RaisePropertyChanged(string attribute)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(attribute));
        }

        private string Transform(string separator
            , IEnumerable<string> additionalShows = null)
        {
            IEnumerable<string> selectedShows = Split(Filter);

            if (additionalShows != null)
            {
                selectedShows = selectedShows.Union(additionalShows);
            }

            selectedShows = Clean(selectedShows);

            var filter = string.Join(separator, selectedShows.ToArray());

            return (filter);
        }

        private static string[] Split(string filter)
            => (filter.Split('\r', '\n'));

        private IEnumerable<string> Clean(IEnumerable<string> input)
        {
            var output = Enumerable.Empty<string>();

            if (input.HasItems())
            {
                input = input.Where(show => show.Trim() != string.Empty);

                var hashed = new HashSet<string>(input);

                var sorted = new List<string>(hashed);

                sorted.Sort(SortHelper.CompareSeries);

                output = sorted;
            }

            return (output);
        }

        #endregion
    }
}