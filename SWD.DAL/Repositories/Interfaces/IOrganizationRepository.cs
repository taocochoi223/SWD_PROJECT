using SWD.DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWD.DAL.Repositories.Interfaces
{
    public interface IOrganizationRepository
    {
        Task<IEnumerable<Organization>> GetAllAsync();
        Task<IEnumerable<Organization>> GetAllAsync(string? search);
        Task<Organization?> GetByIdAsync(int id);
        Task<Organization> AddAsync(Organization organization);
        Task UpdateAsync(Organization organization);
        Task DeleteAsync(int id);
    }
}
