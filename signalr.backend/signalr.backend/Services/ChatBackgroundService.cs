using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Common;
using signalr.backend.Data;
using signalr.backend.Hubs;

namespace signalr.backend.Services
{
    public class ChatBackgroundService : BackgroundService
    {
        public const int DELAY = 30 * 1000;
        private readonly IHubContext<ChatHub> _monHub;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ChatBackgroundService(IHubContext<ChatHub> monHub, IServiceScopeFactory serviceScopeFactory)
        {
            _monHub = monHub;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task DoSomething(CancellationToken stoppingToken)
        {
            using (IServiceScope scope = _serviceScopeFactory.CreateScope())
            {
                ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Trouver le canal le plus populaire
                var mostPopular = dbContext.Channel
                    .OrderByDescending(c => c.NbMessages)
                    .FirstOrDefault();

                if (mostPopular == null)
                    return;

                // Message envoyé à tous les clients
                string message = $"Most popular channel: {mostPopular.Title} ({mostPopular.NbMessages} messages)";
                await _monHub.Clients.All.SendAsync("MostPopularChannel", message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(DELAY, stoppingToken);
                    await DoSomething(stoppingToken);
                }
            }
    }
}
