using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace Rhea.Bot.Services;

public class Status(DiscordSocketClient client) : IHostedService, IDisposable
{
    private int lastStatus;
    private Timer timer = null!;

    public void Dispose()
    {
        timer?.Dispose();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(SetStatus, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(30));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    private void SetStatus(object? state)
    {
        var status = "/help";
        switch (lastStatus)
        {
            case 0:
                lastStatus++;
                break;
            case 1:
                status = "/play";
                lastStatus++;
                break;
            case 2:
                status = "/simulator-radio";
                lastStatus++;
                break;
            case 3:
                status = "eris.gg";
                lastStatus = 0;
                break;
        }

        client.SetCustomStatusAsync(status).ConfigureAwait(false);
    }
}