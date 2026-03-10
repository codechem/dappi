using System.Net;
using System.Reflection;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Dappi.HeadlessCms.Services.MailServices;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Dappi.HeadlessCms.Tests.Services;

public class AmazonSesServiceTests
{
    private readonly IAmazonSimpleEmailService _mockSesClient;
    private readonly AmazonSesService _sut;

    public AmazonSesServiceTests()
    {
        var mockConfig = Substitute.For<IConfiguration>();
        var mockLogger = Substitute.For<ILogger<AmazonSesService>>();
        _mockSesClient = Substitute.For<IAmazonSimpleEmailService>();

        mockConfig.GetSection("AWS:SES:AccessKey").Value.Returns("fake-key");
        mockConfig.GetSection("AWS:SES:SecretKey").Value.Returns("fake-secret");
        mockConfig.GetSection("AWS:SES:SourceEmail").Value.Returns("test@dappi.com");

        _sut = new AmazonSesService(mockConfig, mockLogger);

        var field = typeof(AmazonSesService).GetField(
            "_sesClient",
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        field?.SetValue(_sut, _mockSesClient);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldUseMockedClient()
    {
        var to = new List<string> { "user@test.com" };
        _mockSesClient
            .SendEmailAsync(Arg.Any<SendEmailRequest>())
            .Returns(new SendEmailResponse { MessageId = "mock-id-123" });

        var result = await _sut.SendEmailAsync(to, "html", "text", "subject");

        result.Should().Be("mock-id-123");
        await _mockSesClient
            .Received(1)
            .SendEmailAsync(Arg.Is<SendEmailRequest>(r => r.Source == "test@dappi.com"));
    }

    [Fact]
    public void VerifyEmailIdentityAsync_ShouldReturnTrue_OnSuccess()
    {
        _mockSesClient
            .VerifyEmailIdentityAsync(Arg.Any<VerifyEmailIdentityRequest>())
            .Returns(
                Task.FromResult(
                    new VerifyEmailIdentityResponse { HttpStatusCode = HttpStatusCode.OK }
                )
            );

        var result = _sut.VerifyEmailIdentityAsync("test@test.com");

        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyEmailIdentityAsync_ShouldReturnFalse_OnFailure()
    {
        _mockSesClient
            .VerifyEmailIdentityAsync(Arg.Any<VerifyEmailIdentityRequest>())
            .Returns(
                Task.FromResult(
                    new VerifyEmailIdentityResponse { HttpStatusCode = HttpStatusCode.BadRequest }
                )
            );

        var result = _sut.VerifyEmailIdentityAsync("test@test.com");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateEmailTemplateAsync_ShouldReturnTrue_WhenResponseIsOk()
    {
        _mockSesClient
            .CreateTemplateAsync(Arg.Any<CreateTemplateRequest>())
            .Returns(new CreateTemplateResponse { HttpStatusCode = HttpStatusCode.OK });

        var result = await _sut.CreateEmailTemplateAsync(
            "WelcomeTemplate",
            "Subject",
            "Text body",
            "<html><body>Html body</body></html>"
        );

        result.Should().BeTrue();
        await _mockSesClient
            .Received(1)
            .CreateTemplateAsync(
                Arg.Is<CreateTemplateRequest>(r =>
                    r.Template.TemplateName == "WelcomeTemplate"
                    && r.Template.SubjectPart == "Subject"
                    && r.Template.TextPart == "Text body"
                    && r.Template.HtmlPart == "<html><body>Html body</body></html>"
                )
            );
    }

    [Fact]
    public async Task CreateEmailTemplateAsync_ShouldReturnFalse_WhenResponseIsNotOk()
    {
        _mockSesClient
            .CreateTemplateAsync(Arg.Any<CreateTemplateRequest>())
            .Returns(
                new CreateTemplateResponse { HttpStatusCode = HttpStatusCode.InternalServerError }
            );

        var result = await _sut.CreateEmailTemplateAsync("Fail", "Fail", "Fail", "Fail");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_ShouldReturnMessageId_WhenSuccessful()
    {
        var toAddresses = new List<string> { "customer@test.com" };
        var templateName = "OrderConfirmation";
        var userName = "John Doe";
        var expectedMessageId = "templated-msg-789";

        _mockSesClient
            .SendTemplatedEmailAsync(Arg.Any<SendTemplatedEmailRequest>())
            .Returns(new SendTemplatedEmailResponse { MessageId = expectedMessageId });

        var result = await _sut.SendTemplatedEmailAsync(toAddresses, userName, templateName);

        result.Should().Be(expectedMessageId);
        await _mockSesClient
            .Received(1)
            .SendTemplatedEmailAsync(
                Arg.Is<SendTemplatedEmailRequest>(r =>
                    r.Source == "test@dappi.com"
                    && r.Template == templateName
                    && r.TemplateData == "{ \"name\":\"John Doe\" }"
                    && r.Destination.ToAddresses.Contains("customer@test.com")
                )
            );
    }

    [Fact]
    public async Task SendTemplatedEmailAsync_ShouldPassCorrectTemplateData()
    {
        var userName = "Special Characters & Name";
        _mockSesClient
            .SendTemplatedEmailAsync(Arg.Any<SendTemplatedEmailRequest>())
            .Returns(new SendTemplatedEmailResponse { MessageId = "any-id" });

        await _sut.SendTemplatedEmailAsync(
            new List<string> { "test@test.com" },
            userName,
            "Template"
        );

        await _mockSesClient
            .Received(1)
            .SendTemplatedEmailAsync(
                Arg.Is<SendTemplatedEmailRequest>(r =>
                    r.TemplateData.Contains("\"name\":\"Special Characters & Name\"")
                )
            );
    }
}
