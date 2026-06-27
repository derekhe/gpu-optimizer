using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace GpuOptimizer.App.Services;

public sealed class GitHubReleaseUpdateChecker
{
    private static readonly Uri LatestReleaseUri = new("https://api.github.com/repos/derekhe/gpu-optimizer/releases/latest");
    private readonly HttpClient _httpClient;

    public GitHubReleaseUpdateChecker(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
        if (!_httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("GpuOptimizer"))
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("GpuOptimizer");
        }
    }

    public async Task<UpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var currentVersion = GetCurrentVersion();
        var release = await _httpClient.GetFromJsonAsync<GitHubReleaseResponse>(LatestReleaseUri, cancellationToken)
            .ConfigureAwait(false);

        if (release is null ||
            string.IsNullOrWhiteSpace(release.TagName) ||
            !TryParseVersion(release.TagName, out var latestVersion))
        {
            throw new InvalidOperationException("Unable to read the latest release version.");
        }

        return new UpdateCheckResult(
            currentVersion,
            latestVersion,
            release.TagName,
            release.HtmlUrl,
            latestVersion > currentVersion);
    }

    private static Version GetCurrentVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version is null ? new Version(0, 0, 0) : new Version(version.Major, version.Minor, version.Build);
    }

    private static bool TryParseVersion(string tagName, out Version version)
    {
        var normalized = tagName.Trim().TrimStart('v', 'V');
        return Version.TryParse(normalized, out version!);
    }

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; init; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; init; } = string.Empty;
    }
}
