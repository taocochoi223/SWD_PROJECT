using Microsoft.EntityFrameworkCore;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.DAL.Repositories.Implementations
{
    public class AlertRepository : IAlertRepository
    {
        private readonly IoTFinalDbContext _context;

        public AlertRepository(IoTFinalDbContext context)
        {
            _context = context;
        }

        public async Task<List<AlertRule>> GetActiveRulesBySensorIdAsync(int sensorId)
        {
            return await _context.AlertRules
                .Where(r => r.SensorId == sensorId && r.IsActive == true)
                .ToListAsync();
        }

        public async Task<List<AlertRule>> GetAllRulesAsync()
        {
            return await _context.AlertRules
                .Include(r => r.Sensor)
                .ThenInclude(s => s.Hub)
                .ThenInclude(h => h.Site)
                .ToListAsync();
        }

        public async Task<List<AlertRule>> GetAllRulesAsync(string? search, bool? isActive, string? priority)
        {
            var query = _context.AlertRules
                .Include(r => r.Sensor)
                .ThenInclude(s => s.Hub)
                .ThenInclude(h => h.Site)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();
                query = query.Where(r =>
                    (r.Name != null && r.Name.ToLower().Contains(searchLower)) ||
                    (r.Sensor != null && r.Sensor.Name != null && r.Sensor.Name.ToLower().Contains(searchLower)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(r => r.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                var priorityLower = priority.Trim().ToLower();
                query = query.Where(r => r.Priority != null && r.Priority.ToLower() == priorityLower);
            }

            return await query.ToListAsync();
        }

        public async Task CreateRuleAsync(AlertRule rule)
        {
            await _context.AlertRules.AddAsync(rule);
        }

        public async Task<AlertRule?> GetRuleByIdAsync(int ruleId)
        {
            return await _context.AlertRules.FindAsync(ruleId);
        }

        public Task UpdateRuleAsync(AlertRule rule)
        {
            _context.AlertRules.Update(rule);
            return Task.CompletedTask;
        }

        public async Task DeleteRuleAsync(int ruleId)
        {
            var rule = await _context.AlertRules.FindAsync(ruleId);
            if (rule != null)
            {
                _context.AlertRules.Remove(rule);
            }
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
