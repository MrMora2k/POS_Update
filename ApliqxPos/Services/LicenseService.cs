using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Net.NetworkInformation;

namespace ApliqxPos.Services;

public class LicenseService
{
    private static readonly Lazy<LicenseService> _instance = new(() => new LicenseService());
    public static LicenseService Instance => _instance.Value;

    private readonly string _licenseFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ApliqxPos", "license.dat");
    private readonly string _firebaseUrl = "https://pos-lic-default-rtdb.europe-west1.firebasedatabase.app";
    
    private readonly HttpClient _httpClient;

    private LicenseService()
    {
        _httpClient = new HttpClient();
    }

    public Task InitializeAsync()
    {
        // No initialization needed for Firebase REST API
        return Task.CompletedTask;
    }

    public bool IsActivated()
    {
        if (!File.Exists(_licenseFile)) return false;

        try
        {
            var encryptedData = File.ReadAllBytes(_licenseFile);
            var decryptedData = ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser);
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
            // 1. Fetch license from Firebase by key
            var url = $"{_firebaseUrl}/licenses/{Uri.EscapeDataString(key.Trim())}.json";
            var response = await _httpClient.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return (false, $"خطأ في الاتصال بالخادم: {response.StatusCode}", null, null);
            }

            if (json == "null" || string.IsNullOrWhiteSpace(json))
            {
                return (false, "مفتاح التفعيل غير موجود في قاعدة البيانات", null, null);
            }

            var license = JsonSerializer.Deserialize<FirebaseLicenseModel>(json);
            if (license == null)
            {
                return (false, $"خطأ في تحليل البيانات", null, null);
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

                // Update Firebase with PATCH
                var updateData = new Dictionary<string, object>
                {
                    ["isActive"] = true,
                    ["deviceIds"] = newDeviceIds,
                    ["machineId"] = currentMachineId,
                    ["activatedAt"] = DateTime.UtcNow.ToString("o")
                };

                var updateJson = JsonSerializer.Serialize(updateData);
                var content = new StringContent(updateJson, Encoding.UTF8, "application/json");
                await _httpClient.PatchAsync($"{_firebaseUrl}/licenses/{key}.json", content);

                SaveLicenseLocally(key, currentMachineId);
                return (true, "تم تفعيل البرنامج بنجاح (جهاز جديد)", license.Username, license.PasswordHash);
            }
            else
            {
                return (false, $"عذراً، تجاوزت الحد الأقصى لعدد الأجهزة المسموح بها ({license.MaxDevices}).", null, null);
            }
        }
        catch (Exception ex)
        {
            return (false, $"خطأ في الاتصال: {ex.Message}", null, null);
        }
    }

    private void SaveLicenseLocally(string key, string machineId)
    {
        var dir = Path.GetDirectoryName(_licenseFile);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var data = $"{key}|{machineId}";
        var bytes = Encoding.UTF8.GetBytes(data);
        var encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_licenseFile, encrypted);
    }

    private string GetMachineId()
    {
        try
        {
            var mac = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.GetPhysicalAddress().GetAddressBytes().Length > 0)
                .OrderBy(n => n.Id)
                .Select(n => n.GetPhysicalAddress().ToString())
                .FirstOrDefault();

            if (string.IsNullOrEmpty(mac)) 
            {
                mac = Environment.MachineName;
            }
            
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
        using SHA256 sha256Hash = SHA256.Create();
        byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            builder.Append(bytes[i].ToString("x2"));
        }
        return builder.ToString();
    }

    // Firebase JSON Model (System.Text.Json)
    public class FirebaseLicenseModel
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;

        [JsonPropertyName("plainPassword")]
        public string PlainPassword { get; set; } = string.Empty;

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("machineId")]
        public string MachineId { get; set; } = string.Empty;

        [JsonPropertyName("activatedAt")]
        public string? ActivatedAt { get; set; }

        [JsonPropertyName("createdAt")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("maxDevices")]
        public int MaxDevices { get; set; } = 1;

        [JsonPropertyName("deviceIds")]
        public string DeviceIds { get; set; } = string.Empty;
    }
}
