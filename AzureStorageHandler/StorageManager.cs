//using AzureStorageHandler;
//using Microsoft.WindowsAzure.Storage;
//using Microsoft.WindowsAzure.Storage.Auth;
//using Microsoft.WindowsAzure.Storage.Blob;
//using System;
//using System.Collections.Generic;
//using System.Configuration;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;

//namespace AzureStorageHandler
//{
//    public class StorageManager
//    {
//        public BlobTransfer Transfer { get; private set; }

//        private List<BlobTransfer> transfers = new List<BlobTransfer>();

//        private CloudStorageAccount _account;
//        private CloudBlobClient _blobClient;
//        private CloudBlobContainer _root;
//        private BlobTransfer.TransferTypeEnum _transferType;

//        public MainWindow View { get; set; }
//        public bool Transfering { get; set; }

//        public async Task<bool> Connect()
//        {
//            var appSettings = ConfigurationManager.AppSettings;
//            string accountName = appSettings.Get("storageName");
//            string accountKey = appSettings.Get("storageKey");
//            var sc = new StorageCredentials(accountName, accountKey);
//            _account = new CloudStorageAccount(sc, true);
//            _blobClient = _account.CreateCloudBlobClient();
//            _root = _blobClient.GetContainerReference("azurestorage");
//            await _root.CreateIfNotExistsAsync();

//            await _root.CreateIfNotExistsAsync();
//            return await _root.ExistsAsync();
//        }

//        public IEnumerable<CloudBlob> Blobs()
//        {
//            List<CloudBlob> blobs = new List<CloudBlob>();
//            foreach (IListBlobItem item in _root.ListBlobs())
//            {
//                CloudBlockBlob blob = _root.GetBlockBlobReference(item.Uri.Segments.Last());
//                if (blob.Exists())
//                {
//                    blob.FetchAttributes();
//                    blobs.Add(new CloudBlob()
//                    {
//                        Name = blob.Uri.Segments.Last(),
//                        Uri = blob.Uri,
//                        Size = Math.Round(blob.Properties.Length / 1000000D, 2) + "MB",
//                        UploadDate = DateTime.Parse(blob.Properties.LastModified.ToString())
//                    });

//                }
//            }
//            return blobs;
//        }

//        public async Task<BlobTransfer> UploadFile(string path)
//        {
//            CloudBlockBlob blob = _root.GetBlockBlobReference(Path.GetFileName(path));

//            using (FileStream tmpfs = new FileStream(path, FileMode.Open, FileAccess.Read))
//            {

//            }

//            var transfer = new BlobTransfer(blob, path);
//            transfer.TransferType = BlobTransfer.TransferTypeEnum.Upload;
//            transfer.TransferCompleted += Transfer_TransferCompleted;
//            transfers.Add(transfer);
//            if (transfers.Count < 3)
//                transfer.UploadBlobAsync();
//            return transfer;
//        }

//        void Transfer_TransferCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
//        {
//            View.ViewTransferCompleted(e);
//            transfers.Remove((BlobTransfer)sender);
//            foreach (BlobTransfer transfer in transfers)
//            {
//                if (!transfer.Active)
//                {
//                    if (transfer.TransferType == BlobTransfer.TransferTypeEnum.Download) transfer.DownloadBlobAsync();
//                    else transfer.UploadBlobAsync();
//                }
//            }
//        }

//        public async Task<bool> RemoveFile(Uri fileUri)
//        {
//            CloudBlockBlob blob = _root.GetBlockBlobReference(fileUri.Segments.Last());
//            return await blob.DeleteIfExistsAsync();
//        }

//        public void Cancel(BlobTransfer transfer)
//        {
//            transfer.CancelAsync();
//            transfers.Remove(transfer);
//            View.ViewTransferCancelled(_transferType);
//        }
//    }

//    public struct CloudBlob
//    {
//        public string Name { get; set; }
//        public DateTime UploadDate { get; set; }
//        public string Size { get; set; }
//        public Uri Uri { get; set; }
//    }

