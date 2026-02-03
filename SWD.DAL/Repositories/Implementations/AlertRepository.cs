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

        public async Task<List<AlertRule>> GetAllRulesAsync()
        {
            return await _context.AlertRules
                .Include(r => r.Sensor)
                .ToListAsync();
        }

        public async Task<List<AlertRule>> GetActiveRulesBySensorIdAsync(int sensorId)
        {
            return await _context.AlertRules
                .Where(r => r.SensorId == sensorId && r.IsActive == true)
                .ToListAsync();
        }

        public async Task CreateRuleAsync(AlertRule rule)
        {
            await _context.AlertRules.AddAsync(rule);
        }

        public async Task AddAlertHistoryAsync(AlertHistory history)
        {
            await _context.AlertHistories.AddAsync(history);
        }

        public async Task<List<AlertHistory>> GetAlertHistoryAsync(int? sensorId, DateTime? from, DateTime? to)
        {
            var query = _context.AlertHistories
                .Include(h => h.Sensor)
                .Include(h => h.Rule)
                .AsQueryable();

            if (sensorId.HasValue)
                query = query.Where(h => h.SensorId == sensorId);

            if (from.HasValue)
                query = query.Where(h => h.TriggeredAt >= from);

            if (to.HasValue)
                query = query.Where(h => h.TriggeredAt <= to);

            return await query
                .OrderByDescending(h => h.TriggeredAt)
                .ToListAsync();
        }

        public async Task<AlertHistory?> GetAlertHistoryByIdAsync(long historyId)
        {
            return await _context.AlertHistories
                .Include(h => h.Sensor)
                .Include(h => h.Rule)
                .FirstOrDefaultAsync(h => h.HistoryId == historyId);
        }

        public async Task<List<AlertHistory>> GetAlertHistoryWithFiltersAsync(string? status, string? search)
        {
            var query = _context.AlertHistories
                .Include(h => h.Sensor)
                .Include(h => h.Rule)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "All")
            {
                if (status == "Active")
                    query = query.Where(h => h.ResolvedAt == null);
                else if (status == "Resolved")
                    query = query.Where(h => h.ResolvedAt != null);
            }

            if (!string.IsNullOrEmpty(search))
                query = query.Where(h => h.Sensor != null && h.Sensor.Name != null && h.Sensor.Name.Contains(search));

            return await query.OrderByDescending(h => h.TriggeredAt).ToListAsync();
        }

        public Task UpdateAlertHistoryAsync(AlertHistory history)
        {
            _context.AlertHistories.Update(history);
            return Task.CompletedTask;
        }

        public async Task DeleteAlertHistoryAsync(long historyId)
        {
            var history = await _context.AlertHistories.FindAsync(historyId);
            if (history != null)
                _context.AlertHistories.Remove(history);
        }

        public async Task<AlertRule?> GetRuleByIdAsync(int ruleId)
        {
            return await _context.AlertRules
                .Include(r => r.Sensor)
                .FirstOrDefaultAsync(r => r.RuleId == ruleId);
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
                _context.AlertRules.Remove(rule);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
