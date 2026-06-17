using AngleSharp.Dom;
using System.Runtime.ConstrainedExecution;

namespace KPKflowApi.Utility
{
    public class ActivityLog
    {
        public static readonly int ActivityID_Login = 1;
        public static readonly int ActivityID_LogOut = 2;
        public static readonly int ActivityID_Get = 3;
        public static readonly int ActivityID_Insert = 4;
        public static readonly int ActivityID_Update = 5;
        public static readonly int ActivityID_Delete = 6;
        public static readonly int ActivityID_View = 7;
        public static readonly int ActivityID_Search = 8;
        public static readonly int ActivityID_Error = 9;

        public static readonly string ActivityDetails_Login = "Logged In";
        public static readonly string ActivityDetails_LogOut = "Logged Out";
        public static readonly string ActivityDetails_Get = "Data Is Retrived By Using ";
        public static readonly string ActivityDetails_Insert = "Data Is Inserted By Using ";
        public static readonly string ActivityDetails_Update = "Data Is Updated By Using ";
        public static readonly string ActivityDetails_Delete = "Data Is Deleted By Using ";
        public static readonly string ActivityDetails_View = "User Visited This Page ";
        public static readonly string ActivityDetails_Search = "Data Is Searched By Using ";
        public static readonly string ActivityDetails_Get2 = "Data Is Not Retrived By Using ";
        public static readonly string ActivityDetails_Insert2 = "Data Is Not Inserted By Using ";
        public static readonly string ActivityDetails_Update2 = "Data Is Not Updated By Using ";
        public static readonly string ActivityDetails_Delete2 = "Data Is Not Deleted By Using ";
        public static readonly string ActivityDetails_Search2 = "Data Is Not Searched By Using ";

        public static string EmailSubject = "Your Randomly Generated Password";
        public static readonly string EmailBody = "Dear User,\r\n\r\nWe hope this email finds you well. " +
                             "As part of our security protocols, we have generated a random password for your account. " +
                             "Please find your new password below:\r\n\r\n" +
                             "\r\n\r\nFor security reasons, we recommend changing this password to something memorable as soon as possible. " +
                             "Should you have any questions or concerns, please don't hesitate to reach out to our support team." +
                             "\r\n\r\nThank you for your attention to this matter.";
        public static string EmailSubjectForVendorSupplier = "Invitation to Register: Official Bid Portal";




        private const string PrimaryColor = "#2563eb";   // Modern Blue
        private const string SuccessColor = "#0d6e31";   // Vibrant Success Green
        private const string InfoBgColor = "#f0f9ff";    // Light Blue Background
        private const string InfoBorderColor = "#bae6fd"; // Light Blue Border
        private const string FontFamily = "'Segoe UI', Roboto, Helvetica, Arial, sans-serif";

        #region Private Helper Methods (Optimized Spacing)

        // Added textColor parameter to handle dark text on light backgrounds
        private static string GetHeader(string title, string bgColor, string textColor = "#ffffff") => $@"
        <div style='background-color: {bgColor}; padding: 25px 20px; text-align: center;'>
            <h2 style='color: {textColor}; margin: 0; font-size: 22px; font-weight: 700; letter-spacing: 0.5px;'>{title}</h2>
        </div>";

        private static string GetFooter(string note) => $@"
        <div style='padding: 30px; background-color: #ffffff;'>
            <hr style='border: 0; border-top: 1px solid #f1f5f9; margin: 20px 0;' />
            <p style='font-size: 16px; margin: 0; color: #000000;'>
                Regards,<br>
                <span style='font-weight: normal; color: #334155; font-weight: bold;'>Bid Team</span>
            </p>
        </div>
        <div style='background-color: #f8fafc; padding: 15px; text-align: center; font-size: 11px; color: #94a3b8; border-top: 1px solid #f1f5f9;'>
            {note}<br>© {DateTime.Now.Year} Bid Management System
        </div>";