//    // A modified version of the ProgressStream from http://blogs.msdn.com/b/paolos/archive/2010/05/25/large-message-transfer-with-wcf-adapters-part-1.aspx
//    // This class allows progress changed events to be raised from the blob upload/download.
//    public class ProgressStream : Stream
//    {
//        #region Private Fields
//        private Stream stream;
//        private long bytesTransferred;
//        private long totalLength;
//        #endregion

//        #region Public Handler
//        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
//        #endregion

//        #region Public Constructor
//        public ProgressStream(Stream file)
//        {
//            this.stream = file;
//            this.totalLength = file.Length;
//            this.bytesTransferred = 0;
//        }
//        #endregion

//        #region Public Properties
//        public override bool CanRead
//        {
//            get
//            {
//                return this.stream.CanRead;
//            }
//        }

//        public override bool CanSeek
//        {
//            get
//            {
//                return this.stream.CanSeek;
//            }
//        }

//        public override bool CanWrite
//        {
//            get
//            {
//                return this.stream.CanWrite;
//            }
//        }

//        public override void Flush()
//        {
//            this.stream.Flush();
//        }

//        public override void Close()
//        {
//            this.stream.Close();
//        }

//        public override long Length
//        {
//            get
//            {
//                return this.stream.Length;
//            }
//        }

//        public override long Position
//        {
//            get
//            {
//                return this.stream.Position;
//            }
//            set
//            {
//                this.stream.Position = value;
//            }
//        }
//        #endregion

//        #region Public Methods
//        public override int Read(byte[] buffer, int offset, int count)
//        {
//            int result = stream.Read(buffer, offset, count);
//            bytesTransferred += result;
//            if (ProgressChanged != null)
//            {
//                try
//                {
//                    OnProgressChanged(new ProgressChangedEventArgs(bytesTransferred, totalLength));
//                    //ProgressChanged(this, new ProgressChangedEventArgs(bytesTransferred, totalLength));
//                }
//                catch (Exception)
//                {
//                    ProgressChanged = null;
//                }
//            }
//            return result;
//        }

//        protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
//        {
//            if (ProgressChanged != null)
//                ProgressChanged(this, e);
//        }

//        public override long Seek(long offset, SeekOrigin origin)
//        {
//            return this.stream.Seek(offset, origin);
//        }

//        public override void SetLength(long value)
//        {
//            totalLength = value;
//            //this.stream.SetLength(value);
//        }

//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            this.stream.Write(buffer, offset, count);
//            bytesTransferred += count;
//            {
//                try
//                {
//                    OnProgressChanged(new ProgressChangedEventArgs(bytesTransferred, totalLength));
//                    //ProgressChanged(this, new ProgressChangedEventArgs(bytesTransferred, totalLength));
//                }
//                catch (Exception)
//                {
//                    ProgressChanged = null;
//                }
//            }
//        }

//        protected override void Dispose(bool disposing)
//        {
//            stream.Dispose();
//            base.Dispose(disposing);
//        }

//        #endregion
//    }

//    public enum TransferTypeEnum
//    {
//        Download,
//        Upload
//    }

//    public class ProgressStream : Stream
//    {
//        #region Private Fields
//        private Stream stream;
//        private long bytesTransferred;
//        private long totalLength;
//        #endregion

//        #region Public Handler
//        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;
//        #endregion

//        #region Public Constructor
//        public ProgressStream(Stream file)
//        {
//            this.stream = file;
//            this.totalLength = file.Length;
//            this.bytesTransferred = 0;
//        }
//        #endregion

//        #region Public Properties
//        public override bool CanRead
//        {
//            get
//            {
//                return this.stream.CanRead;
//            }
//        }

//        public override bool CanSeek
//        {
//            get
//            {
//                return this.stream.CanSeek;
//            }
//        }

//        public override bool CanWrite
//        {
//            get
//            {
//                return this.stream.CanWrite;
//            }
//        }

//        public override void Flush()
//        {
//            this.stream.Flush();
//        }

//        public override void Close()
//        {
//            this.stream.Close();
//        }

//        public override long Length
//        {
//            get
//            {
//                return this.stream.Length;
//            }
//        }

//        public override long Position
//        {
//            get
//            {
//                return this.stream.Position;
//            }
//            set
//            {
//                this.stream.Position = value;
//            }
//        }
//        #endregion

