using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using StockPulse.WebAPI.BackgroundServices;
using StockPulse.WebAPI.Interfaces;

namespace StockPulse.WebAPI.Hubs;

[Authorize]
public sealed class StockHub(StockSimulatorWorker simulator) : Hub<IStockClient>
{
    private static int _activeConnections = 0;

    public override async Task OnConnectedAsync()
    {
        Interlocked.Increment(ref _activeConnections);
        Console.WriteLine($"[SignalR] Client connected. Total: {_activeConnections}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Interlocked.Decrement(ref _activeConnections);
        Console.WriteLine($"[SignalR] Client disconnected. Total: {_activeConnections}");

        if (_activeConnections <= 0)
        {
            _activeConnections = 0;
            simulator.ChangeSpeed(SimulatorSpeed.Stopped);
            Console.WriteLine("[SignalR Auto-Stop] No active clients left. Simulator auto-stopped to save Neon/Render resources.");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