        private static string BaseContainer(string content) => $@"
        <div style='background-color: #f3f4f6; padding: 20px 10px; font-family: {FontFamily};'>
            <div style='line-height: 1.5; color: #334155; max-width: 550px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 16px; overflow: hidden; box-shadow: 0 10px 15px -3px rgba(0,0,0,0.1); background-color: #ffffff;'>
                {content}
            </div>
        </div>";

        #endregion

        // 1. Vendor Invitation Email
        public static string EmailBodyForInviteVendor(string registrationLink)
        {
            string body = $@"
            {GetHeader("Bank Invitation", PrimaryColor)}
            <div style='padding: 30px; background-color: #ffffff;'>
                <p style='margin-top:0; font-size: 16px;'>Dear <strong>Valued Client</strong>,</p>
                <p style='font-size: 15px;'>We invite you to register on our <strong>Bid Portal</strong> to participate in upcoming bidding opportunities.</p>
                
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{registrationLink}' target='_blank' style='background-color: {PrimaryColor}; color: #ffffff; padding: 14px 28px; text-decoration: none; font-size: 16px; font-weight: 600; border-radius: 8px; display: inline-block;'>Complete Registration</a>
                </div>

                <div style='background-color: {InfoBgColor}; border-radius: 10px; padding: 15px; border: 1px solid {InfoBorderColor};'>
                    <p style='font-size: 14px; color: #0369a1; margin: 0;'><strong>Note:</strong> You will set your secure password after submission.</p>
                </div>
            </div>
            {GetFooter("Official invitation for Bank onboarding.")}";

            return BaseContainer(body);
        }

        // 2. Email Verification (OTP) - UPDATED HEADER TEXT
        public static string EmailBodyForVerification(string emailOtp)
        {
            string body = $@"
            {GetHeader("Email Verification", InfoBgColor, "#1e293b")}
            <div style='padding: 30px; background-color: #ffffff;'>
                <p style='margin-top:0; font-size: 16px;'>Dear Valued Client,</p>
                <p style='font-size: 15px;'>Use the code below to verify your email address:</p>
        
                <div style='background-color: {InfoBgColor}; border: 2px dashed {InfoBorderColor}; border-radius: 12px; padding: 25px; text-align: center; margin: 25px 0;'>
                    <span style='font-size: 36px; font-weight: 800; letter-spacing: 8px; color: {PrimaryColor};'>{emailOtp}</span>
                    <p style='margin: 8px 0 0 0; font-size: 12px; color: #0369a1;'>Valid for 2 minutes only</p>
                </div>

                <p style='color: #ef4444; font-size: 13px; text-align: center; margin-bottom:0;'>For security, do not share this code.</p>
            </div>
            {GetFooter("Automated security verification.")}";

            return BaseContainer(body);
        }

        // 3. Resend OTP Email - UPDATED HEADER TEXT
        public static string EmailBodyForResendOtp(string newEmailOtp)
        {
            string body = $@"
            {GetHeader("New OTP Requested", InfoBgColor, "#1e293b")}
            <div style='padding: 30px; background-color: #ffffff;'>
                <p style='margin-top:0; font-size: 16px;'>Dear Bank,</p>
                <p style='font-size: 15px;'>As requested, here is your new verification code:</p>
        
                <div style='background-color: {InfoBgColor}; border: 2px dashed {InfoBorderColor}; border-radius: 12px; padding: 25px; text-align: center; margin: 25px 0;'>
                    <span style='font-size: 36px; font-weight: 800; letter-spacing: 8px; color: {PrimaryColor};'>{newEmailOtp}</span>
                </div>

                <p style='font-size: 13px; color: #64748b; text-align: center; margin-bottom:0;'>If you did not request this, please ignore this email.</p>
            </div>
            {GetFooter("A new security OTP has been generated.")}";

            return BaseContainer(body);
        }

