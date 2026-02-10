using System;
using System.IO;
using XPlayer.Core.Models;

namespace XPlayer.Desktop.Services.Media;

public class DesktopClientProfile : IClientProfile
{
    private readonly string _deviceId;

    public DesktopClientProfile()
    {
        // Try to load persisted device ID, or generate a new one
        var deviceIdPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, ".device_id");
        
        if (System.IO.File.Exists(deviceIdPath))
        {
            _deviceId = System.IO.File.ReadAllText(deviceIdPath).Trim();
        }
        else
        {
            _deviceId = Guid.NewGuid().ToString();
            try { System.IO.File.WriteAllText(deviceIdPath, _deviceId); }
            catch { /* Ignore write failure */ }
        }
    }

    public string ClientName => "XPlayer";
    public string ClientVersion => typeof(DesktopClientProfile).Assembly.GetName().Version?.ToString() ?? "1.0.0";
    public string DeviceName => Environment.MachineName;
    public string DeviceId => _deviceId;
}
