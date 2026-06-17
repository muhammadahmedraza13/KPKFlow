using System.Net.Mail;
using System.Net;
using KPKflowApi.Extensions;
using System.Data;
using KPKflowApi.Context;
using System.Collections.Specialized;
using System.Text;
using System.Reflection;
using KPKflowApi.Mail;
using Microsoft.IdentityModel.Tokens;

namespace KPKflowApi.Utility
{
    public class SendEmail
    {
        private readonly DataEncryptor _dataencryptor;
        private readonly ILogger<EmailSetup> _logger;
        public SendEmail(ILogger<EmailSetup> logger,DataEncryptor dataencryptor)
        {
            _logger = logger;
            _dataencryptor = dataencryptor;
        }
        public void SystemActivityLog(int UserID, int? ActivityID, string? ActivityDetails, DataAccessLayer _DAL)
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
        public bool SendEmailToUsers(List<string> EmailList, string EmailBody, string EmailSubject, DataAccessLayer _DAL, List<string> ccList = null, List<string> attachments = null)
        {
            bool CheckEmail = false;
            try
            {
                DataTable? smtpsettings = GetSMTPSettings(_DAL);
                // SMTP INFORMATION
                if (smtpsettings != null && smtpsettings.Rows.Count > 0)
                {
                    string smtpServer = smtpsettings.Rows[0]["Smtp"].ToString();
                    int smtpPort = Convert.ToInt32(smtpsettings.Rows[0]["SmtpPort"].ToString());
                    string smtpUsername = smtpsettings.Rows[0]["SenderEmailID"].ToString();
                    string smtpPassword = _dataencryptor.DecryptPassword(smtpsettings.Rows[0]["SmtpPassword"].ToString());
                    string EnableSSL = smtpsettings.Rows[0]["EnableSSL"].ToString();
                    string subject = EmailSubject;
                    string body = EmailBody;
                    // SMTP class to send Email
                    SmtpClient client = new SmtpClient(smtpServer, smtpPort)
                    {
                        UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(smtpUsername, smtpPassword),
                        EnableSsl = (EnableSSL == "True" || EnableSSL == "true") ? true : false
                    };

                    // Create Compose Email
                    MailMessage mail = new MailMessage
                    {
                        From = new MailAddress(smtpUsername, "Bid Management System")
                    };

                    foreach (var to in EmailList)
                    {
                        mail.To.Add(to);
                    }

                    mail.Subject = subject;
                    mail.Body = body;

                    // ADDING CC RECIPIENT PROVIDER
                    if (ccList != null && ccList.Count > 0)
                    {
                        foreach (var cc in ccList)
                        {
                            mail.CC.Add(cc);
                        }
                    }

                    if (attachments != null && attachments.Count > 0)
                    {
                        foreach (var attach in attachments)
                        {
                            mail.Attachments.Add(new Attachment(attach));
                        }
                    }

                    // SENDING MAIL
                    client.Send(mail);
                    Console.Write("Email Sent Successfully");
                    CheckEmail = true;
                    return CheckEmail;
                }
                else
                {
                    return CheckEmail;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error on Sending Mail" + ex.Message);
                return CheckEmail;
            }
        }
        public async Task<bool> SendWorkflowEmail(string ToAddress, string EmailSub, string Body, DataAccessLayer _DAL, List<string> attachments = null)
        {
            //var _configuration = LazyConfiguration.Value;
            DataTable? smtpsettings = GetSMTPSettings(_DAL);
            try
            {
                if (smtpsettings != null && smtpsettings.Rows.Count > 0)
                {
                    string smtpServer = smtpsettings.Rows[0]["Smtp"].ToString();
                    int smtpPort = Convert.ToInt32(smtpsettings.Rows[0]["SmtpPort"].ToString());
                    string smtpUsername = smtpsettings.Rows[0]["SenderEmailID"].ToString();
                    string smtpPassword = _dataencryptor.DecryptPassword(smtpsettings.Rows[0]["SmtpPassword"].ToString());
                    string EnableSSL = smtpsettings.Rows[0]["EnableSSL"].ToString();
                    string subject = EmailSub;
                    string body = Body;


                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(smtpUsername, "Bid Management System");
                        mail.To.Add(ToAddress);
                        mail.Subject = EmailSub;
                        mail.Body = Body;
                        mail.IsBodyHtml = true;
                        mail.BodyEncoding = Encoding.UTF8;

                        // HTML view
                        AlternateView htmlView = AlternateView.CreateAlternateViewFromString(Body, null, "text/html");
                        mail.AlternateViews.Add(htmlView);

                        if (attachments != null && attachments.Count > 0)
                        {
                            foreach (var attach in attachments)
                            {
                                mail.Attachments.Add(new Attachment(attach));
                            }
                        }

                        using (var smtp = new SmtpClient())
                        {
                            smtp.Host = smtpServer;
                            smtp.Port = smtpPort;
                            smtp.EnableSsl = (EnableSSL == "True" || EnableSSL == "true") ? true : false;
                            smtp.Timeout = 15000;
                            smtp.UseDefaultCredentials = false;
                            smtp.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

                            await smtp.SendMailAsync(mail);
                        }
                    }


                    _logger.LogInformation("Workflow Email Sent Successfully to: " + ToAddress);

                    return true;
                }
                else
                {
                    _logger.LogError("smtp settings not found or incorrect");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Workflow Email Not Sent Successfully to: " + ToAddress + "\r\n Reason : " + ex);
                return false;
            }
        }

        public bool SendEmailToVendors(List<string> EmailList, string EmailBody, string EmailSubject, DataAccessLayer _DAL, List<string> ccList = null)
        {
            try
            {
                DataTable? smtpsettings = GetSMTPSettings(_DAL);
                if (smtpsettings == null || smtpsettings.Rows.Count == 0) return false;

                var row = smtpsettings.Rows[0];
                string smtpServer = row["Smtp"].ToString();
                int smtpPort = Convert.ToInt32(row["SmtpPort"]);
                string EnableSSL = smtpsettings.Rows[0]["EnableSSL"].ToString();
                string smtpUsername = row["SenderEmailID"].ToString();
                string smtpPassword = _dataencryptor.DecryptPassword(row["SmtpPassword"].ToString());

                using (var client = new SmtpClient(smtpServer, smtpPort))
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
                    client.EnableSsl = (EnableSSL == "True" || EnableSSL == "true") ? true : false; 

                    using (var mail = new MailMessage())
                    {
                        mail.From = new MailAddress(smtpUsername, "Bid Management System"); 
                        mail.Subject = EmailSubject;
                        mail.Body = EmailBody;
                        mail.IsBodyHtml = true;

                        foreach (var email in EmailList)
                        {
                            if (!string.IsNullOrEmpty(email)) mail.To.Add(email);
                        }

                        ccList?.ForEach(cc => { if (!string.IsNullOrEmpty(cc)) mail.CC.Add(cc); });

                        client.Send(mail);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log locally to server for debugging
                System.Diagnostics.Debug.WriteLine($"SMTP Error: {ex.Message}");
                return false;
            }
        }
        private DataTable? GetSMTPSettings(DataAccessLayer DAL)
        {
            DataTable? dt = new DataTable();
            try
            {
                NameValueCollection nv = new NameValueCollection();
                nv.Clear();
                nv.Add("WithPassword-INT", "1");
                dt = DAL.GetData("sp_select_SmtpSettings", nv, DAL.CSManagementPortalDatabase);

                if (dt.Rows.Count > 0)
                {
                    return dt;
                }
            }
            catch (Exception ex)
            {
                return dt;
            }
            return dt;
        }
    }
}