        // 4. Success Registration Email
        public static string EmailBodyForSuccessRegistration()
        {
            string body = $@"
            {GetHeader("Registration Submitted", SuccessColor)}
            <div style='padding: 30px; background-color: #ffffff;'>
                <p style='margin-top:0; font-size: 16px;'>Hello,</p>
                <p style='font-size: 15px;'>Your registration is <strong>successfully submitted</strong> and is now under review.</p>
        
                <div style='background-color: #f0fdf4; padding: 20px; border-radius: 10px; margin: 25px 0; border: 1px solid #bbf7d0;'>
                    <h4 style='margin: 0 0 10px 0; color: #15803d; font-size: 15px;'>Next Steps:</h4>
                    <ul style='margin: 0; padding-left: 20px; color: #15803d; font-size: 14px;'>
                        <li style='margin-bottom: 5px;'>Profile and document verification.</li>
                        <li style='margin-bottom: 5px;'>Email notification upon approval.</li>
                        <li>Direct contact if details are needed.</li>
                    </ul>
                </div>

                <p style='font-size: 15px; margin-bottom:0;'>Thank you for your interest.</p>
            </div>
            {GetFooter("Application currently under process.")}";

            return BaseContainer(body);
        }

        // 5. Vendor Approval Email
        public static string EmailBodyForVendorApproval(string vendorName)
        {
            string body = $@"
                {GetHeader("Registration Approved", SuccessColor)}
                <div style='padding: 30px; background-color: #ffffff;'>
                    <p style='margin-top:0; font-size: 16px;'>Dear <strong>{vendorName}</strong>,</p>
                    <p style='font-size: 15px;'>Congratulations! Your registration request has been <strong>Approved</strong>.</p>
        
                    <div style='background-color: {InfoBgColor}; border-radius: 10px; padding: 20px; border: 1px solid {InfoBorderColor}; margin: 25px 0;'>
                        <p style='font-size: 14px; color: #0369a1; margin: 0;'>
                            <strong>What's Next?</strong><br>
                            You can now log in to the portal using your registered credentials to participate in biddings and manage your profile.
                        </p>
                    </div>

                    <p style='font-size: 15px; margin-bottom:0;'>We appreciate your interest and look forward to your participation.</p>
                </div>
                {GetFooter("Welcome to our Bid network.")}";

            return BaseContainer(body);
        }

        // 6. Vendor Rejection Email
        public static string EmailBodyForVendorRejection(string vendorName)
        {
            string body = $@"
                {GetHeader("Registration Update", "#e11d48")} // Rose/Red Color
                <div style='padding: 30px; background-color: #ffffff;'>
                    <p style='margin-top:0; font-size: 16px;'>Dear <strong>{vendorName}</strong>,</p>
                    <p style='font-size: 15px;'>Thank you for your interest in our Bid Portal.</p>
                    <p style='font-size: 15px;'>After careful review, we regret to inform you that your registration application has been <strong>Rejected</strong> at this time.</p>
        
                    <div style='background-color: #fff1f2; border-radius: 10px; padding: 20px; border: 1px solid #fecdd3; margin: 25px 0;'>
                        <p style='font-size: 14px; color: #9f1239; margin: 0;'>
                            <strong>Note:</strong> If you believe this is a misunderstanding or wish to provide additional documentation, please contact our bid helpdesk.
                        </p>
                    </div>

                    <p style='font-size: 15px; margin-bottom:0;'>Thank you for your time.</p>
                </div>
                {GetFooter("Registration status notification.")}";

            return BaseContainer(body);
        }

