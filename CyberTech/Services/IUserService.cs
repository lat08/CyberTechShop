using CyberTech.Models;

namespace CyberTech.Services
{
    public interface IUserService
    {
        Task<bool> RegisterAsync(string name, string username, string email, string password);
        Task<(bool Success, string ErrorMessage, User User)> AuthenticateAsync(string email, string password);
        Task<bool> GeneratePasswordResetTokenAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
        Task<bool> IsEmailTakenAsync(string email);
        Task<bool> IsUsernameTakenAsync(string username);
        Task<User> GetUserByEmailAsync(string email);
        Task<(bool Success, string ErrorMessage, User User)> ExternalLoginAsync(string provider, string providerId, string email, string name);
        Task<User> GetUserByProviderAsync(string provider, string providerId);
        Task<bool> UpdateProfileImageAsync(int userId, string imageUrl);
        Task<UserAuthMethod> GetUserAuthMethodAsync(int userId, string provider);
        Task<bool> AddAuthMethodAsync(int userId, string provider, string providerId);
        Task<bool> CanResetPasswordAsync(string email);
        Task<bool> HasPasswordAuthAsync(string email);
        Task<bool> IsExternalAccountAsync(string email);
        Task<bool> ValidatePasswordResetTokenAsync(string token);
        Task<bool> InvalidatePasswordResetTokenAsync(string token);
        Task<bool> IsPasswordResetTokenExpiredAsync(string token);
        Task<int> GetPasswordResetAttemptsAsync(string email);
    }
}