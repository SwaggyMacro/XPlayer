using System.Net.Http.Json;
using System.Text.Json;
using XPlayer.Core.Models;
using XPlayer.Core.Network;

namespace XPlayer.Providers.Jellyfin
{
    public class JellyfinAuthenticationService : IAuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IClientProfile _clientProfile;

        public JellyfinAuthenticationService(HttpClient httpClient, IClientProfile clientProfile)
        {
            _httpClient = httpClient;
            _clientProfile = clientProfile;
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
        public string? CurrentUserId { get; private set; }
        public string? AccessToken { get; private set; }
        public IUserInfo? CurrentUser { get; private set; }

        public async Task<bool> AuthenticateByNameAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var request = new
            {
                Username = username,
                Pw = password
            };

            var authHeader = $"MediaBrowser Client=\"{_clientProfile.ClientName}\", Device=\"{_clientProfile.DeviceName}\", DeviceId=\"{_clientProfile.DeviceId}\", Version=\"{_clientProfile.ClientVersion}\"";
            _httpClient.DefaultRequestHeaders.Remove("X-Emby-Authorization");
            _httpClient.DefaultRequestHeaders.Add("X-Emby-Authorization", authHeader);

            var response = await _httpClient.PostAsJsonAsync("Users/AuthenticateByName", request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
                
                if (result.TryGetProperty("SessionInfo", out var sessionInfo))
                {
                    if (sessionInfo.TryGetProperty("UserId", out var userId))
                    {
                        CurrentUserId = userId.GetString();
                    }
                }
                
                if (result.TryGetProperty("AccessToken", out var token))
                {
                    AccessToken = token.GetString();
                }

                // Parse user info from auth response
                if (result.TryGetProperty("User", out var userJson))
                {
                    CurrentUser = ParseUserInfo(userJson);
                }

                if (!string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(CurrentUserId))
                {
                    _httpClient.DefaultRequestHeaders.Remove("X-Emby-Authorization");
                    _httpClient.DefaultRequestHeaders.Add("X-Emby-Authorization", $"{authHeader}, Token=\"{AccessToken}\"");
                    return true;
                }
            }

            return false;
        }

        public async Task<IUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            if (!IsAuthenticated || CurrentUserId == null) return null;

            try
            {
                var result = await _httpClient.GetFromJsonAsync<JsonElement>($"Users/{CurrentUserId}", cancellationToken);
                CurrentUser = ParseUserInfo(result);
                return CurrentUser;
            }
            catch
            {
                return null;
            }
        }

        public Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            AccessToken = null;
            CurrentUserId = null;
            CurrentUser = null;
            _httpClient.DefaultRequestHeaders.Remove("X-Emby-Authorization");
            return Task.CompletedTask;
        }

        private IUserInfo ParseUserInfo(JsonElement user)
        {
            var id = user.GetProperty("Id").GetString() ?? "";
            var name = user.GetProperty("Name").GetString() ?? "";
            var serverName = user.TryGetProperty("ServerName", out var sn) ? sn.GetString() : null;
            var lastLogin = user.TryGetProperty("LastLoginDate", out var ll) ? ll.GetDateTime() : (DateTime?)null;
            var lastActivity = user.TryGetProperty("LastActivityDate", out var la) ? la.GetDateTime() : (DateTime?)null;
            var hasPassword = user.TryGetProperty("HasPassword", out var hp) && hp.GetBoolean();

            // Check if user is admin
            string? policy = null;
            if (user.TryGetProperty("Policy", out var pol) && pol.TryGetProperty("IsAdministrator", out var isAdmin))
            {
                policy = isAdmin.GetBoolean() ? "Admin" : "User";
            }

            // Avatar URL — always provide it, tag is optional (used for caching)
            string? avatarUrl;
            var hasImage = user.TryGetProperty("PrimaryImageTag", out var imgTag) 
                           && imgTag.ValueKind == JsonValueKind.String 
                           && !string.IsNullOrEmpty(imgTag.GetString());
            
            if (hasImage)
            {
                avatarUrl = $"{_httpClient.BaseAddress}Users/{id}/Images/Primary?tag={imgTag.GetString()}";
            }
            else
            {
                // Even without a tag, the endpoint may still work if user has an avatar
                avatarUrl = $"{_httpClient.BaseAddress}Users/{id}/Images/Primary";
            }

            return new JellyfinUserInfo(id, name, serverName, avatarUrl, hasImage, lastLogin, lastActivity, hasPassword, policy);
        }
    }

    public record JellyfinUserInfo(
        string Id,
        string Name,
        string? ServerName,
        string? AvatarUrl,
        bool HasPrimaryImage,
        DateTime? LastLoginDate,
        DateTime? LastActivityDate,
        bool HasPassword,
        string? Policy
    ) : IUserInfo;
}