        // 6. RFQ Invitation Email
        public static string EmailBodyForRFQRequest(string category, string deadline, string rfqLink)
        {
            string body = $@"
            {GetHeader("Request for Bids", "#1e293b")} 
            <div style='padding: 40px; background-color: #ffffff; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                <p style='margin-top:0; font-size: 16px; color: #1e293b;'>Dear <strong>Valued Client</strong>,</p>
        
                <p style='font-size: 15px; line-height: 1.6; color: #475569;'>
                    We are formally inviting your organization to participate in our bid process. Please find the details for the Bids below:
                </p>

                <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; margin: 25px 0;'>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>Service/Category</td>
                            <td style='padding: 8px 0; color: #0f172a; font-weight: 600; font-size: 15px;'>{category}</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>Submission Deadline</td>
                            <td style='padding: 8px 0; color: #be123c; font-weight: 600; font-size: 15px;'>{deadline}</td>
                        </tr>
                    </table>
                </div>

                <p style='font-size: 15px; color: #475569;'>
                    Detailed specifications and submission guidelines are available through our bid portal. 
                    <strong>Please ensure your financial proposal is submitted before the deadline.</strong>
                </p>

                <div style='text-align: center; margin: 35px 0;'>
                    <a href='{rfqLink}' target='_blank' style='background-color: #2563eb; color: #ffffff; padding: 16px 40px; text-decoration: none; font-size: 15px; font-weight: 600; border-radius: 6px; display: inline-block; transition: background-color 0.3s ease;'>
                        Access Bid Portal
                    </a>
                </div>

                <div style='border-left: 4px solid #f59e0b; background-color: #fffbeb; padding: 16px; margin-top: 30px;'>
                    <p style='font-size: 14px; color: #92400e; margin: 0; line-height: 1.5;'>
                        <strong>Notice:</strong> To maintain fairness in the bidding process, our system automatically restricts submissions after the specified deadline.
                    </p>
                </div>
            </div>
            {GetFooter("This is a formal automated notification from the Bid Department.")}";

            return BaseContainer(body);
        }

        // 7. Vendor Selection Email
        public static string EmailBodyForVendorSelection(string vendorName, string instanceId)
        {
            string body = $@"
            {GetHeader("Selection Confirmation", "#059669")} 
            <div style='padding: 40px; background-color: #ffffff; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>
                <p style='margin-top:0; font-size: 16px; color: #1e293b;'>Dear <strong>{vendorName}</strong>,</p>
        
                <p style='font-size: 15px; line-height: 1.6; color: #475569;'>
                    We are pleased to inform you that your proposal has been <strong>successfully selected</strong> for the project/requirement mentioned below. Congratulations on winning this bid!
                </p>

                <div style='background-color: #f0fdf4; border: 1px solid #bbf7d0; border-radius: 12px; padding: 24px; margin: 25px 0;'>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr>
                            <td style='padding: 8px 0; color: #166534; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>Status</td>
                            <td style='padding: 8px 0; color: #15803d; font-weight: 700; font-size: 15px;'>Winner Selected</td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>Request ID</td>
                            <td style='padding: 8px 0; color: #0f172a; font-weight: 600; font-size: 15px;'>{instanceId}</td>
                        </tr>
                    </table>
                </div>

                <p style='font-size: 15px; color: #475569;'>
                      Your submission has been successfully reviewed and selected. Additional details and updates related to this request will be communicated in due course.
                </p>
            </div>
            {GetFooter("This is an official selection notification from our Bid System.")}";

            return BaseContainer(body);
        }

