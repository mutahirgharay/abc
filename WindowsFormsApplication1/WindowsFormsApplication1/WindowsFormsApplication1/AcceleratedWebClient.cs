using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ComponentModel;
using System.IO;
using WindowsFormsApplication1.Properties;
using System.Windows.Forms;
using WindowsFormsApplication1;
using System.Diagnostics;

namespace NAccelerate
{
    public class AcceleratedWebClient : IDisposable
    {

        public AcceleratedWebClient()
        {


        }
        private List<RangeWebClient> _webClients;
        private string _savePath;

        const int BUFFER_SIZE = 500000;
        const int MIN_FILE_SIZE = 500000;

        private int? GetFileSize(Uri uri)
        {
            WebRequest req = System.Net.HttpWebRequest.Create(uri);
            req.Method = "HEAD";
            using (WebResponse resp = req.GetResponse())
            {
                int contentLength;
                if (resp.Headers["Accept-Ranges"] != "bytes")
                    return null;
                if (int.TryParse(resp.Headers.Get("Content-Length"), out contentLength))
                    return contentLength;
            }
            return null;
        }

        private Tuple<long?, long?> GetRange(int index, int count, long size)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException("count");
            if (count == 1)
                return new Tuple<long?, long?>(null, null);

            long minSize = size / (long)count;

            if (index == (count - 1))
                return new Tuple<long?, long?>(index * minSize, null);
            else
                return new Tuple<long?, long?>(index * minSize, (index + 1) * minSize - 1);
        }

        private void MergeParts()
        {
            Form1._status.Text = "Merging";
            using (var outputFileStream = File.OpenWrite(_savePath))
                foreach (var client in _webClients)
                {
                    using (var fileStream = File.OpenRead(client.Filename))
                        CopyStream(outputFileStream, fileStream);
                    File.Delete(client.Filename);
                }
            Form1._status.Text = "Completed";
            Form1._clear.Enabled = true;
        }

        private void CopyStream(Stream destination, Stream source)
        {
            int count;
            byte[] buffer = new byte[BUFFER_SIZE];
            while ((count = source.Read(buffer, 0, buffer.Length)) > 0)
                destination.Write(buffer, 0, count);
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (_webClients.All(c => c.Status == RangeWebClientStatus.Completed))
                OnDownloadFileCompleted(e);
        }

        protected virtual void OnDownloadFileCompleted(AsyncCompletedEventArgs args)
        {
            MergeParts();
            if (DownloadFileCompleted != null)
                DownloadFileCompleted(this, args);
            st.Stop();
        }
        DateTime previous;
        long last_byte;
        long speed=0;
            Stopwatch st = new Stopwatch();
        protected virtual void OnDownloadProgressChanged(DownloadProgressChangedEventArgs args)
        {
           Form1._watch.Text=(st.ElapsedMilliseconds / 1000).ToString()+"s";

            var totalProgress = (double)_webClients.Sum(client => client.ProgressPercentage) / (double)_webClients.Count;
            var callArgs = new SegmentAwareProgressChangedEventArgs((int)Math.Round(totalProgress));
            if (DownloadProgressChanged != null)
                DownloadProgressChanged(this, callArgs);
            for (int x = 0; x < 4; x++)
            {
                if (_webClients.Count>x)
                    Form1.progress[x + 1].Value = _webClients[x].ProgressPercentage;
            }
            Form1.progress[0].Value = callArgs.ProgressPercentage;
            
            if (args.BytesReceived == 0)
            {
                previous = DateTime.Now;
                last_byte = args.BytesReceived;
                goto x;
            }
            var now = DateTime.Now;
            var time_diff = now - previous;
            var byte_diff = args.BytesReceived - last_byte;
            if (time_diff.Milliseconds != 0 && byte_diff != 0&& byte_diff > 0 )
            {
                    speed = byte_diff*1000 / time_diff.Milliseconds;
                    speed = speed / 1024;
            }

            last_byte = args.BytesReceived;
            previous = now;
            x:
            if (speed<200)
            {
            Form1.speed.Text = speed.ToString()+" KB";            

            }

        }

        public event AsyncCompletedEventHandler DownloadFileCompleted;
        public event SegmentAwareProgressChangedEventHandler DownloadProgressChanged;


        public void DownloadFileAsync(Uri uri, string savePath, int segmentsCount )
        {
            st.Start();
            if (segmentsCount < 1)
                throw new ArgumentOutOfRangeException("segmentsCount");

            int? size = GetFileSize(uri);

            if (!size.HasValue)
                segmentsCount = 1;
            else if (size.Value < MIN_FILE_SIZE)
                segmentsCount = 1;

            _savePath = savePath;
            using (File.Create(_savePath)) { }

            _webClients = new List<RangeWebClient>();
            for (int i = 0; i < segmentsCount; i++)
            {
                var client = new RangeWebClient();

                if (segmentsCount > 1)
                {
                    var range = GetRange(i, segmentsCount, size.Value);
                    client.From = range.Item1;
                    client.To = range.Item2;
                    
                }
                
                client.DownloadProgressChanged += (s, e) => OnDownloadProgressChanged(e);
                client.DownloadFileCompleted += client_DownloadFileCompleted;
                client.Filename = Path.ChangeExtension(_savePath, Path.GetExtension(_savePath) + "." + i.ToString("D3"));
                _webClients.Add(client);
            }
            _webClients.ForEach(client => client.DownloadFileAsync(uri));
        }
        public void Dispose() { }
    }
}
