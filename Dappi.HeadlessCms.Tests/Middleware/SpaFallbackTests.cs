using System.Net;
using System.Net.Http.Headers;
using Dappi.HeadlessCms.Tests.Auth;
using Microsoft.AspNetCore.Hosting;

namespace Dappi.HeadlessCms.Tests.Middleware
{
    public class SpaWebAppFactory : IntegrationWebAppFactory
    {
        public const string IndexHtml = "<html><body>dappi-spa</body></html>";

        private readonly string _webRoot = Directory
            .CreateTempSubdirectory("dappi-webroot-")
            .FullName;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            File.WriteAllText(Path.Combine(_webRoot, "index.html"), IndexHtml);
            base.ConfigureWebHost(builder);
            builder.UseWebRoot(_webRoot);
        }
    }

    public class SpaFallbackTests : IClassFixture<SpaWebAppFactory>
    {
        private readonly HttpClient _client;

        public SpaFallbackTests(SpaWebAppFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/content-type-builder")]
        [InlineData("/settings/users")]
        public async Task Client_Route_Should_Serve_Index_With_Ok(string path)
        {
            var response = await _client.GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(SpaWebAppFactory.IndexHtml, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Head_Client_Route_Should_Return_Ok()
        {
            var response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Head, "/"));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/does-not-exist")]
        [InlineData("/api/auditTrail/")]
        public async Task Unknown_Api_Route_Should_Return_NotFound_Without_Spa(string path)
        {
            var response = await _client.GetAsync(path);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("dappi-spa", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Missing_Static_File_Should_Return_NotFound_Without_Spa()
        {
            var response = await _client.GetAsync("/assets/missing-chunk.js");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.DoesNotContain("dappi-spa", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Non_Get_Request_To_Client_Route_Should_Not_Serve_Spa()
        {
            var response = await _client.PostAsync("/content-type-builder", null);

            Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
            Assert.DoesNotContain("dappi-spa", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Index_File_Should_Still_Be_Served_Directly()
        {
            var response = await _client.GetAsync("/index.html");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(SpaWebAppFactory.IndexHtml, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Api_Responses_Should_Keep_Their_Status_And_Body()
        {
            var unauthorized = await _client.GetAsync("/api/users");
            Assert.Equal(HttpStatusCode.Unauthorized, unauthorized.StatusCode);

            var auth = await _client.Authorize();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                auth?.Token
            );

            var ok = await _client.GetAsync("/api/models");
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
            Assert.Equal("application/json", ok.Content.Headers.ContentType?.MediaType);

            var notFound = await _client.GetAsync($"/api/users/username/{Guid.NewGuid()}");
            Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
            Assert.Contains("User not found", await notFound.Content.ReadAsStringAsync());
        }
    }
}