        // 7. Procurement Move Forward By Console
        public static string EmailBodyForProcurementMoveForward(string InstanceID, string UserName, string category, string deadline, string rfqLink)
        {
            string body = $@"
            {GetHeader("Bid Progress Update", "#1e293b")} 
            <div style='padding: 40px; background-color: #ffffff; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>

                <p style='margin-top:0; font-size: 16px; color: #1e293b;'>
                    Dear <strong>{UserName}</strong>,
                </p>

                <p style='font-size: 15px; line-height: 1.6; color: #475569;'>
                    We are pleased to inform you that submission for the bid has been 
                    <strong style='color:#16a34a;'>successfully forwarded</strong> and request has been 
                    <strong> proceed to the next stage</strong> of our bid process.
                </p>

                <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; margin: 25px 0;'>
                    <table style='width: 100%; border-collapse: collapse;'>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                Request #
                            </td>
                            <td style='padding: 8px 0; color: #0f172a; font-weight: 600; font-size: 15px;'>
                                {InstanceID}
                            </td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                Service/Category
                            </td>
                            <td style='padding: 8px 0; color: #0f172a; font-weight: 600; font-size: 15px;'>
                                {category}
                            </td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                Bank Evaluation
                            </td>
                            <td style='padding: 8px 0; color: #be123c; font-weight: 600; font-size: 15px;'>
                                {deadline}
                            </td>
                        </tr>
                    </table>
                </div>

                <p style='font-size: 15px; color: #475569;'>
                    You are requested to review the updated RFQ details and proceed with the required actions 
                    for the next phase. Please ensure timely submission to avoid disqualification.
                </p>

                <div style='text-align: center; margin: 35px 0;'>
                    <a href='{rfqLink}' target='_blank'
                       style='background-color: #16a34a; color: #ffffff; padding: 16px 40px; text-decoration: none; font-size: 15px; font-weight: 600; border-radius: 6px; display: inline-block;'>
                        Continue to Bid Portal
                    </a>
                </div>

                <div style='border-left: 4px solid #2563eb; background-color: #eff6ff; padding: 16px; margin-top: 30px;'>
                    <p style='font-size: 14px; color: #1e40af; margin: 0; line-height: 1.5;'>
                        <strong>Next Step:</strong> Kindly complete all required actions within the given timeline 
                        to remain eligible for final evaluation.
                    </p>
                </div>

                <p style='margin-top:30px; font-size:14px; color:#64748b;'>
                    Should you have any questions, please feel free to contact our bid team.
                </p>

            </div>
            {GetFooter("This is a formal automated notification regarding your Bid progression.")}";

            return BaseContainer(body);
        }

        // 8. Procurement Move Extended By Console
        public static string EmailBodyForProcurementExtended(string InstanceID, string UserName, string category, string deadline, string rfqLink)
        {
            string body = $@"
            {GetHeader("Bid Deadline Extension Notice", "#1e293b")} 

            <div style='padding: 40px; background-color: #ffffff; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif;'>

                <p style='margin-top:0; font-size: 16px; color: #1e293b;'>
                    Dear <strong>{UserName}</strong>,
                </p>

                <p style='font-size: 15px; line-height: 1.6; color: #475569;'>
                    We would like to inform you that the submission deadline for the bid
                    has been <strong style='color:#f59e0b;'>extended</strong> to allow additional time for preparation and submission of your proposal.
                </p>

                <div style='background-color: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; margin: 25px 0;'>

                    <table style='width: 100%; border-collapse: collapse;'>

                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                Request #
                            </td>
                            <td style='padding: 8px 0; color: #0f172a; font-weight: 600; font-size: 15px;'>
                                {InstanceID}
                            </td>
                        </tr>
                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                Service/Category
                            </td>
                            <td style='padding: 8px 0; color: #0f172a; font-weight: 600; font-size: 15px;'>
                                {category}
                            </td>
                        </tr>

                        <tr>
                            <td style='padding: 8px 0; color: #64748b; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px;'>
                                New Extended Deadline
                            </td>
                            <td style='padding: 8px 0; color: #b91c1c; font-weight: 700; font-size: 15px;'>
                                {deadline}
                            </td>
                        </tr>

                    </table>

                </div>

                <p style='font-size: 15px; color: #475569;'>
                    Please take advantage of this extension to review your proposal and ensure compliance with all RFQ requirements.
                    All other terms and conditions remain unchanged.
                </p>

                <div style='text-align: center; margin: 35px 0;'>
                    <a href='{rfqLink}' target='_blank'
                       style='background-color: #2563eb; color: #ffffff; padding: 16px 40px; text-decoration: none; font-size: 15px; font-weight: 600; border-radius: 6px; display: inline-block;'>
                        Access Bid Portal
                    </a>
                </div>

                <div style='border-left: 4px solid #f59e0b; background-color: #fffbeb; padding: 16px; margin-top: 30px;'>
                    <p style='font-size: 14px; color: #92400e; margin: 0; line-height: 1.5;'>
                        <strong>Important:</strong> All submissions must be completed before the revised deadline.
                        Late submissions will not be considered.
                    </p>
                </div>

            </div>

            {GetFooter("This is a formal notification regarding Bid deadline extension.")}";

            return BaseContainer(body);
        }
    }
}
