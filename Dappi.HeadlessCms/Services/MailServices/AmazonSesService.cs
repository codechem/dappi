using System.Net;
using Amazon.Internal;
using Dappi.HeadlessCms.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dappi.HeadlessCms.Services.MailServices;

using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;

public class AmazonSesService : IEmailService
{
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AmazonSesService> _logger;

    public AmazonSesService(IConfiguration configuration, ILogger<AmazonSesService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var sesAccessKey = configuration.GetSection("AWS:SES:AccessKey").Value;
        var sesSecretKey = configuration.GetSection("AWS:SES:SecretKey").Value;

        _sesClient = new AmazonSimpleEmailServiceClient(sesAccessKey, sesSecretKey);
        ;
    }

    public async Task<string> SendEmailAsync(
        List<string> toAddresses,
        string bodyHtml,
        string bodyText,
        string subject
    )
    {
        var messageId = "";

        var sendRequest = new SendEmailRequest
        {
            Destination = new Destination { ToAddresses = toAddresses },
            Message = new Message
            {
                Body = new Body
                {
                    Html = new Content { Charset = "UTF-8", Data = bodyHtml },
                    Text = new Content { Charset = "UTF-8", Data = bodyText },
                },
                Subject = new Content { Charset = "UTF-8", Data = subject },
            },
            Source = _configuration.GetSection("AWS:SES:SourceEmail").Value,
        };

        try
        {
            var response = await _sesClient.SendEmailAsync(sendRequest);
            messageId = response.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }

        return messageId;
    }

    public async Task<bool> VerifyEmailIdentityAsync(string mail)
    {
        var success = false;
        try
        {
            var response = _sesClient
                .VerifyEmailIdentityAsync(new VerifyEmailIdentityRequest { EmailAddress = mail })
                .Result;

            success = response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }

        return success;
    }

    public async Task<bool> CreateEmailTemplateAsync(
        string name,
        string subject,
        string text,
        string html
    )
    {
        var success = false;

        var createTemplateRequest = new CreateTemplateRequest
        {
            Template = new Template
            {
                TemplateName = name,
                SubjectPart = subject,
                TextPart = text,
                HtmlPart = html,
            },
        };

        try
        {
            var response = await _sesClient.CreateTemplateAsync(createTemplateRequest);
            success = response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }

        return success;
    }

    public async Task<string> SendTemplatedEmailAsync(string toAddress, string userName)
    {
        var sourceEmail = _configuration.GetSection("AWS:SES:SourceEmail").Value;

        var sendTemplatedEmailRequest = new SendTemplatedEmailRequest
        {
            Source = sourceEmail,
            Destination = new Destination { ToAddresses = new List<string> { toAddress } },
            // Template is the actual name of the template created inside the Amazon SES Console
            // be careful, this is case-sensitive
            // Template = templateNameFromConsole
            TemplateData = $"{{ \"userName\":\"{userName}\" }}",
        };

        var messageId = "";
        try
        {
            var response = await _sesClient.SendTemplatedEmailAsync(sendTemplatedEmailRequest);
            messageId = response.MessageId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.ToString());
        }

        return messageId;
    }
}
