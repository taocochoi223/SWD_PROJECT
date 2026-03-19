using SWD.DAL.Models;

namespace SWD.BLL.Interfaces
{
    /// <summary>
    /// Service for User management and authentication operations
    /// </summary>
    public interface IUserService
    {
        Task<User?> AuthenticateUserAsync(string email, string password);
        Task<List<User>> GetAllUsersAsync();
        Task<(List<User> Users, int TotalCount)> GetAllUsersAsync(string? search, bool? isActive, int? pageNumber, int? pageSize, string? sortBy = null, string? sortOrder = "asc", int? siteId = null);
        Task<User?> GetUserByIdAsync(int userId);
        Task<User?> GetUserByEmailAsync(string email);
        Task RegisterUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task DeactivateUserAsync(int userId);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}
