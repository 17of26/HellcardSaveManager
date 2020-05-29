using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Windows;

namespace HellcardSaveManager
{
    internal static class SendMail
    {
        public static void SendMailSmtp(string to,
                                        string subject,
                                        string htmlbody,
                                        List<(string FilePath, bool ShouldDelete)> attachments,
                                        string server,
                                        string smtpuser,
                                        byte[] smtpPWcrypt                                        )
        {
            //build email
            using (var mail = new MailMessage())
            {
                mail.From = new MailAddress(smtpuser);
                mail.To.Add(new MailAddress(to));
                mail.Subject = subject;
                mail.IsBodyHtml = true;
                mail.Body = htmlbody;

                foreach (var attachment in attachments)
                {
                    mail.Attachments.Add(new Attachment(attachment.FilePath));
                }

                //set server and send mail
                var smtp = new SmtpClient(server)
                {
                    UseDefaultCredentials = false,
                    Credentials = new System.Net.NetworkCredential(smtpuser, SimpleCrypt.Crypt.Decrypt(smtpPWcrypt)),
                    Port = 587,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    EnableSsl = false
                };

                try
                {
                    smtp.Send(mail);
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Failed to send email: {e}");
                }
            }

            foreach (var (filePath, _) in attachments.Where(x => x.ShouldDelete))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception)
                {
                    // Oh well, file just stays on the hard drive :P
                }
            }
        }
    }
}
