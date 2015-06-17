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
    /// Interaction logic for StreamingWindow.xaml
    /// </summary>
    public partial class StreamingWindow : Window
    {
        public StreamingWindow(string url)
        {
            InitializeComponent();

            Streamer.Source = new Uri(url.Replace("https", "http"));
            Streamer.LoadedBehavior = MediaState.Manual;
            Streamer.UnloadedBehavior = MediaState.Manual;
            Streamer.Play();
        }
    }
}
