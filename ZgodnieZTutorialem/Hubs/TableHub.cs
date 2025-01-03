using System.ComponentModel;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;
using ZgodnieZTutorialem.Client.Models;

namespace ZgodnieZTutorialem.Hubs;

public class TableHub : Hub
{
    private static List<Table> table = [];
    public Random random = new();
    public async Task Fold(string tableName, int index)
    {
        if (DebugInfo.debug)
            Console.WriteLine($"Fold in hub called for \"{tableName}\" with player index {index}");

        foreach (var tab in table)
        {
            if (tab.TableName == tableName)
            {
                tab.Players[index].Fold = true;
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
                tab.Players[index].Check = true;

                foreach (var player in tab.Players)
                {
                    if (!player.Check && !player.Fold)
                    {
                        //if not all players checked
                        if(DebugInfo.debug)
                            Console.WriteLine($"Player \"{player.Nick}\" did not check");
                        tab.Players[index].CurrentBid += bid;
                        tab.Players[index].Chips -= bid;
                        await Clients.Group(tableName).SendAsync("NewCheck", index, bid);
                        return;
                    }
                }

                //if all players checked
                if (DebugInfo.debug)
                    Console.WriteLine($"Every player checked");
                tab.Players[index].CurrentBid += bid;
                tab.Players[index].Chips -= bid;
                tab.Stage++;
                foreach(var player in tab.Players)
                {
                    player.Check = false;
                }
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
                foreach (var player in tab.Players)
                {
                    player.Check = false;
                }
                tab.Players[index].Check = true;
                tab.Players[index].CurrentBid += bid;
                tab.Players[index].Chips -= bid;
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

                //CardFlag is used to check if card is already drawn (set false if drawn)

                //drawing on table
                for (int i = 0; i < 5; i++)
                {
                    bool CardFlag = true;
                    while (CardFlag)
                    {
                        tab.Cards[i] = random.Next(52);

                        CardFlag = false;
                        for (int j = 0; j < i; j++)
                        {
                            if (tab.Cards[i] == tab.Cards[j])
                            {
                                CardFlag = true;
                                break;
                            }
                        }
                    }

                    if (DebugInfo.debug)
                        Console.WriteLine($"On table \"{tableName}\" card {tab.Cards[i]} has been drawn");
                }

                //drawing for players in poker
                for (int i = 0; i < tab.Players.Count; i++)
                {
                    //first card
                    bool CardFlag = true;
                    while (CardFlag)
                    {
                        tab.Players[i].Cards[0] = random.Next(52);

                        //on table
                        CardFlag = false;
                        for (int j = 0; j < 5; j++)
                        {
                            if (tab.Players[i].Cards[0] == tab.Cards[j])
                            {
                                CardFlag = true;
                                break;
                            }
                        }

                        if (CardFlag)
                            continue;

                        //on other players hand
                        CardFlag = false;
                        for (int j = 0; j < i; j++)
                        {
                            if (tab.Players[i].Cards[0] == tab.Players[j].Cards[0] || tab.Players[i].Cards[0] == tab.Players[j].Cards[1])
                            {
                                CardFlag = true;
                                break;
                            }
                        }
                    }

                    if (DebugInfo.debug)
                        Console.WriteLine($"On table \"{tab.TableName}\" card {tab.Players[i].Cards[0]} has been drawn for player \"{tab.Players[i].Nick}\"");

                    //second card
                    CardFlag = true;
                    while (CardFlag)
                    {
                        tab.Players[i].Cards[1] = random.Next(52);

                        if(tab.Players[i].Cards[1] == tab.Players[i].Cards[0])
                            continue;

                        //on table
                        CardFlag = false;
                        for (int j = 0; j < 5; j++)
                        {
                            if (tab.Players[i].Cards[1] == tab.Cards[j])
                            {
                                CardFlag = true;
                                break;
                            }
                        }

                        if (CardFlag)
                            continue;

                        //on other players hand
                        CardFlag = false;
                        for (int j = 0; j < i; j++)
                        {
                            if (tab.Players[i].Cards[1] == tab.Players[j].Cards[0] || tab.Players[i].Cards[1] == tab.Players[j].Cards[1])
                            {
                                CardFlag = true;
                                break;
                            }
                        }

                        if (CardFlag)
                            continue;
                    }

                    if (DebugInfo.debug)
                        Console.WriteLine($"On table \"{tab.TableName}\" card {tab.Players[i].Cards[1]} has been drawn for player \"{tab.Players[i].Nick}\"");
                }

                tab.GameStarted = true;
                tab.Dealer = random.Next(tab.MaxPlayerCount);
                tab.Stage = 0;

                if (tab.MaxPlayerCount == 2)
                {
                    tab.Players[tab.Dealer].CurrentBid = tab.Blind;
                    tab.Players[tab.Dealer].Chips -= tab.Blind;

                    tab.Players[(tab.Dealer + 1) % tab.MaxPlayerCount].CurrentBid = tab.Blind * 2;
                    tab.Players[(tab.Dealer + 1) % tab.MaxPlayerCount].Chips -= tab.Blind * 2;
                    tab.Players[(tab.Dealer + 1) % tab.MaxPlayerCount].Check = true;
                }
                else
                {
                    tab.Players[(tab.Dealer + 1) % tab.MaxPlayerCount].CurrentBid = tab.Blind;
                    tab.Players[(tab.Dealer + 1) % tab.MaxPlayerCount].Chips -= tab.Blind;

                    tab.Players[(tab.Dealer + 2) % tab.MaxPlayerCount].CurrentBid = tab.Blind * 2;
                    tab.Players[(tab.Dealer + 2) % tab.MaxPlayerCount].Chips -= tab.Blind * 2;
                    tab.Players[(tab.Dealer + 2) % tab.MaxPlayerCount].Check = true;
                }

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