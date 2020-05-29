using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;


namespace HellcardSaveManager
{
    public partial class SendLog
    {
        //constants
        private const string _logFile = "HELLCARD_Demo_log.txt";
        private const string _logsHistory = "logs";
        private const string _emailTo = "awesomehellcardtool@thingtrunk.com"; //change this for developing purposes as to not spam thingtrunk
        private const string _emailSubject = "Hellcard Log";
        private const string _smtpClient = "mail.funkfreunde.net"; //please don't abuse!
        private const string _smtpUser = "HellcardCommunityTool@funkfreunde.net";
        private readonly byte[] _smtpPWcrypt = { 220, 125, 173, 237, 89, 151, 230, 248 };

        //Properties
        public FileInfo Logfile { get; set; }
        public bool IsSendMinidump { get; set; }
        public DirectoryInfo GameDir { get; set; }

        //constructor
        public SendLog(string pathLog, bool isSendMinidump, string gameDir)
        {
            InitializeComponent();

            Logfile = new FileInfo(Path.Combine(pathLog, _logFile));

            IsSendMinidump = isSendMinidump;

            GameDir = new DirectoryInfo(gameDir);

            //Initialize strings
            txtContains.Text = "The email will contain:\n"
                          + "* Salutation and greetings\n"
                          + "* Above data\n"
                          + "* Log file " + Logfile.Name + "\n"
                          + "* Historical logs (zip)";

            if (isSendMinidump)
            {
                txtContains.Text += "\n* Dumpfile from crash";
            }
        }
        

        //Button handling/send mail
        public void btnSendMail_OnClick(object sender, RoutedEventArgs e)
        {
            //check inputs
            if (tbxName.Text == "" || tbxDescription.Text == "")
            {
                MessageBox.Show("Please enter your name and a short description.", "Input(s) empty");
                return;
            }

            var htmlbody = "<html>Hello Thing Trunk team,<br/><br/>"
                           + "this is an automatically created email with some attachments (might be: log, historical logs, dump files).<br/><br/>"
                           + "User: " + tbxName.Text + "<br/>"
                           + "Other players: " + tbxPartners.Text + "<br/>"
                           + "Issue description:<br/>" + tbxDescription.Text.Replace("\r\n", "<br/>") + "<br/><br/>"
                           + "Kind regards,<br/>"
                           + "Your Hellcard Save Manager Community Team<br/><br/><br/>"
                           + "P.S.: If you ever get spammed by mails from this adress, please contact Essarielle @Discord.</html>";


            var attachments = new List<(string FilePath, bool ShouldDelete)>();

            //most recent logfile
            var logcopy = Logfile.CopyTo(Path.Combine(Logfile.DirectoryName, "HELLCARD_Demo_log_Copy.txt"), true);
            attachments.Add((logcopy.FullName, true));

            //historical logfiles
            var zipFile = Path.Combine(Logfile.DirectoryName, "HistLogs_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".zip");
            ZipFile.CreateFromDirectory(Path.Combine(Logfile.DirectoryName, _logsHistory), zipFile);
            attachments.Add((zipFile, true));

            //minidump file if isSendMinidump and try sending a crashdump.dmp
            if (IsSendMinidump)
            {
                var minidumpInfo = GameDir.EnumerateFiles("*.mdmp", SearchOption.TopDirectoryOnly).LastOrDefault();

                if (minidumpInfo != null)
                {
                    attachments.Add((minidumpInfo.FullName, false));
                }

                var dumpInfo = GameDir.EnumerateFiles("crashdump.dmp", SearchOption.TopDirectoryOnly).LastOrDefault();
                if (dumpInfo != null)
                {
                    attachments.Add((dumpInfo.FullName, false));
                }
            }

            Task.Run(() => SendMail.SendMailSmtp(_emailTo, _emailSubject, htmlbody, attachments,_smtpClient, _smtpUser, _smtpPWcrypt) );

            Close();
        }
    }
}
