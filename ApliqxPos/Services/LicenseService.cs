using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Supabase;
using System.Net.NetworkInformation;

namespace ApliqxPos.Services;

public class LicenseService
{
    private static readonly Lazy<LicenseService> _instance = new(() => new LicenseService());
    public static LicenseService Instance => _instance.Value;

    private readonly string _licenseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.dat");
    private readonly string _supabaseUrl = "https://hdwioskrgvkzwvkbgjqf.supabase.co";
    private readonly string _supabaseKey = "sb_publishable_pCIZJH1eNxu6jiZZOt0nsg_OYVOfHQY";
    
    private Client _supabase;

    private LicenseService()
    {
        var options = new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };
        _supabase = new Client(_supabaseUrl, _supabaseKey, options);
    }

    public async Task InitializeAsync()
    {
        await _supabase.InitializeAsync();
    }

    public bool IsActivated()
    {
        if (!File.Exists(_licenseFile)) return false;

        try
        {
            var encryptedData = File.ReadAllBytes(_licenseFile);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.LocalMachine);
            var licenseData = Encoding.UTF8.GetString(decryptedData);
            
            var parts = licenseData.Split('|');
            if (parts.Length != 2) return false;

            var storedKey = parts[0];
            var storedMachineId = parts[1];

            return storedMachineId == GetMachineId();
        }
        catch
        {
            return false;
        }
    }

    public async Task<(bool Success, string Message, string? OwnerUsername, string? OwnerPasswordHash)> ActivateLicenseAsync(string key, string username, string password)
    {
        try
        {
            await InitializeAsync();

            // 1. Check if key exists and is available
            var response = await _supabase
                .From<LicenseModel>()
                .Select("*") // Select all to check credentials
                .Where(x => x.Key == key)
                .Get();

            var license = response.Models.FirstOrDefault();

            if (license == null)
            {
                return (false, "مفتاح التفعيل غير صحيح", null, null);
            }

            // 2. Verify Credentials
            if (!string.Equals(license.Username, username, StringComparison.OrdinalIgnoreCase))
            {
                return (false, "اسم المستخدم غير صحيح لهذا المفتاح", null, null);
            }

            var inputHash = ComputeSha256Hash(password);
            if (license.PasswordHash != inputHash)
            {
                return (false, "كلمة المرور غير صحيحة لهذا المفتاح", null, null);
            }

            var currentMachineId = GetMachineId();

            // Deserialize DeviceIds
            var deviceList = (license.DeviceIds ?? "").Split('|', StringSplitOptions.RemoveEmptyEntries).ToList();

            // Check if this machine is already activated
            if (deviceList.Contains(currentMachineId))
            {
                SaveLicenseLocally(key, currentMachineId);
                return (true, "تم إعادة تفعيل البرنامج بنجاح (جهاز معروف)", license.Username, license.PasswordHash);
            }

            // If not active on this machine, check if we have slots available
            if (deviceList.Count < license.MaxDevices)
            {
                // Add this machine
                deviceList.Add(currentMachineId);
                var newDeviceIds = string.Join("|", deviceList);

                await _supabase
                    .From<LicenseModel>()
                    .Where(x => x.Key == key)
                    .Set(x => x.IsActive, true)
                    .Set(x => x.DeviceIds, newDeviceIds)
                    .Set(x => x.MachineId, currentMachineId) // Update legacy field with latest
                    .Set(x => x.ActivatedAt, DateTime.UtcNow)
                    .Update();

                SaveLicenseLocally(key, currentMachineId);
                return (true, "تم تفعيل البرنامج بنجاح (جهاز جديد)", license.Username, license.PasswordHash);
            }
            else
            {
                return (false, $"عذراً، تجاوزت الحد الأقصى لعدد الأجهزة المسموح بها ({license.MaxDevices}).", null, null);
            }

            return (false, "حدث خطأ غير متوقع", null, null);
        }
        catch (Exception ex)
        {
            return (false, $"خطأ في الاتصال: {ex.Message}", null, null);
        }
    }

    private void SaveLicenseLocally(string key, string machineId)
    {
        var data = $"{key}|{machineId}";
        var bytes = Encoding.UTF8.GetBytes(data);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);
        File.WriteAllBytes(_licenseFile, encrypted);
    }

    private string GetMachineId()
    {
        // Simple stable ID based on MAC address of first active network interface
        // This is robust enough for basic binding without needing System.Management
        try
        {
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Select(n => n.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            if (string.IsNullOrEmpty(mac)) return "NO-MAC-ADDRESS";
            
            // Hash it to look like a proper ID
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(mac));
            return BitConverter.ToString(hash).Replace("-", "").Substring(0, 20);
        }
        catch
        {
            return "FALLBACK-ID-" + Environment.MachineName;
        }
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

    // Supabase Model
    [Supabase.Postgrest.Attributes.Table("licenses")]
    public class LicenseModel : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("key")]
        public string Key { get; set; }

        [Supabase.Postgrest.Attributes.Column("username")]
        public string Username { get; set; }

        [Supabase.Postgrest.Attributes.Column("password_hash")]
        public string PasswordHash { get; set; }

        [Supabase.Postgrest.Attributes.Column("machine_id")]
        public string MachineId { get; set; }

        [Supabase.Postgrest.Attributes.Column("is_active")]
        public bool IsActive { get; set; }

        [Supabase.Postgrest.Attributes.Column("activated_at")]
        public DateTime? ActivatedAt { get; set; }

        [Supabase.Postgrest.Attributes.Column("max_devices")]
        public int MaxDevices { get; set; } = 1;

        [Supabase.Postgrest.Attributes.Column("device_ids")]
        public string DeviceIds { get; set; }
    }
}
