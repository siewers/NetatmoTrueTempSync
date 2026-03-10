using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace NetatmoTrueTempSync.Services;

public static class UpdateService
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/siewers/NetatmoTrueTempSync/releases/latest";

    public static string GetExpectedAssetName()
    {
        var rid = RuntimeInformation.RuntimeIdentifier;
        return $"netatmo-truetempsync-{rid}";
    }

    public static string? GetCurrentVersion()
    {
        return Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString();
    }

    public static async Task<(string? TagName, string? AssetUrl)?> CheckForUpdateAsync(HttpClient client, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
        request.Headers.UserAgent.ParseAdd("NetatmoTrueTempSync");

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tagName = root.GetProperty("tag_name").GetString();
        var assetName = GetExpectedAssetName();

        if (root.TryGetProperty("assets", out var assets))
        {
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase))
                {
                    var downloadUrl = asset.GetProperty("browser_download_url").GetString();
                    return (tagName, downloadUrl);
                }
            }
        }

        return (tagName, null);
    }

    public static async Task DownloadAndReplaceAsync(HttpClient client, string assetUrl, string targetPath, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, assetUrl);
        request.Headers.UserAgent.ParseAdd("NetatmoTrueTempSync");

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tempPath = targetPath + ".update";

        try
        {
            await using (var stream = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var file = File.Create(tempPath))
            {
                await stream.CopyToAsync(file, cancellationToken);
            }

            File.Move(tempPath, targetPath, overwrite: true);

            // Preserve executable permissions on Unix
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(targetPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                                  UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                                  UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }
}
