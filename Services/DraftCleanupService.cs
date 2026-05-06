/*
    DraftCleanupService.cs, a background service that periodically (every 12 hours) cleans up draft articles older than 12 hours.

    Going to /Admin/Articles/Create, creates a empty draft article in the db with IsDraft = true, this is then set to false when the user actually posts/creates the article.
    So if the user abandons the draft, we will have a empty draft article in the db that we want to clean up.
*/
using devjamin.Models;

namespace devjamin.Services
{
    public class DraftCleanupService(IServiceProvider serviceProvider, ILogger<DraftCleanupService> logger) : IHostedService, IDisposable
    {
        private Timer? _timer;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly ILogger<DraftCleanupService> _logger = logger;

        // Start background service by initializing timer
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Draft cleanup service started.");

            // Schedule the DoWork method to run every 12 hours
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(12));

            return Task.CompletedTask;
        }

        // The method that gets called by the timer.
        private void DoWork(object? state)
        {
            _logger.LogInformation("Draft cleanup task initiated.");

            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BlogDbContext>();

            var timeThreshold = DateTime.UtcNow.AddHours(-12);

            // Fetch all draft articles that were edited more than 12 hours ago.
            var oldDrafts = dbContext.BlogPosts
                                     .Where(bp => bp.IsDraft && bp.EditedDate <= timeThreshold)
                                     .ToList();

            // Check if there are any to delete
            if (oldDrafts.Count != 0)
            {
                _logger.LogInformation("Deleting {OldDraftsCount} old drafts.", oldDrafts.Count);
                dbContext.BlogPosts.RemoveRange(oldDrafts);
                dbContext.SaveChanges();
            }
            else
            {
                _logger.LogInformation("No old drafts found for deletion.");
            }
        }

        // Stops background service and disposes timer
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Draft cleanup service stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing DraftCleanupService resources.");
            GC.SuppressFinalize(this); // We don't need to call the finalizer
        }
    }
}
