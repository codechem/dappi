namespace Dappi.HeadlessCms.Models;

public class DappiGitOptions
{
    public const string Section = "Dappi:Git";

    public string? RepoUrl { get; set; }
    public string? Token { get; set; }
    public string Branch { get; set; } = "main";

    public bool IsEnabled =>
        !string.IsNullOrEmpty(Token)
        && Uri.TryCreate(RepoUrl, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
}
