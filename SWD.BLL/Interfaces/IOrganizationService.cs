using SWD.DAL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWD.BLL.Interfaces
{
    public interface IOrganizationService
    {
        Task<IEnumerable<Organization>> GetAllOrganizationsAsync();
        Task<(List<Organization> Orgs, int TotalCount)> GetAllOrganizationsAsync(string? search, int? pageNumber, int? pageSize);
        Task<Organization?> GetOrganizationByIdAsync(int id);
        Task<Organization> CreateOrganizationAsync(Organization organization);
        Task UpdateOrganizationAsync(Organization organization);
        Task DeleteOrganizationAsync(int id);
    }
}
