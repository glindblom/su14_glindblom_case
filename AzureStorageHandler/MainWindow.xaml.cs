using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.Win32;
using Microsoft.WindowsAzure.Storage.Blob;
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
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Configuration;
using System.Runtime.Remoting.Messaging;

namespace AzureStorageHandler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private CloudStorageAccount _account;
        private CloudBlobClient _blobClient;
        private CloudBlobContainer _root;

        public delegate void AddFileTransferDelegate(ICloudBlob b);
        public delegate void FinishedLoadingFileTransferDelegate();
        private delegate bool ConnectionResultDelegate();

        private bool _loadingFileTransfer = false;

        private List<TransferViewItem> _activeTransfers = new List<TransferViewItem>();

        public MainWindow()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 35;
            InitializeComponent();
        }

        public override void EndInit()
        {
            base.EndInit();
            Connect();
        }

        public async void Connect()
        {
            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                string accountName = appSettings.Get("storageName");
                string accountKey = appSettings.Get("storageKey");
                var sc = new StorageCredentials(accountName, accountKey);
                _account = new CloudStorageAccount(sc, true);
                var retry = new Microsoft.WindowsAzure.Storage.RetryPolicies.LinearRetry(TimeSpan.FromSeconds(2), 3);
                _blobClient = _account.CreateCloudBlobClient();
                _blobClient.DefaultRequestOptions = new BlobRequestOptions();
                _blobClient.DefaultRequestOptions.RetryPolicy = retry;
                _root = _blobClient.GetContainerReference(appSettings.Get("containerName"));
                await _root.CreateIfNotExistsAsync();
                StatusLight.Background = Brushes.Green;
                StatusText.Content = "Status: Connected";
                GetFileTransferAsync();

                ConnectionMenuItem.IsEnabled = false;
                ConnectionMenuItem.Header = "Connected";
            }
            catch (Exception e)
            {
                StatusLight.Background = Brushes.Red;
                StatusText.Content = "Status: Connection failed. Check connectivity and try again. If connected, check your Storage Credentials under the menu Application/Settings";
                ConnectionMenuItem.IsEnabled = true;
                ConnectionMenuItem.Header = "Connect";
            }
        }

        private void AddFileTransfer(ICloudBlob blob)
        {
            FileTransfer file = new FileTransfer();
            file.Blob = blob;
            file.Container = System.IO.Path.GetFileName(blob.Parent.Uri.AbsolutePath);
            file.FileName = System.IO.Path.GetFileName(blob.Uri.AbsolutePath);


            object item = new
            {
                Tag = file,
                Container = file.Container,
                FileName = file.FileName,
                FileSize = (blob.Properties.Length / 1024).ToString("N0"),
                FileUpload = DateTime.Parse(file.Blob.Properties.LastModified.ToString())
            };
            int index = FileList.Items.Add(item);
            file.Item = FileList.Items[index];
        }

        private void GetFileTransferAsync()
        {
            if (_loadingFileTransfer) return;
            else _loadingFileTransfer = true;

            StatusLight.Background = Brushes.Yellow;
            StatusText.Content = "Status: Fetching files...";

            FileList.Items.Clear();
            BlobContinuationToken continuation = null;
            BlobRequestOptions options = new BlobRequestOptions();

            _root.BeginListBlobsSegmented("", true, new BlobListingDetails(), 5, continuation, options, new OperationContext(), new AsyncCallback(GetFilesCallback), null);
        }

        private void GetFilesCallback(IAsyncResult ar)
        {
            BlobResultSegment list = _root.EndListBlobsSegmented(ar);

            foreach (ICloudBlob b in list.Results)
            {
                this.Dispatcher.Invoke(new AddFileTransferDelegate(this.AddFileTransfer), new object[] { b });   
            }

            if (list.ContinuationToken != null)
            {
                BlobRequestOptions options = new BlobRequestOptions();
                _root.BeginListBlobsSegmented(list.ContinuationToken, new AsyncCallback(GetFilesCallback), null);
            }
            else
            {
                this.Dispatcher.Invoke(new FinishedLoadingFileTransferDelegate(FinishedLoadingFileTransfer));
            }
        }

        private void FinishedLoadingFileTransfer()
        {
            _loadingFileTransfer = false;
            StatusLight.Background = Brushes.Green;
            StatusText.Content = "Status: Files fetched";
        }

        private void DownloadFileTransfer(FileTransfer file, string folder)
        {
            TransferViewItem tfi = new TransferViewItem();
            tfi.LocalFile = System.IO.Path.Combine(folder, System.IO.Path.GetFileName(file.Blob.Uri.AbsolutePath));
            tfi.FileByteSize = file.Blob.Properties.Length;
            tfi.TransferCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(transfer_DownloadComplete);
            tfi.Tag = file.Item;
            tfi.Blob = file.Blob;
            try
            {
                tfi.Download();
            } catch (Exception e)
            {
                StatusLight.Background = Brushes.Red;
                StatusText.Content = "Error: " + e.Message;
                return;
            }
            StatusLight.Background = Brushes.Green;
            StatusText.Content = "Status: Transfers active...";
            
            Activity.Items.Add(tfi);
            Activity.ScrollIntoView(tfi);
            _activeTransfers.Add(tfi);
        }

        private void UploadFileTransfer(string file)
        {
            string filename = System.IO.Path.GetFileName(file);
            ICloudBlob blob = _root.GetBlockBlobReference(filename);

            TransferViewItem tfi = new TransferViewItem();
            tfi.Blob = blob;
            tfi.URL = blob.Uri.AbsolutePath;
            tfi.LocalFile = file;
            tfi.FileByteSize = new System.IO.FileInfo(file).Length;
            tfi.TransferCompleted += new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(transfer_UploadComplete);
            try
            {
                tfi.Upload();
            } catch (Exception e)
            {
                StatusLight.Background = Brushes.Red;
                StatusText.Content = "Error: " + e.Message;
                return;
            }
            StatusLight.Background = Brushes.Green;
            StatusText.Content = "Status: Transfers active...";

            Activity.Items.Add(tfi);
            Activity.ScrollIntoView(tfi);
            _activeTransfers.Add(tfi);
        }

        private void transfer_DownloadComplete(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            _activeTransfers.Remove(sender as TransferViewItem);
            if (_activeTransfers.Count == 0)
            {
                StatusLight.Background = Brushes.Green;
                StatusText.Content = "Status: Idle";
            }
        }

        private void transfer_UploadComplete(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            _activeTransfers.Remove(sender as TransferViewItem);

            if (e.Error != null)
            {
            }
            else if (e.Cancelled)
            {

            }
            else
            {
                GetFileTransferAsync();
            }
        }

        private void FileList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            dynamic item = (sender as ListView).SelectedItem;

            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.DefaultDirectory = Environment.GetEnvironmentVariable("userprofile");
            dlg.Title = "Choose Folder to Download to";
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                DownloadFileTransfer(item.Tag as FileTransfer, System.IO.Path.GetFullPath(dlg.FileName));
            }
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.DefaultDirectory = Environment.GetEnvironmentVariable("userprofile");
            dlg.Title = "Choose File to Upload";
            dlg.IsFolderPicker = false;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                UploadFileTransfer(dlg.FileName);
            }
        }

        private void UploadFolder_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.DefaultDirectory = Environment.GetEnvironmentVariable("userprofile");
            dlg.Title = "Choose Folder to Upload";
            dlg.IsFolderPicker = true;
            dlg.Multiselect = false;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                var folder = new System.IO.DirectoryInfo(dlg.FileName);
                string prefix = folder.Name;
                var files = folder.GetFiles();
                foreach (var fileInfo in files)
                {
                    UploadFileTransfer(fileInfo.FullName);
                }
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FileList.SelectedItem != null)
            {
                Remove.IsEnabled = true;
                Download.IsEnabled = true;
            }
            else
            {
                Remove.IsEnabled = false;
                Download.IsEnabled = false;
            }
        }

        private async void Remove_Click(object sender, RoutedEventArgs e)
        {
            foreach (dynamic item in FileList.SelectedItems)
            {
                ICloudBlob blob = _root.GetBlockBlobReference(item.FileName);
                if (await blob.DeleteIfExistsAsync())
                {
                    GetFileTransferAsync();
                }
            }
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dlg = new CommonOpenFileDialog();
            dlg.DefaultDirectory = Environment.GetEnvironmentVariable("userprofile");
            dlg.Title = "Choose Folder to Download to";
            dlg.IsFolderPicker = true;
            if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
            {
                foreach (dynamic item in FileList.SelectedItems)
                {
                    DownloadFileTransfer(item.Tag as FileTransfer, System.IO.Path.GetFullPath(dlg.FileName));
                }
            }
        }

        // Make sure we cancel any active transfer before closing the application
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            foreach (var filetransfer in _activeTransfers)
            {
                filetransfer.Cancel();
            }
        }

        private void ConnectionMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ConnectionMenuItem.IsEnabled = false;
            ConnectionMenuItem.Header = "Connecting";
            StatusLight.Background = Brushes.Yellow;
            StatusText.Content = "Status: Connecting...";
            Connect();
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow window = new SettingsWindow();

            if (window.ShowDialog() == true)
            {
                var appSettings = ConfigurationManager.AppSettings;
                appSettings.Set("storageName", window.CloudSettings.AccountName);
                appSettings.Set("storageKey", window.CloudSettings.AccountKey);
                appSettings.Set("containerName", window.CloudSettings.ContainerName);

                Connect();
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


    }

    public class BaseItem
    {
        private ICloudBlob _blob;
        private object _item;

        public ICloudBlob Blob
        {
            get { return _blob; }
            set { _blob = value; }
        }

        public object Item
        {
            get { return _item; }
            set { _item = value; }
        }
    }

    public class FileTransfer : BaseItem
    {
        private string _fileName;
        private string _container;

        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; }
        }

        public string Container
        {
            get { return _container; }
            set { _container = value; }
        }
    }
}
