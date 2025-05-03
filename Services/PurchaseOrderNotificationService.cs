using CompanyManagementSystem.Data;
using CompanyManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace CompanyManagementSystem.Services
{
    public class PurchaseOrderNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PurchaseOrderNotificationService> _logger;

        public PurchaseOrderNotificationService(
            IServiceScopeFactory scopeFactory,
            ILogger<PurchaseOrderNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendNotifications();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking purchase order notifications");
                }

                // Check every minute
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task CheckAndSendNotifications()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var now = DateTime.Now;
            var purchaseOrders = await dbContext.PurchaseOrders
                .Include(po => po.Company)
                .Where(po => po.NotificationTime <= now && 
                            !string.IsNullOrEmpty(po.NotificationEmail))
                .ToListAsync();

            foreach (var order in purchaseOrders)
            {
                try
                {
                    var emailBody = GenerateEmailBody(order);
                    await emailService.SendEmailAsync(
                        order.NotificationEmail!,
                        $"Purchase Order Notification - Order #{order.Id}",
                        emailBody
                    );
                    _logger.LogInformation($"Sent notification email for purchase order {order.Id}");

                    // Clear the notification time to prevent resending
                    order.NotificationTime = null;
                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send notification email for purchase order {order.Id}");
                }
            }
        }

        private string GenerateEmailBody(PurchaseOrder order)
        {
            return $@"
                <html>
                <body>
                    <h2>Purchase Order Notification</h2>
                    <p>Your purchase order has been created and is being processed:</p>
                    <ul>
                        <li><strong>Order ID:</strong> {order.Id}</li>
                        <li><strong>Company:</strong> {order.Company?.Name ?? "N/A"}</li>
                        <li><strong>Order Date:</strong> {order.OrderDate:d}</li>
                        <li><strong>Total Amount:</strong> ${order.TotalAmount:N2}</li>
                    </ul>
                    <p>Please review the order details and contact us if you have any questions.</p>
                </body>
                </html>";
        }
    }
}