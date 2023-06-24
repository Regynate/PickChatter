using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PickChatter
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool HasModifiedFields()
        {
            return SettingsManager.Instance.HasModifiedProperties();
        }

        private void UpdateTwitchChannel()
        {
            if (SettingsManager.Instance.PropertyModified(nameof(SettingsManager.Instance.TwitchChannel)))
            {
                TwitchClient.Instance.UpdateChannel();
                ChatterPicker.Instance.ClearChatters();
            }
        }

        private void SaveConfig()
        {
            SettingsManager.Instance.Save();
        }

        private bool ShowUnsavedChangesPrompt()
        {
            return MessageBox.Show(this,
                "Are you sure you want to quit? This will reset all the unsaved changes",
                "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
        }

        private void ConnectToTwitchButton_Click(object sender, RoutedEventArgs e)
        {
            TwitchClient.Instance.StartBrowserAuth();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateTwitchChannel();
            SaveConfig();
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (HasModifiedFields() && !ShowUnsavedChangesPrompt())
            {
                e.Cancel = true;
            }
            else
            {
                SettingsManager.Instance.ResetProperties();
            }
        }

        private void TestVoiceButton_Click(object sender, RoutedEventArgs e)
        {
            SpeechManager.Instance.Speak("Hello, my name is " + SpeechManager.Instance.CurrentVoice);
        }
    }
}
