using Microsoft.EntityFrameworkCore;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.DAL.Repositories.Implementations
{
    /// <summary>
    /// Repository implementation for User management and authentication
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly IoTFinalDbContext _context;

        public UserRepository(IoTFinalDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Site)
                .Include(u => u.Role)
                .Include(u => u.Org)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Site)
                .Include(u => u.Role)
                .Include(u => u.Org)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.Site)
                .Include(u => u.Role)
                .Include(u => u.Org)
                .ToListAsync();
        }

        public async Task<List<User>> GetAllUsersAsync(string? search, bool? isActive, string? sortBy = null, string? sortOrder = "asc")
        {
            var query = _context.Users
                .Include(u => u.Site)
                .Include(u => u.Role)
                .Include(u => u.Org)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(u =>
                    (u.FullName != null && u.FullName.ToLower().Contains(searchLower)) ||
                    (u.Email != null && u.Email.ToLower().Contains(searchLower)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            bool isDesc = sortOrder?.ToLower() == "desc";
            query = sortBy?.ToLower() switch {
                "fullname" => isDesc ? query.OrderByDescending(u => u.FullName) : query.OrderBy(u => u.FullName),
                "email"    => isDesc ? query.OrderByDescending(u => u.Email)    : query.OrderBy(u => u.Email),
                "isactive" => isDesc ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
                "roleid"   => isDesc ? query.OrderByDescending(u => u.RoleId)   : query.OrderBy(u => u.RoleId),
                _          => isDesc ? query.OrderByDescending(u => u.UserId)   : query.OrderBy(u => u.UserId)
            };

            return await query.ToListAsync();
        }

        public async Task AddUserAsync(User user)
        {
            await _context.Users.AddAsync(user);
        }

        public Task UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            return Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
