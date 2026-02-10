namespace XPlayer.Core.Models
{
    public interface IClientProfile
    {
        string ClientName { get; }
        string ClientVersion { get; }
        string DeviceName { get; }
        string DeviceId { get; }
    }
}
