using System.ComponentModel;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;
using ZgodnieZTutorialem.Client.Models;
using ZgodnieZTutorialem.Components.DatabaseAccess;
using Dapper;
using System.Reflection.Metadata;

namespace ZgodnieZTutorialem.Hubs;

public class TableHub : Hub
{
    private static List<Table> table = [];
    public Random random = new();
    public async Task RemoveTable(string tableName)
    {
        if (DebugInfo.debug)
            Console.WriteLine($"RemoveTable called for \"{tableName}\".");

        foreach (var tab in table)
        {
            if(tab.TableName == tableName)
            {
                IDbConnection connection = new SqlConnection("Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=Leaderboard;Integrated Security=True;Connect Timeout=30;Encrypt=True;Trust Server Certificate=False;Application Intent=ReadWrite;Multi Subnet Failover=False");
                Record record = new Record(tableName, tab.Players[0].Nick);
                await connection.ExecuteAsync(@"insert into dbo.WinnersDatabase (Nick, TableName) values (@Nick, @TableName)", record);

                table.Remove(tab);

                if (DebugInfo.debug)
                    Console.WriteLine($"Table \"{tableName}\" removed from hub.");

                await Clients.All.SendAsync("TableRemoved", tableName);
                return;
            }
        }
    }
    public async Task Okej(string tableName, int index)
    {
        if(DebugInfo.debug)
            Console.WriteLine($"Okej in hub called for \"{tableName}\" with player index {index}");

        foreach (var tab in table)
        {
            if (tab.TableName == tableName)
            {
                tab.Players[index].Okej = true;

                bool okejFlag = true;
                foreach(var player in tab.Players)
                {
                    if (!player.Okej)
                        okejFlag = false;
                }
                if (okejFlag)
                {
                    if (tab.Stage == 6)
                    {
                        if (DebugInfo.debug)
                            Console.WriteLine($"RemovePlayers send for table \"{tableName}\"");

                        tab.RemovePlayers();
                        await Clients.Group(tableName).SendAsync("RemovePlayers", tab);
                    }
                    else
                    {
                        if (DebugInfo.debug)
                            Console.WriteLine($"EndRound send for table \"{tableName}\"");

                        tab.EndRound();
                        await Clients.Group(tableName).SendAsync("EndRound", tab);
                    }
                    return;
                }

                await Clients.Group(tableName).SendAsync("NewOkej", index);
                return;
            }
        }
    }
    public async Task Fold(string tableName, int index)
    {
        if (DebugInfo.debug)
            Console.WriteLine($"Fold in hub called for \"{tableName}\" with player index {index}");

        foreach (var tab in table)
        {
            if (tab.TableName == tableName)
            {
                tab.Fold(index);

                await Clients.Group(tableName).SendAsync("NewFold", index);
                return;
            }
        }
    }
    public async Task Check(string tableName, int index, int bid)
    {
        if (DebugInfo.debug)
            Console.WriteLine($"Check in hub called for \"{tableName}\" with player index {index} and bid {bid}");

        foreach (var tab in table)
        {
            if (tab.TableName == tableName)
            {
                tab.Check(index, bid);

                await Clients.Group(tableName).SendAsync("NewCheck", index, bid);
                return;
            }
        }
    }
    public async Task Bid(string tableName, int index, int bid)
    {
        if(DebugInfo.debug)
            Console.WriteLine($"Bid in hub called for \"{tableName}\" with player index {index} and bid {bid}");

        foreach (var tab in table)
        {
            if (tab.TableName == tableName)
            {
                tab.Bid(index, bid);

                await Clients.Group(tableName).SendAsync("NewBid", index, bid);
                return;
            }
        }
    }
    public async Task Ready(string tableName, int index)
    {
        foreach(var tab in table)
        {
            if (tab.TableName == tableName)
            {
                tab.Players[index].IsReady = true;

                foreach (var player in tab.Players)
                {
                    if (!player.IsReady)
                    {
                        if (DebugInfo.debug)
                            Console.WriteLine($"Ready in hub called for \"{tableName}\" with player index {index}");

                        await Clients.Group(tableName).SendAsync("NewReady", index);
                        return;
                    }
                }

                //every player ready

                if (DebugInfo.debug)
                    Console.WriteLine($"Ready in hub called for \"{tableName}\", gameIsStarting");

                tab.StartRound(true);

                await Clients.Group(tableName).SendAsync("StartGame", tab);
                break;
            }
        }
    }

    public async Task AddTable(Table newTable)
    {
        if (DebugInfo.debug)
            Console.WriteLine($"AddTable in hub called for \"{newTable.TableName}\"");

        foreach (var tab in table)
        {
            if (tab.TableName == newTable.TableName)
            {
                if (DebugInfo.debug)
                    Console.WriteLine($"Table \"{newTable.TableName}\" already exists");
                return;
            }
        }

        table.Add(newTable);

        if (DebugInfo.debug)
            Console.WriteLine($"\"{newTable.TableName}\" added to hub");

        await Clients.All.SendAsync("ReceiveNewTable", newTable);
    }

    public async Task GetTables()
    {
        if (DebugInfo.debug)
            Console.WriteLine("GetTables called in hub");

        foreach (var tab in table)
        {
            if (DebugInfo.debug)
                Console.WriteLine($"Table \"{tab.TableName}\" has {tab.Players.Count} players");
            await Clients.Caller.SendAsync("ReceiveNewTable", tab);
        }
    }

    public async Task RequestInfo(string tableName)
    {
        if (DebugInfo.debug)
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
        if (DebugInfo.debug)
            Console.WriteLine($"AddToGroup called in hub for \"{tableName}\"");

        await Groups.AddToGroupAsync(Context.ConnectionId, tableName);
    }

    public async Task JoinTable(string tableName, string nick)
    {
        if (DebugInfo.debug)
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