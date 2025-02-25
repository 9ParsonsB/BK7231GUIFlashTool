﻿using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace BK7231Flasher
{
    public partial class FormDownloader : Form
    {
        static FormDownloader Singleton;
        Thread worker;
        FormMain fm;
        string list_url = "https://github.com/openshwprojects/OpenBK7231T_App/releases";
        // string list_url = "http://example.com/";
        BKType bkType;

        public FormDownloader(FormMain formMain, BKType bkType)
        {
            Singleton = this;
            this.fm = formMain;
            this.bkType = bkType;
            InitializeComponent();
        }

        private void FormDownloader_Load(object sender, EventArgs e)
        {
            startDownloaderThread();
        }
        void startDownloaderThread()
        {
            worker = new Thread(new ThreadStart(downloadThread));
            worker.Start();
        }
        public void setState(string ss, Color c)
        {
            Singleton.progressBar1.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                labelState.Text = ss;
                labelState.BackColor = c;
            });
        }
        public void setProgress(int cur, int max)
        {
            Singleton.progressBar1.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                progressBar1.Maximum = max;
                progressBar1.Value = cur;
            });
        }
        void downloadThread()
        {
            try
            {
                doDownloadInternal();
            }
            catch (Exception ex)
            {
                setState("Exception!", Color.Red);
                setState(ex.ToString(), Color.Red);
                addLog("It's possible that your system does not support Secure Protocol needed by github.", Color.Red);
                addLog("Sorry, exception occured.", Color.Red);
                addLog("Please manually download firmware from here:", Color.Red);
                addLog(list_url, Color.Red);
                string pfx = FormMain.getFirmwarePrefix(bkType);
                addLog("Please choose the file with prefix "+pfx, Color.Red);
                addLog("Please put this in 'firmwares' dir in dir where the flasher exe is and restart flasher",Color.Red);
            }
        }
        void doDownloadInternal() { 
            setState("Downloading main Releases page...", Color.Transparent);
            Thread.Sleep(200);
            addLog("Target platform: " + bkType);
            ServicePointManager.ServerCertificateValidationCallback += ValidateRemoteCertificate;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolTypeExtensions.Tls11 | SecurityProtocolTypeExtensions.Tls12;// | SecurityProtocolType.Ssl3;
            WebClient webClient = new WebClient();
            webClient.DownloadProgressChanged += (s, e) =>
            {
                setProgress((int)e.BytesReceived, (int)e.TotalBytesToReceive);
            };
            webClient.DownloadFileCompleted += (s, e) =>
            {
            };
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolTypeExtensions.Tls11 | SecurityProtocolTypeExtensions.Tls12;// | SecurityProtocolType.Ssl3;
            webClient.Headers.Add("user-agent", "request");
            addLog("Will request page: " + list_url);
            string contents = webClient.DownloadString(list_url);
            if (contents.Length <= 1)
            {
                setState("Failed to download HTML page, receiver empty buffer?!", Color.Red);
                addError("Failed to download HTML page, receiver empty buffer?!");
                return;
            }
            addLog("Got reply length " + contents.Length);
            addLog("Now will search page for binary link...");
            string pfx = FormMain.getFirmwarePrefix(bkType);
            addLog("Searching for: " +pfx+"!");
            int ofs = contents.IndexOf(pfx);
            if(ofs == -1)
            {
                setState("Failed to find binary link!", Color.Red);
                addError("Failed to find binary link in "+list_url+"!");
                return;
            }
            setState("Searching downloaded page...", Color.Transparent);
            Thread.Sleep(200);
            string firmware_binary_url = pickQuotedString(contents,ofs);
            addLog("Found link: " + firmware_binary_url + "!");
            string fileName = Path.GetFileName(firmware_binary_url);
            string dir = fm.getFirmwareDir();
            string tg = Path.Combine(dir, fileName);
            addLog("Now will try to download it to " + tg+"!");
            webClient.DownloadFile(firmware_binary_url, tg);
            if (File.Exists(tg))
            {
                addSuccess("Downloaded and saved "+tg+"!");
                setState("Download ready! You can close this dialog now.", Color.Green);
            }
            else
            {
                setState("Failed to download!", Color.Red);
                addError("Failed to download!");
            }
        }
        void addLog(string s)
        {
            this.addLog(s, Color.Black);
        }
        void addError(string s)
        {
            this.addLog(s, Color.Red);
        }
        void addSuccess(string s)
        {
            this.addLog(s, Color.Green);
        }
        void addWarning(string s)
        {
            this.addLog(s, Color.Orange);
        }
        public void addLog(string s, Color col)
        {
            Singleton.richTextBoxLog.Invoke((MethodInvoker)delegate {
                // Running on the UI thread
                RichTextUtil.AppendText(Singleton.richTextBoxLog, s + Environment.NewLine, col);
            });
        }
        string pickQuotedString(string buffer, int at)
        {
            int start = at;
            while(start>0&&buffer[start] != '"')
            {
                start--;
            }
            start++;
            int end = at;
            while (end + 1 < buffer.Length && buffer[end] != '"')
            {
                end++;
            }
            return buffer.Substring(start, end - start);
        }
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors error)
        {
            // If the certificate is a valid, signed certificate, return true.
            if (error == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            Console.WriteLine("X509Certificate [{0}] Policy Error: '{1}'",
                cert.Subject,
                error.ToString());

            return false;
        }
    }
}