//        #region Public Methods
//        public override int Read(byte[] buffer, int offset, int count)
//        {
//            int result = stream.Read(buffer, offset, count);
//            bytesTransferred += result;
//            if (ProgressChanged != null)
//            {
//                try
//                {
//                    OnProgressChanged(new ProgressChangedEventArgs(bytesTransferred, totalLength));
//                    //ProgressChanged(this, new ProgressChangedEventArgs(bytesTransferred, totalLength));
//                }
//                catch (Exception)
//                {
//                    ProgressChanged = null;
//                }
//            }
//            return result;
//        }

//        protected virtual void OnProgressChanged(ProgressChangedEventArgs e)
//        {
//            if (ProgressChanged != null)
//                ProgressChanged(this, e);
//        }

//        public override long Seek(long offset, SeekOrigin origin)
//        {
//            return this.stream.Seek(offset, origin);
//        }

//        public override void SetLength(long value)
//        {
//            totalLength = value;
//            //this.stream.SetLength(value);
//        }

//        public override void Write(byte[] buffer, int offset, int count)
//        {
//            this.stream.Write(buffer, offset, count);
//            bytesTransferred += count;
//            {
//                try
//                {
//                    OnProgressChanged(new ProgressChangedEventArgs(bytesTransferred, totalLength));
//                    //ProgressChanged(this, new ProgressChangedEventArgs(bytesTransferred, totalLength));
//                }
//                catch (Exception)
//                {
//                    ProgressChanged = null;
//                }
//            }
//        }

//        protected override void Dispose(bool disposing)
//        {
//            stream.Dispose();
//            base.Dispose(disposing);
//        }

//        #endregion
//    }

//    private class BlobTransferAsyncState
//    {
//        public ICloudBlob Blob;
//        public Stream Stream;
//        public DateTime Started;
//        public bool Cancelled;

//        public BlobTransferAsyncState(ICloudBlob blob, Stream stream)
//            : this(blob, stream, DateTime.Now)
//        { }

//        public BlobTransferAsyncState(ICloudBlob blob, Stream stream, DateTime started)
//        {
//            Blob = blob;
//            Stream = stream;
//            Started = started;
//            Cancelled = false;
//        }
//    }

//    private class ProgressChangedEventArgs : EventArgs
//    {
//        #region Private Fields
//        private long bytesRead;
//        private long totalLength;
//        #endregion

//        #region Public Constructor
//        public ProgressChangedEventArgs(long bytesRead, long totalLength)
//        {
//            this.bytesRead = bytesRead;
//            this.totalLength = totalLength;
//        }
//        #endregion

//        #region Public properties

//        public long BytesRead
//        {
//            get
//            {
//                return this.bytesRead;
//            }
//            set
//            {
//                this.bytesRead = value;
//            }
//        }

//        public long TotalLength
//        {
//            get
//            {
//                return this.totalLength;
//            }
//            set
//            {
//                this.totalLength = value;
//            }
//        }
//        #endregion
//    }

//    public class BlobTransferProgressChangedEventArgs : System.ComponentModel.ProgressChangedEventArgs
//    {
//        private long m_BytesSent = 0;
//        private long m_TotalBytesToSend = 0;
//        private double m_Speed = 0;

//        public long BytesSent
//        {
//            get { return m_BytesSent; }
//        }

//        public long TotalBytesToSend
//        {
//            get { return m_TotalBytesToSend; }
//        }

//        public double Speed
//        {
//            get { return m_Speed; }
//        }

//        public TimeSpan TimeRemaining
//        {
//            get
//            {
//                TimeSpan time = new TimeSpan(0, 0, (int)((TotalBytesToSend - m_BytesSent) / (m_Speed == 0 ? 1 : m_Speed)));
//                return time;
//            }
//        }

//        public BlobTransferProgressChangedEventArgs(long BytesSent, long TotalBytesToSend, int progressPercentage, double Speed, object userState)
//            : base(progressPercentage, userState)
//        {
//            m_BytesSent = BytesSent;
//            m_TotalBytesToSend = TotalBytesToSend;
//            m_Speed = Speed;
//        }
//    }
//}
