using System.Collections.ObjectModel;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace ApliqxKeyGen;

public partial class MainWindow : Window
{
    private readonly string _firebaseUrl = "https://pos-lic-default-rtdb.europe-west1.firebasedatabase.app";
    private readonly HttpClient _httpClient = new();

    public ObservableCollection<LicenseModel> Licenses { get; set; } = new ObservableCollection<LicenseModel>();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        LoadLicensesAsync();
    }

    private async void LoadLicensesAsync()
    {
        try
        {
            LoadingBar.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "Loading licenses...";

            var response = await _httpClient.GetAsync($"{_firebaseUrl}/licenses.json");
            var json = await response.Content.ReadAsStringAsync();

            Licenses.Clear();

            if (response.IsSuccessStatusCode && json != "null" && !string.IsNullOrWhiteSpace(json))
            {
                var licensesDict = JsonSerializer.Deserialize<Dictionary<string, LicenseModel>>(json);
                if (licensesDict != null)
                {
                    // Sort by CreatedAt descending
                    var sorted = licensesDict
                        .Select(kvp =>
                        {
                            kvp.Value.Key = kvp.Key;
                            return kvp.Value;
                        })
                        .OrderByDescending(l => l.CreatedAt)
                        .ToList();

                    foreach (var license in sorted)
                    {
                        Licenses.Add(license);
                    }
                }
            }

            StatusTextBlock.Text = $"Loaded {Licenses.Count} licenses.";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Black;
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error loading data: {ex.Message}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
        finally
        {
            LoadingBar.Visibility = Visibility.Collapsed;
        }
    }

    private void GenerateKey_Click(object sender, RoutedEventArgs e)
    {
        var key = $"{GenerateSegment()}-{GenerateSegment()}-{GenerateSegment()}-{GenerateSegment()}";
        KeyTextBox.Text = key;
    }

    private string GenerateSegment()
    {
        var random = new Random();
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, 4)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    private string? _editingLicenseKey = null;
    private bool _isSyncing = false;

    private async void CreateLicense_Click(object sender, RoutedEventArgs e)
    {
        var key = KeyTextBox.Text.Trim();
        var username = UsernameTextBox.Text.Trim();
        var password = ShowPasswordToggle.IsChecked == true ? VisiblePasswordBox.Text : PasswordBox.Password;

        if (!int.TryParse(MaxDevicesTextBox.Text, out int maxDevices) || maxDevices < 1)
        {
            StatusTextBlock.Text = "Please enter a valid number for Max Devices.";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }

        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            StatusTextBlock.Text = "Please fill all fields.";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            return;
        }

        CreateButton.IsEnabled = false;
        LoadingBar.Visibility = Visibility.Visible;
        StatusTextBlock.Text = _editingLicenseKey == null ? "Creating license..." : "Updating license...";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Black;

        try
        {
            var passwordHash = ComputeSha256Hash(password);

            if (_editingLicenseKey == null)
            {
                // Create Mode - PUT new license
                var model = new LicenseModel
                {
                    Key = key,
                    Username = username,
                    PasswordHash = passwordHash,
                    PlainPassword = password,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow.ToString("o"),
                    MaxDevices = maxDevices,
                    DeviceIds = ""
                };

                var jsonData = JsonSerializer.Serialize(model);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                await _httpClient.PutAsync($"{_firebaseUrl}/licenses/{key}.json", content);
                StatusTextBlock.Text = "License Created Successfully!";
            }
            else
            {
                // Update Mode - PATCH existing license
                var updateData = new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["passwordHash"] = passwordHash,
                    ["plainPassword"] = password,
                    ["maxDevices"] = maxDevices
                };

                var jsonData = JsonSerializer.Serialize(updateData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                await _httpClient.PatchAsync($"{_firebaseUrl}/licenses/{_editingLicenseKey}.json", content);
                StatusTextBlock.Text = "License Updated Successfully!";
            }

            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;

            ResetForm();
            LoadLicensesAsync();
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = $"Error: {ex.Message}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
        }
        finally
        {
            CreateButton.IsEnabled = true;
            LoadingBar.Visibility = Visibility.Collapsed;
        }
    }

    private void CopyDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is LicenseModel license)
        {
            var text = $"Key: {license.Key}\nUsername: {license.Username}\nPassword: {license.PlainPassword}";
            Clipboard.SetText(text);
            StatusTextBlock.Text = "Details copied to clipboard!";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Black;
        }
    }

    private void EditLicense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is LicenseModel license)
        {
            _editingLicenseKey = license.Key;

            KeyTextBox.Text = license.Key;
            KeyTextBox.IsReadOnly = true;

            UsernameTextBox.Text = license.Username;
            PasswordBox.Password = license.PlainPassword;
            MaxDevicesTextBox.Text = license.MaxDevices.ToString();

            CreateButton.Content = "Update License";
            CancelEditButton.Visibility = Visibility.Visible;
            ResetActivationButton.Visibility = Visibility.Visible;

            StatusTextBlock.Text = $"Editing license: {license.Key}";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Blue;
        }
    }

    private async void DeleteLicense_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is LicenseModel license)
        {
            var result = MessageBox.Show($"Are you sure you want to delete license {license.Key}?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    LoadingBar.Visibility = Visibility.Visible;

                    // Firebase DELETE
                    await _httpClient.DeleteAsync($"{_firebaseUrl}/licenses/{license.Key}.json");

                    LoadLicensesAsync();
                    StatusTextBlock.Text = "License deleted successfully.";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = $"Error deleting: {ex.Message}";
                    StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                }
                finally
                {
                    LoadingBar.Visibility = Visibility.Collapsed;
                }
            }
        }
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        ResetForm();
        StatusTextBlock.Text = "Edit cancelled.";
        StatusTextBlock.Foreground = System.Windows.Media.Brushes.Black;
    }

    private async void ResetActivation_Click(object sender, RoutedEventArgs e)
    {
        if (_editingLicenseKey == null) return;

        var result = MessageBox.Show($"Are you sure you want to reset activations for license {_editingLicenseKey}?", "Confirm Reset", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                LoadingBar.Visibility = Visibility.Visible;

                // PATCH to reset activation fields
                var updateData = new Dictionary<string, object>
                {
                    ["isActive"] = false,
                    ["deviceIds"] = "",
                    ["machineId"] = ""
                };

                var jsonData = JsonSerializer.Serialize(updateData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                await _httpClient.PatchAsync($"{_firebaseUrl}/licenses/{_editingLicenseKey}.json", content);

                StatusTextBlock.Text = "Activations reset successfully.";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;

                ResetForm();
                LoadLicensesAsync();
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Error resetting: {ex.Message}";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
            }
            finally
            {
                LoadingBar.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void ResetForm()
    {
        _editingLicenseKey = null;
        KeyTextBox.Clear();
        KeyTextBox.IsReadOnly = false;
        UsernameTextBox.Clear();
        PasswordBox.Clear();
        VisiblePasswordBox.Clear();
        MaxDevicesTextBox.Text = "1";

        CreateButton.Content = "Create License";
        CancelEditButton.Visibility = Visibility.Collapsed;
        ResetActivationButton.Visibility = Visibility.Collapsed;
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        VisiblePasswordBox.Text = PasswordBox.Password;
        _isSyncing = false;
    }

    private void VisiblePasswordBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_isSyncing) return;
        _isSyncing = true;
        PasswordBox.Password = VisiblePasswordBox.Text;
        _isSyncing = false;
    }

    private void ShowPasswordToggle_Checked(object sender, RoutedEventArgs e)
    {
        PasswordBox.Visibility = Visibility.Collapsed;
        VisiblePasswordBox.Visibility = Visibility.Visible;
        VisiblePasswordBox.Focus();
    }

    private void ShowPasswordToggle_Unchecked(object sender, RoutedEventArgs e)
    {
        PasswordBox.Visibility = Visibility.Visible;
        VisiblePasswordBox.Visibility = Visibility.Collapsed;
        PasswordBox.Focus();
    }

    private void Refresh_Click(object sender, RoutedEventArgs e)
    {
        LoadLicensesAsync();
    }

    private static string ComputeSha256Hash(string rawData)
    {
        using SHA256 sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }

    // Firebase JSON Model
    public class LicenseModel
    {
        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;

        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("plainPassword")]
        public string PlainPassword { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("maxDevices")]
        public int MaxDevices { get; set; } = 1;

        [JsonPropertyName("deviceIds")]
        public string DeviceIds { get; set; } = string.Empty;

        [JsonPropertyName("machineId")]
        public string MachineId { get; set; } = string.Empty;

        [JsonPropertyName("activatedAt")]
        public string? ActivatedAt { get; set; }
    }
}