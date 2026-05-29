using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using _10x_cookbook_backend.Data;

namespace _10x_cookbook_backend.Services
{
    public class DataRetentionService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DataRetentionService> _logger;
        private readonly IConfiguration _configuration;

        public DataRetentionService(IServiceScopeFactory scopeFactory, ILogger<DataRetentionService> logger, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var settings = _configuration.GetSection("DataRetentionSettings");
            var intervalHours = settings.GetValue<int>("CleanupIntervalHours", 24);
            var retentionMonths = settings.GetValue<int>("RetentionPeriodMonths", 24);

            if (intervalHours <= 0)
            {
                _logger.LogWarning("Invalid CleanupIntervalHours value configured: {Value}. Falling back to 24 hours.", intervalHours);
                intervalHours = 24;
            }

            if (retentionMonths <= 0)
            {
                _logger.LogWarning("Invalid RetentionPeriodMonths value configured: {Value}. Falling back to 24 months.", retentionMonths);
                retentionMonths = 24;
            }

            _logger.LogInformation("DataRetentionService initialized. Interval: {Hours}h, Retention: {Months}m", intervalHours, retentionMonths);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                        var cutoff = DateTime.UtcNow.AddMonths(-retentionMonths);
                        
                        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
                        {
                            var inactiveUsers = dbContext.Users
                                .Where(u => u.LastActive < cutoff)
                                .ToList();

                            if (inactiveUsers.Any())
                            {
                                _logger.LogInformation("Found {Count} inactive users older than {Cutoff} (InMemory). Purging...", inactiveUsers.Count, cutoff);
                                dbContext.Users.RemoveRange(inactiveUsers);
                                await dbContext.SaveChangesAsync(stoppingToken);
                                _logger.LogInformation("Successfully purged {Count} inactive users.", inactiveUsers.Count);
                            }
                        }
                        else
                        {
                            var deletedCount = await dbContext.Users
                                .Where(u => u.LastActive < cutoff)
                                .ExecuteDeleteAsync(stoppingToken);

                            if (deletedCount > 0)
                            {
                                _logger.LogInformation("Successfully purged {Count} inactive users older than {Cutoff}.", deletedCount, cutoff);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during data retention cleanup execution.");
                }

                await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
            }
        }
    }
}
