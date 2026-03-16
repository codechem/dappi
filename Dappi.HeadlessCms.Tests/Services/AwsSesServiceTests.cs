using System.Net;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Dappi.HeadlessCms.Interfaces;
using Dappi.HeadlessCms.Services.MailServices;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Dappi.HeadlessCms.Tests.Services;

public class AwsSesServiceTests
{
    private readonly IConfiguration _mockConfig;
    private readonly ILogger<AwsSesService> _mockLogger;
    private readonly IAmazonSimpleEmailService _mockSesClient;
    private readonly ISesClientFactory _mockFactory;
    private readonly AwsSesService _sut;

    public AwsSesServiceTests()
    {
        _mockConfig = Substitute.For<IConfiguration>();
        _mockLogger = Substitute.For<ILogger<AwsSesService>>();
        _mockSesClient = Substitute.For<IAmazonSimpleEmailService>();
        _mockFactory = Substitute.For<ISesClientFactory>();

        _mockFactory.CreateClient().Returns(_mockSesClient);

        _mockConfig.GetSection("AWS:SES:SourceEmail").Value.Returns("test@dappi.com");

        _sut = new AwsSesService(_mockConfig, _mockLogger, _mockFactory);
    }

    [Fact]
    public async Task SendEmailAsync_ShouldUseMockedClient_AndReturnMessageId()
    {
        var to = new List<string> { "user@test.com" };
        var expectedId = "mock-id-123";
        _mockSesClient
            .SendEmailAsync(Arg.Any<SendEmailRequest>())
            .Returns(new SendEmailResponse { MessageId = expectedId });

        var result = await _sut.SendEmailAsync(to, "<html></html>", "text", "subject");

        result.Should().Be(expectedId);
        await _mockSesClient
            .Received(1)
            .SendEmailAsync(
                Arg.Is<SendEmailRequest>(r =>
                    r.Source == "test@dappi.com"
                    && r.Destination.ToAddresses.Contains("user@test.com")
                )
            );
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
            new { name = "User" },
            "Subject",
            "Text body",
            "<html><body>Default HTML</body></html>"
        );

        result.Should().BeTrue();
        await _mockSesClient
            .Received(1)
            .CreateTemplateAsync(
                Arg.Is<CreateTemplateRequest>(r =>
                    r.Template.TemplateName == "WelcomeTemplate"
                    && r.Template.SubjectPart == "Subject"
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

        var result = await _sut.CreateEmailTemplateAsync("Fail", new { name = "User" }, "Fail", "Fail", "Fail");

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
