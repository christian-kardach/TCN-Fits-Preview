using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;
using TA.ObjectOrientedAstronomy.FlexibleImageTransportSystem;

namespace TCN_Fits_Preview
{
    sealed class ViewModel : INotifyPropertyChanged
    {
        FileSystemWatcher watcher = new FileSystemWatcher();

        private bool _isActive;
        private string _activeBtnText = "Start";
        private string _statusText;
        private string _activePath;
        private string _destinationPath;
        private string _destinationIP;
        private string _userPassword;
        private BitmapImage _previewImage;

        public ViewModel()
        {

            watcher.EnableRaisingEvents = false;
            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite;
            watcher.Filter = "*.fits";
            watcher.IncludeSubdirectories = true;
            watcher.Created += new FileSystemEventHandler(OnFileCreated);

        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChange("IsActive");
                }
            }
        }

        public string ActiveBtnText
        {
            get { return _activeBtnText; }
            set
            {
                if (_activeBtnText != value)
                {
                    _activeBtnText = value;
                    OnPropertyChange("ActiveBtnText");
                }
            }
        }

        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChange("StatusText");
                }
            }
        }

        public string ActivePath
        {
            get { return _activePath; }
            set
            {
                if (_activePath != value)
                {
                    _activePath = value;
                    Settings.Default.ActivePath = value;
                    OnPropertyChange("ActivePath");
                }
            }
        }

        public string DestinationPath
        {
            get { return _destinationPath; }
            set
            {
                if (_destinationPath != value)
                {
                    _destinationPath = value;
                    Settings.Default.DestinationPath = value;
                    OnPropertyChange("DestinationPath");
                }
            }
        }

        public string DestinationIP
        {
            get { return _destinationIP; }
            set
            {
                if (_destinationIP != value)
                {
                    _destinationIP = value;
                    Settings.Default.DestinationIP = value;
                    OnPropertyChange("DestinationIP");
                }
            }
        }

        public string UserPassword
        {
            get { return _userPassword; }
            set
            {
                if (_userPassword != value)
                {
                    _userPassword = value;
                    OnPropertyChange("UserPassword");
                }
            }
        }

        public BitmapImage PreviewImage
        {
            get { return _previewImage; }
            set
            {
                if (_previewImage != value)
                {
                    _previewImage = value;
                    OnPropertyChange("PreviewImage");
                }
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                var f = e.FullPath;
                if (WaitForFile(f))
                {
                    OpenImage(f);
                }
            }
        }

        private async void OpenImage(string sourceFile)
        {
            FitsHeaderDataUnit hdu;
            using (var fs = File.OpenRead(sourceFile))
            {
                var reader = new FitsReader(fs);
                hdu = await reader.ReadPrimaryHeaderDataUnit().ConfigureAwait(true);
            }


            StatusText = "New image found...processing";
            var hs = new HistogramStretch();
            hs.StretchMethod = HistogramShape.Linear;

            /*
            var dataArray = PrimaryDataExtractor.ExtractDataArray(hdu);

            if (!hdu.Header.HeaderRecords.Any(p => p.Keyword == "CBLACK"))
                hs.BlackPoint = dataArray.Cast<double>().Min();
            if (!hdu.Header.HeaderRecords.Any(p => p.Keyword == "CWHITE"))
                hs.WhitePoint = dataArray.Cast<double>().Max();
            */
            hs.BlackPoint = 1000;
            hs.WhitePoint = 10000;

            var image = hdu.ToWindowsBitmap(hs);

            System.Drawing.Imaging.ImageCodecInfo jpgEncoder = GetEncoder(System.Drawing.Imaging.ImageFormat.Jpeg);
            System.Drawing.Imaging.Encoder myEncoder = System.Drawing.Imaging.Encoder.Quality;
            System.Drawing.Imaging.EncoderParameters myEncoderParameters = new System.Drawing.Imaging.EncoderParameters(1);
            System.Drawing.Imaging.EncoderParameter myEncoderParameter = new System.Drawing.Imaging.EncoderParameter(myEncoder, 50L);
            myEncoderParameters.Param[0] = myEncoderParameter;


            var savePath = Path.GetDirectoryName(sourceFile);
            var saveName = Path.GetFileNameWithoutExtension(sourceFile);
            image.Save(Path.Combine(savePath, "image.jpg"), jpgEncoder, myEncoderParameters);

            // Thumbnail
            float width = 1024;
            float height = 768;
            var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

            float scale = Math.Min(width / image.Width, height / image.Height);

            var bmp = new System.Drawing.Bitmap((int)width, (int)height);
            var graph = System.Drawing.Graphics.FromImage(bmp);

            // uncomment for higher quality output
            //graph.InterpolationMode = InterpolationMode.High;
            //graph.CompositingQuality = CompositingQuality.HighQuality;
            //graph.SmoothingMode = SmoothingMode.AntiAlias;

            var scaleWidth = (int)(image.Width * scale);
            var scaleHeight = (int)(image.Height * scale);

            graph.FillRectangle(brush, new System.Drawing.RectangleF(0, 0, width, height));
            graph.DrawImage(image, ((int)width - scaleWidth) / 2, ((int)height - scaleHeight) / 2, scaleWidth, scaleHeight);

            bmp.Save(System.IO.Path.Combine(savePath, "image_thumb.jpg"), jpgEncoder, myEncoderParameters);

            var resultImage = Convert(bmp);
            resultImage.Freeze();
            PreviewImage = resultImage;

            CopyToDestination(savePath);

            StatusText = "Done!";
        }

        private bool WaitForFile(string filePath)
        {
            Int32 tries = 0;

            while (true)
            {
                ++tries;

                Boolean wait = false;
                FileStream stream = null;

                try
                {
                    stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None);
                    break;
                }
                catch (Exception ex)
                {
                    if (tries > 10)
                    {
                        return false;
                    }

                    wait = true;
                }
                finally
                {
                    if (stream != null)
                        stream.Close();
                }

                if (wait)
                    Thread.Sleep(250);
            }
            return true;
        }

        private System.Drawing.Imaging.ImageCodecInfo GetEncoder(System.Drawing.Imaging.ImageFormat format)
        {
            System.Drawing.Imaging.ImageCodecInfo[] codecs = System.Drawing.Imaging.ImageCodecInfo.GetImageEncoders();
            foreach (System.Drawing.Imaging.ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        public BitmapImage Convert(System.Drawing.Bitmap src)
        {
            MemoryStream ms = new MemoryStream();
            ((System.Drawing.Bitmap)src).Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private void CopyToDestination(string sourceFilePath)
        {
            var arg = string.Format("-pw {0} {1}/image.jpg {1}/image_thumb.jpg pi@{2}:{3}", UserPassword, sourceFilePath, DestinationIP, DestinationPath);

            var process = new Process();
            var startInfo = new ProcessStartInfo
            {
                WindowStyle = ProcessWindowStyle.Hidden,
                FileName = "pscp",
                Arguments = arg,
                RedirectStandardOutput = true
            };

            process.StartInfo = startInfo;
            process.Start();

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            /*
            // Print the output to Standard Out
            Console.WriteLine(output);

            // Print the ouput to e.g. Visual Studio debug window
            Debug.WriteLine(output);
            */
        }

        public void Start()
        {
            watcher.Path = _activePath;
            watcher.EnableRaisingEvents = true;
            IsActive = true;
            ActiveBtnText = "Stop";
        }

        public void Stop()
        {

            watcher.EnableRaisingEvents = false;
            IsActive = false;
            ActiveBtnText = "Start";
        }

    }
}
