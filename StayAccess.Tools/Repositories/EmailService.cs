using Microsoft.Extensions.Options;
using SendGrid;
using SendGrid.Helpers.Mail;
using StayAccess.DTO.Email;
using StayAccess.Tools.Extensions;
using StayAccess.Tools.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StayAccess.Tools.Repositories
{
    public class EmailService : IEmailService
    {
        private readonly EmailConfiguration _emailConfiguration;
        private readonly ILogService _logService;
        private readonly IEmailLoggerService _emailLoggerService;

        public EmailService(IOptions<EmailConfiguration> emailConfiguration, ILogService logService, IEmailLoggerService emailLoggerService)
        {
            _emailConfiguration = emailConfiguration.Value;
            _logService = logService;
            _emailLoggerService = emailLoggerService;
        }

        /// <summary>
        /// send email message
        /// </summary>
        /// <param name="emailMessage"></param>
        public async Task<bool> SendAsync(EmailMessage emailMessage, List<string> recipients = null, int? reservationId = null)
        {
            try
            {
                var client = new SendGridClient(_emailConfiguration.SendGrid_Key);
                var from = new EmailAddress(_emailConfiguration.SendGrid_From_Email);
                SendGridMessage message = null;

                recipients = recipients.HasAny() ? recipients : _emailConfiguration.SendGrid_To_Email.Split(',').ToList();

                if (recipients.HasAny())
                {
                    List<EmailAddress> tos = new List<EmailAddress>();

                    foreach (var recipient in recipients)
                    {
                        if (!string.IsNullOrEmpty(recipient))
                            tos.Add(new EmailAddress(recipient.Trim()));
                    }

                    message = MailHelper.CreateSingleEmailToMultipleRecipients(from, tos, emailMessage.Subject, emailMessage.Message, emailMessage.Message);
                }

                var result = await client.SendEmailAsync(message);

                if (result.IsSuccessStatusCode)
                    _emailLoggerService.Add(emailMessage.ToJsonString(), recipients.ToJsonString(), DTO.Enums.EmailStatus.Success, reservationId, "");
                else
                    _emailLoggerService.Add(emailMessage.ToJsonString(), recipients.ToJsonString(), DTO.Enums.EmailStatus.Failed, reservationId, result.ToJsonString());

                return result.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _emailLoggerService.Add(emailMessage.ToJsonString(), recipients.ToJsonString(), DTO.Enums.EmailStatus.Failed, reservationId, ex.ToJsonString());
                throw;
            }
        }

        public string GetListAsHtmlTableString<T>(List<T> list = null, T singleObject = default)
        {
            try
            {
                _logService.LogMessage($"In EmailService GetListAsHtmlTableString." +
                    $" list: {list.ToJsonString()}." +
                    $" singleObject: {singleObject.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), DTO.Enums.LogType.Information);

                if (list == null && singleObject == null)
                    return "";


                StringBuilder sb = new();
                sb.Append($"<table style=\"background: black;\">");
                PropertyInfo[] propertyInfos = typeof(T)?.GetProperties()?.Where(x => x?.PropertyType?.IsSealed == true).OrderBy(x => x?.MetadataToken)?.ToArray(); //property names of object for table column names
                sb.Append("<tr>");
                foreach (PropertyInfo prop in propertyInfos)
                {
                    //table header
                    sb.Append($"<th style=\"background: #f1f1f1;\" >{prop.Name}</th>");//#d6ddd6;
                }
                sb.Append("</tr>");

                string tdBackgroundColor = " white ";

                if (list != null)
                {
                    foreach (var item in list)
                    {
                        sb.Append("<tr>");
                        //table body
                        foreach (PropertyInfo prop in propertyInfos)
                        {
                            sb.Append($"<td style=\"background: {tdBackgroundColor};\" >{prop.GetValue(item)}</td>");//#f1f1f1
                        }
                        sb.Append("</tr>");
                    }
                }
                else if (singleObject != null)
                {
                    sb.Append("<tr>");
                    //table body
                    foreach (PropertyInfo prop in propertyInfos)
                    {
                        sb.Append($"<td style=\"background: {tdBackgroundColor};\" >{prop.GetValue(singleObject)}</td>");//#f1f1f1
                    }
                    sb.Append("</tr>");
                }

                sb.Append($"</table><br/>");
                return sb.ToString();

            }
            catch (Exception ex)
            {
                _logService.LogMessage($"Error occurred in EmailService GetListAsHtmlTableString." +
                                       $" list: {list.ToJsonString()}." +
                                       $" singleObject: {singleObject.ToJsonString()}." +
                                       $" Exception: {ex.ToJsonString()}.", null, false, Utilities.GetCurrentTimeInEST(), DTO.Enums.LogType.Error);
                throw;
            }
        }
    }
}