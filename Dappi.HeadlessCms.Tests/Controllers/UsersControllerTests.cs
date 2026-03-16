using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dappi.HeadlessCms.Models;
using Dappi.HeadlessCms.Tests.Auth;

namespace Dappi.HeadlessCms.Tests.Controllers;

public class UsersControllerTests : BaseIntegrationTestFixture
{
    private readonly HttpClient _client;

    public UsersControllerTests(IntegrationWebAppFactory factory)
        : base(factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InviteUser_Should_Assign_Default_User_Role_When_Roles_Are_Empty()
    {
        var auth = await _client.Authorize();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth?.Token);

        var uniqueUsername = Guid.NewGuid().ToString();
        var request = new
        {
            Username = uniqueUsername,
            Email = $"{uniqueUsername}@test.com",
            Password = "Dappi@123",
            Roles = Array.Empty<string>(),
        };

        var response = await _client.PostAsJsonAsync("/api/users", request);
        response.EnsureSuccessStatusCode();

        var createdUserResponse = await _client.GetAsync($"/api/users/username/{uniqueUsername}");
        createdUserResponse.EnsureSuccessStatusCode();

        var createdUser = await createdUserResponse.Content.ReadFromJsonAsync<UserRoleDto>();

        Assert.NotNull(createdUser);
        Assert.Contains("User", createdUser!.Roles);
        Assert.False(createdUser.AcceptedInvitation);
    }
}
