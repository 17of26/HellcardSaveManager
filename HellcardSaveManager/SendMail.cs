using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Threading.Tasks;

namespace HellcardSaveManager
{
    static class SendMail
    {
        public static void SendMailSmtp(string to,
                                        string subject,
                                        string htmlbody,
                                        string[] arrAttachements,
                                        bool[] arrDeleteAttach,
                                        string smtpclient,
                                        string smtpuser,
                                        byte[] smtpPWcrypt                                        )
        {
            //build email
            using (MailMessage mail = new MailMessage())
            {
                mail.From = new MailAddress(smtpuser);
                mail.To.Add(new MailAddress(to));
                mail.Subject = subject;
                mail.IsBodyHtml = true;
                mail.Body = htmlbody;

                foreach (string attachment in arrAttachements)
                {
                    mail.Attachments.Add(new Attachment(attachment));
                }


                //set server and send mail
                SmtpClient smtp = new SmtpClient(smtpclient);
                System.Net.NetworkCredential cred = new System.Net.NetworkCredential(smtpuser, SimpleCrypt.Crypt.Decrypt(smtpPWcrypt));
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = cred;
                smtp.Port = 25;
                smtp.EnableSsl = false;
                smtp.Send(mail);
            }

            //delete temp files
            System.Threading.Thread.Sleep(1000); //wait 1sec, hopefully, no files are in use now

            for (int i = 0; i < arrAttachements.Length; i++)
            {
                if (arrDeleteAttach[i])
                {
                    System.IO.FileInfo fily = new System.IO.FileInfo(arrAttachements[i]);
                    fily.Delete();
                }
            }

            
        }


    }
}
