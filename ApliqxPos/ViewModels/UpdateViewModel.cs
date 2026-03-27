using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ApliqxPos.Services;

namespace ApliqxPos.ViewModels;

/// <summary>
/// ViewModel for the Update dialog.
/// </summary>
public partial class UpdateViewModel : ObservableObject
{
    [ObservableProperty] private string _currentVersion = string.Empty;
    [ObservableProperty] private string _newVersion = string.Empty;
    [ObservableProperty] private string _releaseNotes = string.Empty;
    [ObservableProperty] private int _downloadProgress;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private string _statusMessage = string.Empty;

    private readonly string _downloadUrl;

    public UpdateViewModel(string newVersion, string downloadUrl, string releaseNotes)
    {
        _newVersion = newVersion;
        _downloadUrl = downloadUrl;
        _releaseNotes = releaseNotes;
        _currentVersion = $"الإصدار الحالي: {UpdateService.Instance.GetCurrentVersion().ToString(3)}";
    }

    [RelayCommand]
    private async Task DownloadAndInstallAsync()
    {
        IsDownloading = true;
        StatusMessage = "جاري تحميل التحديث...";

        var progress = new Progress<int>(p =>
        {
            DownloadProgress = p;
            StatusMessage = $"جاري التحميل... {p}%";
        });

        var downloadedPath = await UpdateService.Instance.DownloadUpdateAsync(_downloadUrl, progress);

        if (downloadedPath == null)
        {
            StatusMessage = "❌ فشل التحميل. يرجى المحاولة لاحقاً.";
            IsDownloading = false;
            return;
        }

        StatusMessage = "✅ تم التحميل! جاري تطبيق التحديث...";
        await Task.Delay(800);

        UpdateService.Instance.ApplyUpdate(downloadedPath);
    }
}
