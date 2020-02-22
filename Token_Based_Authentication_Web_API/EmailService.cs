using System; using System.Net; using System.Net.Mail; using System.Web.Configuration;

namespace Token_Based_Authentication_Web_API
{
    public class EmailService
    {
        private readonly string SMTPFromEmail = string.Empty;
        private readonly int SMTPServerPort = 0;
        private readonly string SMTPServer = string.Empty;
        private readonly string SMTPPassword = string.Empty;
        public EmailService()
        {
            SMTPFromEmail = WebConfigurationManager.AppSettings["smtpFromEmail"];
            SMTPServerPort = Convert.ToInt32(WebConfigurationManager.AppSettings["smtpPort"]);
            SMTPServer = WebConfigurationManager.AppSettings["smtpServer"];
            SMTPPassword = WebConfigurationManager.AppSettings["smtpPassword"]; 
        }

        public string SendEmail(string toEmailAddress, string emailSubject, string htmlString)
        {
            string isEmailSentMessage = string.Empty;
            try
            {
                MailMessage message = new MailMessage();
                SmtpClient smtp = new SmtpClient();
                message.From = new MailAddress(SMTPFromEmail);
                message.To.Add(new MailAddress(toEmailAddress));
                message.Subject = emailSubject;
                message.IsBodyHtml = true;
                message.Body = htmlString;
                smtp.Port = SMTPServerPort;
                smtp.Host = SMTPServer; //for host
                smtp.EnableSsl = true;
                smtp.UseDefaultCredentials = false;
                smtp.Credentials = new NetworkCredential(SMTPFromEmail, SMTPPassword);
                smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtp.Send(message);
                isEmailSentMessage = "Email Sent Successfully";
            }
            catch (Exception ex) { isEmailSentMessage = ex.Message; }
            return isEmailSentMessage;
        }
    }
}