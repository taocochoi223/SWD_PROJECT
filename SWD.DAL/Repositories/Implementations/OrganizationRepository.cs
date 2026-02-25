using Microsoft.EntityFrameworkCore;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SWD.DAL.Repositories.Implementations
{
    public class OrganizationRepository : IOrganizationRepository
    {
        private readonly IoTFinalDbContext _context;

        public OrganizationRepository(IoTFinalDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Organization>> GetAllAsync()
        {
            return await _context.Organizations
                .Include(o => o.Sites) 
                .ToListAsync();
        }

        public async Task<IEnumerable<Organization>> GetAllAsync(string? search)
        {
            var query = _context.Organizations
                .Include(o => o.Sites)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(o =>
                    (o.Name != null && o.Name.ToLower().Contains(searchLower)) ||
                    (o.Description != null && o.Description.ToLower().Contains(searchLower)));
            }

            return await query.ToListAsync();
        }

        public async Task<Organization?> GetByIdAsync(int id)
        {
            return await _context.Organizations
                .Include(o => o.Sites)
                .FirstOrDefaultAsync(o => o.OrgId == id);
        }

        public async Task<Organization> AddAsync(Organization organization)
        {
            _context.Organizations.Add(organization);
            await _context.SaveChangesAsync();
            return organization;
        }

        public async Task UpdateAsync(Organization organization)
        {
            _context.Organizations.Update(organization);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var organization = await _context.Organizations.FindAsync(id);
            if (organization != null)
            {
                _context.Organizations.Remove(organization);
                await _context.SaveChangesAsync();
            }
        }
    }
}
