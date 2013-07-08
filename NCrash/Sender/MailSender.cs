using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Xml.Serialization;
using Common.Logging;
using NCrash.Core;

namespace NCrash.Sender
{
    public class MailSender : ISender
    {
        private static readonly ILog Logger = LogManager.GetCurrentClassLogger();

        public MailSender()
        {
            UseSsl = false;
            Port = int.MinValue;
        }

		public string From { get; set; }

		public string FromName { get; set; }

		public string To { get; set; }

		public string Cc { get; set; }

		public string Bcc { get; set; }

		public string ReplyTo { get; set; }

		public bool UseAttachment { get; set; }

		public string CustomSubject { get; set; }

		public string CustomBody { get; set; }

		public string SmtpServer { get; set; }

		public bool UseSsl { get; set; }

		public int Port { get; set; }

		public string Priority { get; set; }

		public bool UseAuthentication { get; set; }

		public string Username { get; set; }

		public string Password { get; set; }

        public bool Send(Stream data, string fileName, Report report)
        {
            Logger.Trace("Submitting bug report via email.");
            if (data == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(From) || string.IsNullOrEmpty(To))
            {
                Logger.Warn("Empty mail 'From' or 'To' field");
                return false;
            }

            if (string.IsNullOrEmpty(ReplyTo))
            {
                ReplyTo = From;
            }

            if (Port == int.MinValue)
            {
                Port = UseSsl ? 465 : 25;
            }

            // Make sure that we can use authentication even with emtpy username and password
            if (!string.IsNullOrEmpty(Username))
            {
                UseAuthentication = true;
            }

            using (var smtpClient = new SmtpClient())
            using (var message = new MailMessage())
            {
                if (!string.IsNullOrEmpty(SmtpServer))
                {
                    smtpClient.Host = SmtpServer;
                }

                if (UseAuthentication)
                {
                    smtpClient.Credentials = new NetworkCredential(Username, Password);
                }

                if (UseSsl)
                {
                    smtpClient.EnableSsl = true;
                }

                if (!string.IsNullOrEmpty(Cc))
                {
                    message.CC.Add(Cc);
                }

                if (!string.IsNullOrEmpty(Bcc))
                {
                    message.Bcc.Add(Bcc);
                }

                if (!string.IsNullOrEmpty(Priority))
                {
                    switch (Priority.ToLower())
                    {
                        case "high":
                            message.Priority = MailPriority.High;
                            break;
                        case "normal":
                            message.Priority = MailPriority.Normal;
                            break;
                        case "low":
                            message.Priority = MailPriority.Low;
                            break;
                    }
                }

                message.To.Add(To);
                message.ReplyToList.Add(ReplyTo);
                message.From = !string.IsNullOrEmpty(FromName) ? new MailAddress(From, FromName) : new MailAddress(From);

                if (UseAttachment)
                {
                    // ToDo: Report file name should be attached to the report file object itself, file shouldn't be accessed directly!
                    data.Position = 0;
                    message.Attachments.Add(new Attachment(data, Path.GetFileName(fileName)));
                }

                if (!string.IsNullOrEmpty(CustomSubject))
                {
                    message.Subject = CustomSubject;
                }
                else
                {
                    message.Subject = "NCrash: " + report.GeneralInfo.HostApplication + " (" +
                                      report.GeneralInfo.HostApplicationVersion + "): " +
                                      report.GeneralInfo.ExceptionType + " @ " +
                                      report.GeneralInfo.TargetSite;
                }

                var sb = new StringBuilder();
                if (!string.IsNullOrEmpty(CustomBody))
                {
                    sb.Append(CustomBody).Append(Environment.NewLine).Append(Environment.NewLine);
                }
                WriteReport(report, sb);
                sb.Append(Environment.NewLine).Append(Environment.NewLine);
                WriteException(report.Exception, sb);
                message.Body = sb.ToString();

                smtpClient.Send(message);
                Logger.Trace("Submitted bug report email to: " + To);

                return true;
            }
        }

        private void WriteReport(Report report, StringBuilder sb)
        {
            var serializer = report.CustomInfo != null
                             ? new XmlSerializer(typeof(Report), new[] { report.CustomInfo.GetType() })
                             : new XmlSerializer(typeof(Report));
            TextWriter writer = new StringWriter(sb);
            serializer.Serialize(writer, report);
        }

        private void WriteException(SerializableException exception, StringBuilder sb)
        {
            var serializer = new XmlSerializer(typeof(SerializableException));
            TextWriter writer = new StringWriter(sb);
            serializer.Serialize(writer, exception);
        }
    }
}
