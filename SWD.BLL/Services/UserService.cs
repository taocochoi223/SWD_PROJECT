using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;
using BCrypt.Net;

namespace SWD.BLL.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;

        public UserService(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        public async Task<User?> AuthenticateUserAsync(string email, string password)
        {
            var user = await _userRepo.GetUserByEmailAsync(email);
            if (user == null || user.IsActive == false)
            {
                return null;
            }
            
            if (BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return user;
            }
            return null;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _userRepo.GetAllUsersAsync();
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            return await _userRepo.GetUserByIdAsync(userId);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepo.GetUserByEmailAsync(email);
        }

        public async Task RegisterUserAsync(User user)
        {
            await _userRepo.AddUserAsync(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user)
        {
            await _userRepo.UpdateUserAsync(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task DeactivateUserAsync(int userId)
        {
            var user = await _userRepo.GetUserByIdAsync(userId);
            if (user != null)
            {
                user.IsActive = false;
                await _userRepo.UpdateUserAsync(user);
                await _userRepo.SaveChangesAsync();
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword){
            var user = await _userRepo.GetUserByIdAsync(userId);
            if(user == null || user.IsActive == false){
                return false;
            }

            if(!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash)){  
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userRepo.UpdateUserAsync(user);
            await _userRepo.SaveChangesAsync();
            return true;
        }
    }
}
