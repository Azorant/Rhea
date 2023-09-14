using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;

namespace Rhea.Services;

public class Status : IHostedService, IDisposable
{
    private Timer timer = null!;
    private DiscordSocketClient client;
    private int lastStatus = 0;

    public Status(DiscordSocketClient client)
    {
        this.client = client;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(DoWork, null, TimeSpan.Zero,
            TimeSpan.FromSeconds(30));

        return Task.CompletedTask;
    }

    private void DoWork(object state)
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

        client.SetActivityAsync(new Game(status)).ConfigureAwait(false);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        timer?.Dispose();
    }
}