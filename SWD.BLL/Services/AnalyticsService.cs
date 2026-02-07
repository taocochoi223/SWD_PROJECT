using SWD.BLL.Interfaces;
using SWD.DAL.Models;
using SWD.DAL.Repositories.Interfaces;

namespace SWD.BLL.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly ISensorRepository _sensorRepo;

        public AnalyticsService(ISensorRepository sensorRepo)
        {
            _sensorRepo = sensorRepo;
        }

        public async Task<object> GetSummaryAsync(int sensorId, DateTime? from, DateTime? to)
        {
            var fromDate = from?.Date ?? DateTime.Now.AddDays(-7);
            var toDate = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

            var readings = await _sensorRepo.GetReadingsForChartAsync(sensorId, fromDate, toDate);

            if (!readings.Any())
            {
                return new
                {
                    sensorId,
                    from = fromDate,
                    to = toDate,
                    count = 0,
                    message = "No data available"
                };
            }

            var values = readings.Select(r => r.Value).ToList();

            return new
            {
                sensorId,
                from = fromDate,
                to = toDate,
                count = readings.Count,
                min = values.Min(),
                max = values.Max(),
                average = values.Average(),
                median = GetMedian(values),
                firstReading = readings.First().RecordedAt,
                lastReading = readings.Last().RecordedAt
            };
        }

        public async Task<object> GetTrendsAsync(int sensorId, string interval, DateTime? from, DateTime? to)
        {
            var fromDate = from?.Date ?? DateTime.Now.AddDays(-7);
            var toDate = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

            var readings = await _sensorRepo.GetReadingsForChartAsync(sensorId, fromDate, toDate);

            if (!readings.Any())
            {
                return new { sensorId, trends = new List<object>() };
            }
            object trends;

            if (interval.ToLower() == "daily")
            {
                trends = readings
                    .GroupBy(r => r.RecordedAt?.Date)
                    .Select(g => new
                    {
                        date = g.Key,
                        count = g.Count(),
                        min = g.Min(r => r.Value),
                        max = g.Max(r => r.Value),
                        average = g.Average(r => r.Value)
                    })
                    .OrderBy(x => x.date)
                    .ToList();
            }
            else
            {
                trends = readings
                    .GroupBy(r => new { r.RecordedAt?.Date, r.RecordedAt?.Hour })
                    .Select(g => new
                    {
                        date = g.Key.Date,
                        hour = g.Key.Hour,
                        count = g.Count(),
                        min = g.Min(r => r.Value),
                        max = g.Max(r => r.Value),
                        average = g.Average(r => r.Value)
                    })
                    .OrderBy(x => x.date).ThenBy(x => x.hour)
                    .ToList();
            }

            return new
            {
                sensorId,
                interval,
                from = fromDate,
                to = toDate,
                trends
            };
        }

        public async Task<object> CompareSensorsAsync(string sensorIds, DateTime? from, DateTime? to)
        {
            var ids = sensorIds.Split(',').Select(int.Parse).ToList();
            var fromDate = from?.Date ?? DateTime.Now.AddDays(-7);
            var toDate = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

            var comparisons = new List<object>();

            foreach (var id in ids)
            {
                var readings = await _sensorRepo.GetReadingsForChartAsync(id, fromDate, toDate);
                var sensor = await _sensorRepo.GetSensorByIdAsync(id);

                if (readings.Any())
                {
                    var values = readings.Select(r => r.Value).ToList();
                    comparisons.Add(new
                    {
                        sensorId = id,
                        sensorName = sensor?.Name,
                        count = readings.Count,
                        min = values.Min(),
                        max = values.Max(),
                        average = values.Average()
                    });
                }
            }

            return new
            {
                from = fromDate,
                to = toDate,
                sensors = comparisons
            };
        }

        public async Task<object> DetectAnomaliesAsync(int sensorId, DateTime? from, DateTime? to)
        {
            var fromDate = from?.Date ?? DateTime.Now.AddDays(-7);
            var toDate = to?.Date.AddDays(1).AddTicks(-1) ?? DateTime.Now;

            var readings = await _sensorRepo.GetReadingsForChartAsync(sensorId, fromDate, toDate);

            if (!readings.Any())
            {
                return new { sensorId, anomalies = new List<object>() };
            }

            var values = readings.Select(r => r.Value).ToList();
            var mean = values.Average();
            var stdDev = Math.Sqrt(values.Average(v => Math.Pow(v - mean, 2)));

            // Anomaly = giá trị nằm ngoài 2 standard deviations
            var anomalies = readings
                .Where(r => Math.Abs(r.Value - mean) > 2 * stdDev)
                .Select(r => new
                {
                    dataId = r.DataId,
                    value = r.Value,
                    recordedAt = r.RecordedAt,
                    deviation = Math.Abs(r.Value - mean) / stdDev
                })
                .OrderByDescending(a => a.deviation)
                .ToList();

            return new
            {
                sensorId,
                from = fromDate,
                to = toDate,
                mean,
                standardDeviation = stdDev,
                anomalyCount = anomalies.Count,
                anomalies
            };
        }

        private double GetMedian(List<double> values)
        {
            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;
            if (count % 2 == 0)
            {
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2.0;
            }
            return sorted[count / 2];
        }
    }
}