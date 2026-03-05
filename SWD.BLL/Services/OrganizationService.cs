using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWD.BLL.Services
{
    public class OrganizationService : IOrganizationService
    {
        private readonly IOrganizationRepository _repository;

        public OrganizationService(IOrganizationRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Organization>> GetAllOrganizationsAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<(List<Organization> Orgs, int TotalCount)> GetAllOrganizationsAsync(string? search, int? pageNumber, int? pageSize, string? sortBy = null, string? sortOrder = "asc")
        {
            var orgs = (await _repository.GetAllAsync(search, sortBy, sortOrder)).ToList();
            var totalCount = orgs.Count;

            if (pageNumber.HasValue && pageSize.HasValue)
            {
                orgs = orgs.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value).ToList();
            }

            return (orgs, totalCount);
        }

        public async Task<Organization?> GetOrganizationByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<Organization> CreateOrganizationAsync(Organization organization)
        {
            return await _repository.AddAsync(organization);
        }

        public async Task UpdateOrganizationAsync(Organization organization)
        {
             await _repository.UpdateAsync(organization);
        }

        public async Task DeleteOrganizationAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }
    }
}
