using BLL.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebHostRazor.BackgroundJobs;

public class ContractExpiryReminderHostedService : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<ContractExpiryReminderHostedService> _logger;

    public ContractExpiryReminderHostedService(
        IServiceProvider sp,
        IWebHostEnvironment env,
        ILogger<ContractExpiryReminderHostedService> logger)
    {
        _sp = sp;
        _env = env;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // DEV: mỗi 1 phút; PROD: mỗi ngày
        var interval = _env.IsDevelopment()
            ? TimeSpan.FromMinutes(1)
            : TimeSpan.FromDays(1);

        // chạy ngay 1 lần khi start (để demo dễ)
        await RunOnce(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await RunOnce(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expiry reminder background job failed.");
            }
        }
    }

    private async Task RunOnce(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var contractService = scope.ServiceProvider.GetRequiredService<IContractService>();

        var sent = await contractService.ScanAndSendExpiryRemindersAsync(
            daysBeforeEnd: 7,
            remindType: "Expiry_7d",
            actorUserId: null,
            ct: ct);

        _logger.LogInformation("Expiry reminder scan done. sent={Sent}", sent);
    }
}