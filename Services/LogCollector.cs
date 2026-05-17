using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace AI_SEO_Ssas_Platform.Services;

public interface ILogCollector
{
    void Initialize(string connectionId);
    Task AddLogAsync(string message);
    List<string> GetLogs();
}

public class LogCollector : ILogCollector
{
    private List<string> _logs = new();
    private readonly IHubContext<AgentHub> _hubContext;
    private string _connectionId = string.Empty;

    public LogCollector(IHubContext<AgentHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void Initialize(string connectionId)
    {
        _logs = new List<string>();
        _connectionId = connectionId;
    }

    public async Task AddLogAsync(string message)
    {
        _logs.Add(message);
        System.Console.WriteLine(message);
        if (!string.IsNullOrEmpty(_connectionId))
        {
            await _hubContext.Clients.Client(_connectionId).SendAsync("ReceiveLog", message);
        }
    }

    public List<string> GetLogs()
    {
        return _logs.ToList();
    }
}
