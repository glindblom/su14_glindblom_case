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
using System.Windows.Shapes;

namespace AzureStorageHandler
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {

        public struct Settings
        {
            public string AccountName { get; set; }
            public string AccountKey { get; set; }
            public string ContainerName { get; set; }
        }

        public Settings CloudSettings { get; set; }

        public SettingsWindow()
        {
            InitializeComponent();

            var appSettings = System.Configuration.ConfigurationManager.AppSettings;
            AccountName.Text = appSettings.Get("storageName");
            AccountKey.Text = appSettings.Get("storageKey");
            ContainerName.Text = appSettings.Get("containerName");
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            CloudSettings = new Settings() { AccountName = AccountName.Text, AccountKey = AccountKey.Text, ContainerName = ContainerName.Text };
            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
