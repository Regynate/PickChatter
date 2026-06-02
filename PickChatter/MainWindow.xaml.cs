using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PickChatter;
using TwitchLib.Communication.Interfaces;

namespace PickChatter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private class PickerTab
        {
            public ChatterPicker picker;
            public TabItem tab;
            public int index;

            public PickerTab(ChatterPicker picker, TabItem tab, int index)
            {
                this.picker = picker;
                this.tab = tab;
                this.index = index;
            }
        }

        private readonly List<PickerTab> _pickerList;
        private int CurrentTabIndex => ChattersTabContainer.SelectedIndex;

        public ChatterPicker? CurrentPicker => CurrentTabIndex < _pickerList.Count ? _pickerList[CurrentTabIndex].picker : null;

        public MainWindow()
        {
            InitializeComponent();
            _pickerList = new();
            UpdatePickerList();

            _tabAddNewTab = new TabItem()
            {
                Header = "+",
                Content = null,
                IsEnabled = true
            };

            ChattersTabContainer.SelectionChanged += ChattersTab_SelectionChanged;

            UpdateTabs();

            DataContext = this;
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new()
            {
                Owner = this,
                ShowInTaskbar = false
            };

            window.ShowDialog();
        }

        private TabItem _tabAddNewTab;

        private void AddPicker()
        {
            ChatterPickerList.Instance.AddChatterPicker();
            UpdatePickerList();
            UpdateTabs();
            ChattersTabContainer.SelectedIndex = _pickerList.Count - 1;
        }

        private void ChattersTab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Contains(_tabAddNewTab))
            {
                AddPicker();
            }
        }

        private void UpdatePickerList()
        {
            _pickerList.Clear();
        }

        private string GetHeader(ChatterPicker picker)
        {
            return (picker.SpeechManager.SpeechSpeaking ? "🗣" : "") +
                (picker.IsDefaultID ? $"Chatter {picker.Index}" : picker.ID)
                + (string.IsNullOrEmpty(picker.ChatterName) ? "" : $" - {picker.ChatterName}");
        }

        private void UpdateHeader(PickerTab p)
        {
            p.tab.Header = GetHeader(p.picker);
        }

        private void UpdateTabs()
        {
            ChattersTabContainer.Items.Clear();

            int i = 1;
            foreach (var picker in ChatterPickerList.Instance.ChatterPickers)
            {
                var page = new ChatterPage(picker);
                var tab = new TabItem()
                {
                    Header = GetHeader(picker),
                    Content = new Frame()
                    {
                        Content = page,
                        NavigationUIVisibility = NavigationUIVisibility.Hidden
                    }
                };
                ChattersTabContainer.Items.Add(tab);

                _pickerList.Add(new PickerTab(picker, tab, i));

                picker.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(ChatterPicker.ID))
                    {
                        var p = _pickerList.FirstOrDefault(x => x.picker == picker);

                        if (p != null)
                        {
                            UpdateHeader(p);
                            SettingsManager.Instance.SetChatterID(p.index - 1, p.picker.IsDefaultID ? "" : p.picker.ID);
                        }
                    }

                    if (args.PropertyName == nameof(ChatterPicker.ChatterName))
                    {
                        var p = _pickerList.FirstOrDefault(x => x.picker == picker);
                        UpdateHeader(p);
                    }
                };
                picker.SpeechManager.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(SpeechManager.SpeechSpeaking))
                    {
                        var p = _pickerList.FirstOrDefault(x => x.picker == picker);

                        if (p != null)
                        {
                            UpdateHeader(p);
                        }
                    }
                };

                ++i;
            }

            ChattersTabContainer.Items.Add(_tabAddNewTab);
        }

        private void RemoveChatterButton_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPicker != null)
            {
                int index = CurrentTabIndex;

                ChatterPickerList.Instance.RemovePicker(CurrentPicker);
                UpdatePickerList();
                UpdateTabs();
                ChattersTabContainer.SelectedIndex = index < _pickerList.Count ? index : _pickerList.Count - 1;
            }

            if (_pickerList.Count == 0)
            {
                AddPicker();
            }
        }
    }
}
