using System.Text.Json;
using Rhea.Models;
using Serilog;
using Websocket.Client;

namespace Rhea.Services;

public class SimulatorRadio
{
    public SimulatorRadio()
    {
        StartAsync().ConfigureAwait(false);
    }

    public string song { get; set; } = "N/A";
    public string artist { get; set; } = "N/A";
    public string? artwork { get; set; }
    public string url() => $"https://simulatorradio.stream/stream?t={DateTimeOffset.Now.ToUnixTimeMilliseconds()}";

    private async Task StartAsync()
    {
        var exitEvent = new ManualResetEvent(false);

        using var client = new WebsocketClient(new Uri("wss://ws.simulatorradio.com"));

        client.ReconnectTimeout = TimeSpan.FromSeconds(30);
        client.ReconnectionHappened.Subscribe(info => Log.Information($"[Socket] Simulator Radio reconnected {info.Type}"));
        client.DisconnectionHappened.Subscribe(info => Log.Warning($"[Socket] Simulator Radio disconnected {info.Type}"));
        client.MessageReceived.Subscribe(msg =>
        {
            var packet = JsonSerializer.Deserialize<SMPacket>(msg.Text!);
            if (packet is not { type: "nowPlaying" }) return;

            song = packet.now_playing.title;
            artist = packet.now_playing.artists;
            artwork = packet.now_playing.art;
        });

        await client.Start();
        exitEvent.WaitOne();
    }
}