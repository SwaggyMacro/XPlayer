namespace XPlayer.Core.Models;

/// <summary>
/// 用户信息
/// </summary>
public interface IUserInfo
{
    string Id { get; }
    string Name { get; }
    string? ServerName { get; }
    string? AvatarUrl { get; }
    bool HasPrimaryImage { get; }  // true if user actually has an avatar set
    DateTime? LastLoginDate { get; }
    DateTime? LastActivityDate { get; }
    bool HasPassword { get; }
    string? Policy { get; } // "Admin", "User", etc.
}