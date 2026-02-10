using XPlayer.Core.Models;

namespace XPlayer.Core.Network
{
    public interface IAuthenticationService
    {
        bool IsAuthenticated { get; }
        string? CurrentUserId { get; }
        string? AccessToken { get; }
        IUserInfo? CurrentUser { get; }

        Task<bool> AuthenticateByNameAsync(string username, string password, CancellationToken cancellationToken = default);
        Task<bool> AuthenticateByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<IUserInfo?> GetCurrentUserAsync(CancellationToken cancellationToken = default);
        Task LogoutAsync(CancellationToken cancellationToken = default);
    }
}
