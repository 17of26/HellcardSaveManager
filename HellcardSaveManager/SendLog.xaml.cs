using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Net.Mail;
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


namespace HellcardSaveManager
{
    /// <summary>
    /// Interaktionslogik für SendLog.xaml
    /// </summary>
    public partial class SendLog : Window
    {
        //constants
        private const string _logFile = "HELLCARD_Demo_log.txt";
        private const string _logsHistory = "logs";
        private const string _processName = "HELLCARD_Demo";
        private const string _emailTo = "Threnodia@gmx.de"; //"awesomehellcardtool@thingtrunk.com"; //change this for developing purposes as to not spam thingtrunk
        private const string _emailSubject = "Hellcard Log";
        private const string _smtpClient = "mail.funkfreunde.net"; //please don't abuse!
        private const string _smtpUser = "HellcardCommunityTool@funkfreunde.net";
        private byte[] _smtpPWcrypt = new byte[] { 220, 125, 173, 237, 89, 151, 230, 248 };

        //Properties
        public System.IO.FileInfo Logfile { get; set; }
        public bool IsSendMinidump { get; set; }
        public DirectoryInfo GameDir { get; set; }

        //constructor
        public SendLog(string pathLog, bool isSendMinidump, string gameDir)
        {
            InitializeComponent();

            Logfile = new System.IO.FileInfo(System.IO.Path.Combine(pathLog, "HELLCARD_Demo_log.txt"));

            IsSendMinidump = isSendMinidump;

            GameDir = new DirectoryInfo(gameDir);

            //Initialize strings
            txtContains.Text = "The email will contain:\n"
                          + "* Salutation and greetings\n"
                          + "* Above data\n"
                          + "* Log file " + Logfile.Name + "\n"
                          + "* Historical logs (zip)";
            if (isSendMinidump)
            { txtContains.Text += "\n* Dumpfile from crash"; }
        }




        //Button handling/send mail
        public void btnSendMail_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            //check inputs
            if (tbxName.Text == "" || tbxDescription.Text == "")
            {
                MessageBox.Show("Please enter your name and a short description.", "Input(s) empty");
                return;
            }

            string htmlbody = "<html>Hello Thing Trunk team,<br/><br/>"
                    + "this is an automatically created email with some attachments (might be: log, historical logs, dump files).<br/><br/>"
                    + "User: " + tbxName.Text + "<br/>"
                    + "Other players: " + tbxPartners.Text + "<br/>"
                    + "Issue description:<br/>" + tbxDescription.Text.Replace("\r\n", "<br/>") + "<br/><br/>"
                    + "Kind regards,<br/>"
                    + "Your Hellcard Save Manager Community Team<br/><br/><br/>"
                    + "P.S.: If you ever get spammed by mails from this adress, please contact Essarielle @Discord.</html>";


            //make list of attachments
            List<string> lstAttachments = new List<string>();
            List<bool> lstDeleteAttach = new List<bool>();

            //most recent logfile
            lstAttachments.Add(Logfile.FullName);
            lstDeleteAttach.Add(false); //don't delete main log file

            //historical logfiles
            string zipFile = System.IO.Path.Combine(Logfile.DirectoryName, "HistLogs_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".zip");
            ZipFile.CreateFromDirectory(System.IO.Path.Combine(Logfile.DirectoryName, _logsHistory), zipFile);
            lstAttachments.Add(zipFile);
            lstDeleteAttach.Add(true); //delete zip of historical log files after send

            //minidump file if isSendMinidump and try sending a crashdump.dmp
            if (IsSendMinidump)
            {
                var minidumpInfo = GameDir.EnumerateFiles("*.mdmp", SearchOption.TopDirectoryOnly).LastOrDefault();
                lstAttachments.Add(minidumpInfo.FullName);
                lstDeleteAttach.Add(false);

                var dumpInfo = GameDir.EnumerateFiles("crashdump.dmp", SearchOption.TopDirectoryOnly).LastOrDefault();
                if (dumpInfo != null)
                {
                    lstAttachments.Add(dumpInfo.FullName);
                    lstDeleteAttach.Add(false);
                }
            }

            Task.Run(() =>
            SendMail.SendMailSmtp(_emailTo, _emailSubject, htmlbody,
                                    lstAttachments.ToArray(), lstDeleteAttach.ToArray(),
                                    _smtpClient, _smtpUser, _smtpPWcrypt) );

            //close window
            Close();
        }



        //private void BtnTest_Click(object sender, RoutedEventArgs e)
        //{
        //    //get Hellcard process
        //    System.Diagnostics.Process[] lstProcesses = System.Diagnostics.Process.GetProcessesByName(_processName);

        //    //no game screenshot without process
        //    if (lstProcesses?.Count() !=1)
        //    {
        //        chkScreenshot.IsEnabled = false;
        //        return;
        //    }
        //    string jpgScreenshot = System.IO.Path.Combine(Logfile.DirectoryName, "Screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".jpg");

        //    IntPtr handle = lstProcesses[0].MainWindowHandle;
            
        //    Graphics graphObjWin = Graphics.FromHwnd(handle);
        //    System.Drawing.Rectangle rectWin = System.Drawing.Rectangle.Round(graphObjWin.ClipBounds);

        //    using (Bitmap bitmap = new Bitmap(width: rectWin.Width, height: rectWin.Height))
        //    {
        //        using (Graphics g = Graphics.FromImage(bitmap))
        //        {
        //            g.CopyFromScreen(upperLeftSource: rectWin.Location, 
        //                             upperLeftDestination: System.Drawing.Point.Empty, 
        //                             blockRegionSize: rectWin.Size);
        //        }
        //        bitmap.Save(jpgScreenshot, System.Drawing.Imaging.ImageFormat.Jpeg);
        //    }



        //        return;
        //}
    }
}
