using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Supabase;

namespace ApliqxKeyGen;

public partial class MainWindow : Window
{
    private readonly string _supabaseUrl = "https://hdwioskrgvkzwvkbgjqf.supabase.co";
    private readonly string _supabaseKey = "sb_publishable_pCIZJH1eNxu6jiZZOt0nsg_OYVOfHQY";
    private Client _supabase = null!;

    public ObservableCollection<LicenseModel> Licenses { get; set; } = new ObservableCollection<LicenseModel>();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        InitializeSupabase();
        LoadLicensesAsync();
    }

    private void InitializeSupabase()
    {
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };
        _supabase = new Client(_supabaseUrl, _supabaseKey, options);
    }

    private async void LoadLicensesAsync()
    {
        try
        {
            LoadingBar.Visibility = Visibility.Visible;
            StatusTextBlock.Text = "Loading licenses...";
            
            await _supabase.InitializeAsync();
            
            var response = await _supabase.From<LicenseModel>()
                .Select("*")
                .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                .Get();

            Licenses.Clear();
            foreach (var license in response.Models)
            {
                Licenses.Add(license);
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
        // Simple 4-part key
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
        // Ensure we get the latest password
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
            await _supabase.InitializeAsync();

            // Hash Password
            var passwordHash = ComputeSha256Hash(password);

            if (_editingLicenseKey == null)
            {
                // Create Mode
                var model = new LicenseModel
                {
                    Key = key,
                    Username = username,
                    PasswordHash = passwordHash,
                    PlainPassword = password,
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    MaxDevices = maxDevices
                };
                await _supabase.From<LicenseModel>().Insert(model);
                StatusTextBlock.Text = "License Created Successfully!";
            }
            else
            {
                // Update Mode
                await _supabase.From<LicenseModel>()
                    .Where(x => x.Key == _editingLicenseKey)
                    .Set(x => x.Username, username)
                    .Set(x => x.PasswordHash, passwordHash)
                    .Set(x => x.PlainPassword, password)
                    .Set(x => x.MaxDevices, maxDevices)
                    .Update();
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
                    await _supabase.InitializeAsync();
                    
                    // Supabase Delete (returns void/Task in this version)
                    await _supabase.From<LicenseModel>().Where(x => x.Key == license.Key).Delete();
                    
                    // Verify Deletion by attempting to fetch the record
                    var check = await _supabase.From<LicenseModel>().Where(x => x.Key == license.Key).Get();
                    
                    if (check.Models.Count > 0)
                    {
                        throw new Exception("لم يتم حذف الترخيص. تحقق من صلاحيات قاعدة البيانات (RLS Policy).");
                    }

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
                await _supabase.InitializeAsync();

                // Clear formatting and set IsActive to false
                await _supabase.From<LicenseModel>()
                    .Where(x => x.Key == _editingLicenseKey)
                    .Set(x => x.IsActive, false)
                    .Set(x => x.DeviceIds, string.Empty)
                    // We don't need to set MachineId to null if we removed it from the model, 
                    // but if the column still exists in DB it's fine. 
                    // Postgrest ignores columns not in model usually, or we should have it in model?
                    // Let's assume we depend on DeviceIds now.
                    .Update();

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
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }

    [Supabase.Postgrest.Attributes.Table("licenses")]
    public class LicenseModel : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("key", true)]
        public string Key { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("username")]
        public string Username { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("password_hash")]
        public string PasswordHash { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("plain_password")]
        public string PlainPassword { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("is_active")]
        public bool IsActive { get; set; }

        [Supabase.Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Supabase.Postgrest.Attributes.Column("max_devices")]
        public int MaxDevices { get; set; } = 1;

        [Supabase.Postgrest.Attributes.Column("device_ids")]
        public string DeviceIds { get; set; } = string.Empty;
    }
}