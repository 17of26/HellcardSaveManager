using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
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
        private const string _emailTo = "awesomehellcardtool@thingtrunk.com"; //change this for developing purposes as to not spam thingtrunk
        private const string _emailSubject = "Hellcard Log";
        private const string _smtpClient = "mail.funkfreunde.net"; //please don't abuse!
        private const string _smtpUser = "HellcardCommunityTool@funkfreunde.net";
        private byte[] _smtpPWcrypt = new byte[] { 220, 125, 173, 237, 89, 151, 230, 248 };

        //Properties
        public System.IO.FileInfo Logfile { get; set; }

        //constructor
        public SendLog(string pathLog, bool isSendMinidump)
        {
            InitializeComponent();

            Logfile = new System.IO.FileInfo(System.IO.Path.Combine(pathLog, "HELLCARD_Demo_log.txt"));

            //Initialize strings
            txtContains.Text = "The email will contain:\n"
                          + "* Salutation and greetings\n"
                          + "* Above data\n"
                          + "* Log file " + Logfile.Name + "\n"
                          + "* Historical logs (zip)";
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

            string zipFile;

            //build email
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(_smtpUser);
                mail.To.Add(new MailAddress(_emailTo));
                mail.Subject = _emailSubject;
                mail.IsBodyHtml = true;
                mail.Body = "<html>Hello Thing Trunk team,<br/><br/>"
                    + "this is an automatically created email with log file attached.<br/><br/>"
                    + "User: " + tbxName.Text + "<br/>"
                    + "Other players: " + tbxPartners.Text + "<br/>"
                    + "Issue description:<br/>" + tbxDescription.Text.Replace("\r\n", "<br/>") + "<br/><br/>"
                    + "Kind regards,<br/>"
                    + "Your Hellcard Save Manager Community Team<br/><br/><br/>"
                    + "P.S.: If you ever get spammed by mails from this adress, please contact Essarielle @Discord.</html>";

                //get attachments
                //most recent logfile
                mail.Attachments.Add(new Attachment(Logfile.FullName));
                //historical logfiles
                zipFile = System.IO.Path.Combine(Logfile.DirectoryName, "HistLogs_" + DateTime.Now.ToString("yyyyMMdd_HHmm") + ".zip");
                ZipFile.CreateFromDirectory(System.IO.Path.Combine(Logfile.DirectoryName, _logsHistory), zipFile);
                mail.Attachments.Add(new Attachment(zipFile));

                //set server and send mail
                SmtpClient smtp = new SmtpClient(_smtpClient);
                System.Net.NetworkCredential cred = new System.Net.NetworkCredential(_smtpUser, SimpleCrypt.Crypt.Decrypt(_smtpPWcrypt));
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = cred;
                smtp.Port = 25;
                smtp.EnableSsl = false;
                smtp.Send(mail);
            }

            //delete temp files
            System.IO.FileInfo fiZip = new System.IO.FileInfo(zipFile);
            for (int i = 0;  i < 30; i++)
            {
                try
                {
                    fiZip.Delete();
                    break;
                }
                catch (System.IO.IOException)
                {
                    System.Threading.Thread.Sleep(200);
                }                    
            }
            

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
