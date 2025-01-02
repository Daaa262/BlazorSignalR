using System.Runtime.InteropServices;
using Microsoft.AspNetCore.SignalR;
using ZgodnieZTutorialem.Client.Models;

namespace ZgodnieZTutorialem.Hubs;

public class TableHub : Hub
{
    private static List<Table> table = [];
    public async Task Ready(string tableName, int index)
    {
        Console.WriteLine($"Ready in hub called for \"{tableName}\" with player index {index}");
        await Clients.Group(tableName).SendAsync("NewReady", index);

        foreach(var tab in table)
        {
            if (tab.TableName == tableName)
            {
                tab.Players[index].IsReady = true;

                foreach (var player in tab.Players)
                {
                    if (!player.IsReady)
                    {
                        return;
                    }
                }

                tab.GameStarted = true;
                break;
            }
        }
    }

    public async Task AddTable(Table newTable)
    {
        Console.WriteLine($"AddTable in hub called for \"{newTable.TableName}\"");

        foreach (var tab in table)
        {
            if (tab.TableName == newTable.TableName)
            {
                Console.WriteLine($"Table \"{newTable.TableName}\" already exists");
                return;
            }
        }

        table.Add(newTable);

        Console.WriteLine($"\"{newTable.TableName}\" added to hub");

        await Clients.All.SendAsync("ReceiveNewTable", newTable);
    }

    public async Task GetTables()
    {
        Console.WriteLine("GetTables called in hub");

        foreach (var tab in table)
        {
            Console.WriteLine($"Table \"{tab.TableName}\" has {tab.Players.Count} players");
            await Clients.Caller.SendAsync("ReceiveNewTable", tab);
        }
    }

    public async Task RequestInfo(string tableName)
    {
        Console.WriteLine($"RequestInfo called in hub for \"{tableName}\"");

        foreach (var tab in table)
        {
            if(tableName == tab.TableName)
            {
                await Clients.Caller.SendAsync("ReceiveInfo", tab);
                return;
            }
        }
    }
    public async Task AddToGroup(string tableName)
    {
        Console.WriteLine($"AddToGroup called in hub for \"{tableName}\"");

        await Groups.AddToGroupAsync(Context.ConnectionId, tableName);
    }

    public async Task JoinTable(string tableName, string nick)
    {
        Console.WriteLine($"JoinTable called in hub for \"{tableName}\" with nick \"{nick}\"");

        foreach (var tab in table)
        {
            if (tab.TableName == tableName)
            {
                Player newPlayer = new(false, tab.StartChipCount, nick);
                tab.Players.Add(newPlayer);
                await Clients.All.SendAsync("ReceiveNewPlayerLobby", tableName, newPlayer);
                await Clients.Groups(tableName).SendAsync("ReceiveNewPlayer", newPlayer);
                return;
            }
        }
    }
}