using SWD.DAL.Models;

namespace SWD.DAL.Repositories.Interfaces
{
    /// <summary>
    /// Repository for User management and authentication
    /// </summary>
    public interface IUserRepository
    {
        Task<User?> GetUserByEmailAsync(string email);
        Task<User?> GetUserByIdAsync(int userId);
        Task<List<User>> GetAllUsersAsync();
        Task<List<User>> GetAllUsersAsync(string? search, bool? isActive, string? sortBy = null, string? sortOrder = "asc", int? siteId = null);
        Task AddUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task SaveChangesAsync();
    }
}
