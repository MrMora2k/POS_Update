using Octokit;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;

namespace ApliqxPos.Services;

/// <summary>
/// Handles checking and applying updates from GitHub Releases.
/// </summary>
public class UpdateService
{
    private static UpdateService? _instance;
    public static UpdateService Instance => _instance ??= new UpdateService();

    private const string GitHubOwner = "MrMora2k";
    private const string GitHubRepo = "POS_Update";
    private const string AppName = "ApliqxPos"; // Name of the .exe in GitHub Assets

    private readonly GitHubClient _gitHubClient;

    public UpdateService()
    {
        _gitHubClient = new GitHubClient(new ProductHeaderValue(AppName));
    }

    /// <summary>
    /// Gets the current application version.
    /// </summary>
    public Version GetCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);
    }

    /// <summary>
    /// Checks GitHub Releases for a newer version. Returns the release if one exists, null otherwise.
    /// </summary>
    public async Task<(bool HasUpdate, string? NewVersion, string? DownloadUrl, string? ReleaseNotes)> CheckForUpdateAsync()
    {
        try
        {
            var latestRelease = await _gitHubClient.Repository.Release.GetLatest(GitHubOwner, GitHubRepo);

            // GitHub tags usually start with 'v' (e.g., v1.2.0), strip it
            var tagName = latestRelease.TagName.TrimStart('v');

            if (!Version.TryParse(tagName, out var latestVersion))
                return (false, null, null, null);

            var currentVersion = GetCurrentVersion();
            
            if (latestVersion > currentVersion)
            {
                // Find the .exe asset in the release
                var exeAsset = latestRelease.Assets
                    .FirstOrDefault(a => a.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase));

                return (true, latestRelease.TagName, exeAsset?.BrowserDownloadUrl, latestRelease.Body);
            }

            return (false, null, null, null);
        }
        catch
        {
            // No internet or repo not found - silently fail
            return (false, null, null, null);
        }
    }

    /// <summary>
    /// Downloads the new version .exe from GitHub and returns the path to the downloaded file.
    /// </summary>
    public async Task<string?> DownloadUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"ApliqxPos_Update.exe");

            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd(AppName);

            using var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            var buffer = new byte[8192];
            var totalRead = 0L;

            await using var stream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(tempPath, System.IO.FileMode.Create, FileAccess.Write, FileShare.None);

            int read;
            while ((read = await stream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, read));
                totalRead += read;
                if (progress != null && totalBytes > 0)
                {
                    progress.Report((int)(totalRead * 100 / totalBytes));
                }
            }

            return tempPath;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Applies the downloaded update by creating a batch script, then closing the app.
    /// The script replaces the current .exe with the new one and restarts the app.
    /// </summary>
    public void ApplyUpdate(string downloadedFilePath)
    {
        var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (currentExePath == null) return;

        var batPath = Path.Combine(Path.GetTempPath(), "apliqxpos_updater.bat");

        // The batch script:
        // 1. Waits 2 seconds for the app to fully close
        // 2. Copies the new file over the old one
        // 3. Restarts the app
        // 4. Deletes itself
        var batContent = $@"@echo off
timeout /t 2 /nobreak > nul
copy /y ""{downloadedFilePath}"" ""{currentExePath}"" > nul
start """" ""{currentExePath}""
del ""{batPath}""
";
        File.WriteAllText(batPath, batContent);

        // Launch the updater script and exit the app
        Process.Start(new ProcessStartInfo
        {
            FileName = batPath,
            WindowStyle = ProcessWindowStyle.Hidden,
            CreateNoWindow = true
        });

        System.Windows.Application.Current.Shutdown();
    }
}
