using Octokit;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
                // Find the .zip asset in the release instead of .exe
                var zipAsset = latestRelease.Assets
                    .FirstOrDefault(a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

                return (true, latestRelease.TagName, zipAsset?.BrowserDownloadUrl, latestRelease.Body);
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
    /// Downloads the new version .zip from GitHub and returns the path to the downloaded file.
    /// </summary>
    public async Task<string?> DownloadUpdateAsync(string downloadUrl, IProgress<int>? progress = null)
    {
        try
        {
            var tempPath = Path.Combine(Path.GetTempPath(), "ApliqxPos_Update.zip");

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
    /// Extracts the downloaded ZIP and creates a batch script to replace current files and restart.
    /// </summary>
    public void ApplyUpdate(string downloadedZipPath)
    {
        var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
        var currentDir = Path.GetDirectoryName(currentExePath);
        if (currentExePath == null || currentDir == null) return;

        // Path to extract the ZIP files
        var extractPath = Path.Combine(Path.GetTempPath(), "ApliqxPos_ExtractedUpdate");
        
        try
        {
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
            
            // Extract downloaded ZIP to the temp folder
            ZipFile.ExtractToDirectory(downloadedZipPath, extractPath, overwriteFiles: true);
        }
        catch
        {
            // If extraction fails, abort update
            return;
        }

        var batPath = Path.Combine(Path.GetTempPath(), "apliqxpos_updater.bat");

        // The batch script:
        // 1. Waits 2 seconds for the app to fully close
        // 2. Copies the extracted files to the application directory (overwriting old files)
        // 3. Restarts the app
        // 4. Deletes itself and cleanup
        var batContent = $@"@echo off
timeout /t 2 /nobreak > nul
xcopy /y /s /e /c ""{extractPath}\*"" ""{currentDir}\"" > nul
start """" ""{currentExePath}""
del /q ""{downloadedZipPath}""
rmdir /s /q ""{extractPath}""
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
