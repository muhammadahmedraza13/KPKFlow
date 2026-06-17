using KPKflowApi.Context;
using KPKflowApi.Extensions;
using System.Collections.Specialized;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace KPKflowApi.Mail
{
    public class EmailSetup
    {
        private readonly DataAccessLayer _DAL;
        private readonly ILogger<EmailSetup> _logger;
        private readonly string _From;
        private readonly string _Host;
        private readonly string _Pass;
        private readonly string _Ssl;
        private readonly string _Port;

        public EmailSetup(ILogger<EmailSetup> logger, DataAccessLayer dAL)
        {
            _logger = logger;
            _DAL = dAL;
            _From = Environment.GetEnvironmentVariable("AM_From");
            _Host = Environment.GetEnvironmentVariable("AM_Host");
            _Pass = Environment.GetEnvironmentVariable("AM_Pass");
            _Ssl =  Environment.GetEnvironmentVariable("AM_Ssl ");
            _Port = Environment.GetEnvironmentVariable("AM_Port");
        }

        #region Activity Log
        public void SystemActivityLog(int UserID, int? ActivityID, string? ActivityDetails)
        {
            bool Result = false;

            try
            {
                NameValueCollection nv = new NameValueCollection();
                nv.Clear();
                nv.Add("FormID-INT", "0");
                nv.Add("ActivityID-INT", ActivityID.ToString());
                nv.Add("UserID-INT", UserID.ToString());
                nv.Add("ActivityDetails-VARCHAR", ActivityDetails);
                Result = _DAL.InsertData("sp_insert_activitylog", nv, _DAL.CSManagementPortalDatabase);
                nv.Clear();

                _logger.LogInformation("{0} {1} {2}", "EmailSetup", MethodBase.GetCurrentMethod().Name, ActivityDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError("{0} {1} {2} {3}", "EmailSetup", MethodBase.GetCurrentMethod().Name, ActivityDetails, ex.Message);
            }
        }
        #endregion

        public async Task<bool> SendEmail(string ToAddress, string EmailSub, string Body, string Workflow, string Initiator, int UserId)
        {
            //var _configuration = LazyConfiguration.Value;

            try
            {
                string from = _From;
                string pass = _Pass;
                string host = _Host;
                bool ssl = Convert.ToInt32(_Ssl) == 1 ? true : false;
                int port = Convert.ToInt32(_Port);

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(from);
                    mail.To.Add(ToAddress);
                    mail.Subject = EmailSub;
                    mail.Body = Body;
                    mail.IsBodyHtml = true; // Ensures HTML emails are properly formatted
                    mail.BodyEncoding = Encoding.UTF8;

                    // Alternative HTML View
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(Body, null, "text/html");
                    mail.AlternateViews.Add(htmlView);

                    using (var smtp = new SmtpClient())
                    {
                        smtp.Host = host;
                        smtp.Port = port;
                        smtp.EnableSsl = ssl; // Required for port 465
                        smtp.Timeout = 15000; // Increased timeout
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(from, pass);

                        // Send email asynchronously and wait for completion
                        await smtp.SendMailAsync(mail);
                    }
                }

                //writelog("Email Sent Successfully to: " + ToAddress);
               
                SystemActivityLog(UserId, 1 , "Sent File Workflow Email!");
                return true;
            }
            catch (Exception ex)
            {
                SystemActivityLog(UserId, 1 , ex.Message);
                return false;
            }
        }

      
        #region Muneeb Work Multi-select Email Share
        public async Task<bool> SendEmailWithAttachments(
    string toAddress,
    string emailSubject,
    string body,
    string workflow,
    string initiator,
    int userId,
    List<string> attachmentPaths,
    List<string> attachmentNames)
        {
            try
            {
                string from = _From;
                string pass = _Pass;
                string host = _Host;
                bool ssl = Convert.ToInt32(_Ssl) == 1;
                int port = Convert.ToInt32(_Port);

                using (var mail = new MailMessage())
                {
                    mail.From = new MailAddress(from);
                    mail.To.Add(toAddress);
                    mail.Subject = emailSubject;
                    mail.Body = body;
                    mail.IsBodyHtml = true;
                    mail.BodyEncoding = Encoding.UTF8;

                    // Add HTML view
                    AlternateView htmlView = AlternateView.CreateAlternateViewFromString(body, null, "text/html");
                    mail.AlternateViews.Add(htmlView);

                    // ✅ Attach multiple files
                    if (attachmentPaths != null && attachmentNames != null &&
                        attachmentPaths.Count == attachmentNames.Count)
                    {
                        for (int i = 0; i < attachmentPaths.Count; i++)
                        {
                            string path = attachmentPaths[i];
                            string name = attachmentNames[i];

                            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                            {
                                Attachment attachment = new Attachment(path);
                                attachment.Name = name;
                                mail.Attachments.Add(attachment);
                            }
                        }
                    }

                    using (var smtp = new SmtpClient())
                    {
                        smtp.Host = host;
                        smtp.Port = port;
                        smtp.EnableSsl = ssl;
                        smtp.Timeout = 15000;
                        smtp.UseDefaultCredentials = false;
                        smtp.Credentials = new NetworkCredential(from, pass);

                        await smtp.SendMailAsync(mail);
                    }
                }

                SystemActivityLog(userId, 1, "Sent File Workflow Email with multiple attachments!");
                return true;
            }
            catch (Exception ex)
            {
                SystemActivityLog(userId, 1, ex.Message);
                return false;
            }
        }
        #endregion
    }
}
