using System.Net.Http.Json;

namespace Dappi.HeadlessCms.Tests.Auth
{
    public static class AuthHandler
    {
        public static async Task<AuthTestModel?> Authorize(this HttpClient client)
        {
            var auth = await client.PostAsJsonAsync("/api/Auth/login" , new {Username = "Admin", Password = "Dappi@123"});
            auth.EnsureSuccessStatusCode();
            return await auth.Content.ReadFromJsonAsync<AuthTestModel>();
        }
    }
}