using System.Runtime.InteropServices;
using Microsoft.AspNetCore.SignalR;
using ZgodnieZTutorialem.Client.Models;

namespace ZgodnieZTutorialem.Hubs;

public class TableHub : Hub
{
    private static List<Table> table = new();
    public async Task Ready(string tableName, int index)
    {
        await Clients.Group(tableName).SendAsync("NewReady", index);
    }

    public async Task AddTable(string tableName, int totalPlayerCount, int chipCount, int playerCount)
    {
        for(int i = 0; i < table.Count; i++)
        {
            if (table[i].TableName == tableName)
                return;
        }
        table.Add(new Table(tableName, totalPlayerCount, chipCount, 0));
        await Clients.All.SendAsync("ReceiveNewTable", tableName, totalPlayerCount, chipCount, 0);
    }

    public async Task GetTables()
    {
        foreach (var tab in table)
        {
            await Clients.Caller.SendAsync("ReceiveNewTable", tab.TableName, tab.TotalPlayerCount, tab.ChipCount, tab.PlayerCount);
        }
    }

    public async Task RequestInfo(string tableName)
    {
        for (int i = 0; i < table.Count; i++)
        {
            if (table[i].TableName == tableName)
            {
                await Clients.Group(tableName).SendAsync("ReceiveInfo", table[i].PlayerCount, table[i].TotalPlayerCount, table[i].ChipCount);
                return;
            }
        }
    }
    public async Task AddToGroup(string tableName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, tableName);
    }

    public async Task JoinTable(string tableName)
    {
        for (int i = 0; i < table.Count; i++)
        {
            if (table[i].TableName == tableName)
            {
                table[i].PlayerCount += 1;
                await Clients.Groups(tableName).SendAsync("ReceiveNewPlayer");
                await Clients.All.SendAsync("ReceiveNewPlayerCount", tableName, table[i].PlayerCount);
                return;
            }
        }
    }
}