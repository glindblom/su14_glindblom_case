using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AzureStorageHandler
{
    public class TransferViewItem : ListViewItem
    {

        // Async event raised when a transfer is finished. Virtual void to ensure no exceptions are thrown if nothing has subscribed to the event
        public event EventHandler<AsyncCompletedEventArgs> TransferCompleted;
        protected virtual void OnTransferCompleted(AsyncCompletedEventArgs e) { if (TransferCompleted != null) TransferCompleted(this, e); }

        // Private view properties
        private ProgressBar _progressBar;
        private Label _progressLbl;
        private Label _remainingLbl;
        private Label _speedLbl;
        private Label _fromText;
        private Label _toText;
        private Button _cancelBtn;

        // Private Control properties
        private string _localFile;
        private string _URL;
        private long _fileByteSize;
        private ICloudBlob _blob;
        BlobTransfer _blobTransfer = new BlobTransfer();
        DateTime _startTime;


        // Public Properties
        public string LocalFile
        {
            get { return _localFile; }
            set { _localFile = value; }
        }

        public string URL
        {
            get { return _blob.Uri.ToString(); }
            set { _URL = value; }
        }

        public long FileByteSize
        {
            get { return _fileByteSize; }
            set { _fileByteSize = value; }
        }

        public ICloudBlob Blob
        {
            get { return _blob; }
            set { _blob = value; }
        }

        public TransferViewItem() : base()
        {
            Grid root = new Grid();
            root.ClipToBounds = true;
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition());
            root.RowDefinitions.Add(new RowDefinition());
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.2, GridUnitType.Star), MaxWidth = 350 });
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.1, GridUnitType.Star) });
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.3, GridUnitType.Star), MinWidth = 250 });
            root.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0.2, GridUnitType.Star) });

            Label fromLbl = new Label() { Content = "From: " };
            Label toLbl = new Label() { Content = "To: " };
            Grid.SetColumn(fromLbl, 0);
            Grid.SetRow(fromLbl, 0);
            Grid.SetColumn(toLbl, 0);
            Grid.SetRow(toLbl, 1);
            root.Children.Add(fromLbl);
            root.Children.Add(toLbl);

            _fromText = new Label();
            _toText = new Label();
            Grid.SetColumn(_fromText, 1);
            Grid.SetRow(_fromText, 0);
            Grid.SetColumn(_toText, 1);
            Grid.SetRow(_toText, 1);
            root.Children.Add(_fromText);
            root.Children.Add(_toText);

            Label pLabel = new Label() { Content = "Progress: " };
            Label tLabel = new Label() { Content = "Time Remaining" };
            Grid.SetColumn(pLabel, 2);
            Grid.SetRow(pLabel, 0);
            Grid.SetColumn(tLabel, 2);
            Grid.SetRow(tLabel, 1);
            root.Children.Add(pLabel);
            root.Children.Add(tLabel);

            _progressLbl = new Label() { Content = 0 };
            _remainingLbl = new Label() { Content = 0 };
            Grid.SetColumn(_progressLbl, 3);
            Grid.SetRow(_progressLbl, 0);
            Grid.SetColumn(_remainingLbl, 3);
            Grid.SetRow(_remainingLbl, 1);
            root.Children.Add(_progressLbl);
            root.Children.Add(_remainingLbl);

            _progressBar = new ProgressBar();
            Label sLabel = new Label() { Content = "Speed: " };
            _speedLbl = new Label() { Content = 0 };
            Grid.SetColumnSpan(_progressBar, 2);
            Grid.SetColumn(_progressBar, 4);
            Grid.SetRow(_progressBar, 0);
            Grid.SetColumn(sLabel, 4);
            Grid.SetRow(sLabel, 1);
            Grid.SetColumn(_speedLbl, 5);
            Grid.SetRow(_speedLbl, 1);
            root.Children.Add(_progressBar);
            root.Children.Add(sLabel);
            root.Children.Add(_speedLbl);

            _cancelBtn = new Button() { Content = "Cancel" };
            _cancelBtn.Click += _cancelBtn_Click;
            Grid.SetColumnSpan(_cancelBtn, 2);
            Grid.SetColumn(_cancelBtn, 0);
            Grid.SetRow(_cancelBtn, 2);
            root.Children.Add(_cancelBtn);

            AddChild(root);

            _blobTransfer.TransferCompleted += _blobTransfer_TransferCompleted;
            _blobTransfer.TransferProgressChanged += _blobTransfer_TransferProgressChanged;
        }

        void _blobTransfer_TransferCompleted(object sender, AsyncCompletedEventArgs e)
        {
            DateTime endTime = System.DateTime.Now;

            if (e.Cancelled)
            {
                _progressBar.Value = 0;
                _speedLbl.Content = "0";
                _cancelBtn.Content = "Cancelled";
                _cancelBtn.IsEnabled = false;
            }
            else if (e.Error != null)
            {
                _cancelBtn.Content = "Error";
                _cancelBtn.IsEnabled = false;
            }
            else
            {
                _speedLbl.Content = (((FileByteSize) / 1024 / (endTime - _startTime).TotalSeconds)).ToString("N0") + " KB/s";
                _cancelBtn.Content = "Done";
                _cancelBtn.IsEnabled = false;
            }

            AsyncCompletedEventArgs args = new AsyncCompletedEventArgs(e.Error, e.Cancelled, e.UserState);
            OnTransferCompleted(args);
        }

        void _blobTransfer_TransferProgressChanged(object sender, BlobTransfer.BlobTransferProgressChangedEventArgs e)
        {
            _speedLbl.Content = (e.Speed / 1024).ToString("N0") + " KB/s";
            _remainingLbl.Content = e.TimeRemaining.ToString();
            if (e.ProgressPercentage <= 100) _progressBar.Value = e.ProgressPercentage;
            else _progressBar.Value = 100;
            _progressLbl.Content = (e.BytesSent / 1024).ToString("N0") + " / " + (e.TotalBytesToSend / 1024).ToString("N0") + " KB";
        }

        public void Upload()
        {
            _fromText.Content = LocalFile;
            _toText.Content = URL;
            _startTime = DateTime.Now;

            _blobTransfer.Blob = Blob;
            _blobTransfer.LocalFile = LocalFile;
            _blobTransfer.UploadBlobAsync();
        }

        public void Download()
        {
            _fromText.Content = URL;
            _toText.Content = LocalFile;
            _startTime = DateTime.Now;

            string path = System.IO.Path.GetDirectoryName(LocalFile);
            if (!System.IO.Directory.Exists(path))
            {
                System.IO.Directory.CreateDirectory(path);
            }

            _blobTransfer.Blob = Blob;
            _blobTransfer.LocalFile = LocalFile;
            _blobTransfer.DownloadBlobAsync();
        }

        public void Cancel()
        {
            _cancelBtn.IsEnabled = false;
            _blobTransfer.CancelAsync();
        }

        void _cancelBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_cancelBtn.Content.ToString() == "Cancel")
            {
                Cancel();
            }
        }
    }
}
