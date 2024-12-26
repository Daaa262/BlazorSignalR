using Microsoft.AspNetCore.SignalR;
using ZgodnieZTutorialem.Models;

namespace ZgodnieZTutorialem.Hubs;

public class TableHub : Hub
{
    private static List<Table> table = new();

    public async Task AddTable(string tableName, int playerCount, int chipCount)
    {
        for(int i = 0; i < table.Count; i++)
        {
            if (table[i].TableName == tableName)
                return;
        }
        //table.Add(new Table(tableName, playerCount, chipCount ));
        table.Add(new Table(Context.ConnectionId, playerCount, chipCount));
        await Clients.All.SendAsync("ReceiveNewTable", tableName, playerCount, chipCount);
    }

    public async Task GetTables()
    {
        foreach (var tab in table)
        {
            await Clients.Caller.SendAsync("ReceiveNewTable", tab.TableName, tab.PlayerCount, tab.ChipCount);
        }
    }
    public async Task CheckIfPlayerOnTable()
    {
        await Clients.Caller.SendAsync("GoToLobby");
        string ID = Context.ConnectionId;
        foreach(var tab in table)
        {
            for (int i = 0; i < tab.PlayerCount; i++)
            {
                if (tab.playerID[i] == ID)
                {
                    await Clients.Caller.SendAsync("GoToGame", tab.TableName);
                    return;
                }
            }
        }
    }
}