﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

namespace ZgodnieZTutorialem.Client.Models
{
    public class Table
    {
        public string? TableName { get; set; }
        public int MaxPlayerCount { get; set; }
        public int StartChipCount { get; set; }
        public List<Player> Players { get; set; } = [];
        public bool GameStarted { get; set; } = false;
        public int[] Cards { get; set; } = new int[5];
        public int Dealer { get; set; }
        public int Stage { get; set; }
        public int Blind { get; set; }
        public int Winner { get; set; }
        public int WinningConfiguration { get; set; }
        public int Turn { get; set; }
        public Random Rand { get; set; } = new();
        public Table(string tableName, int maxPlayerCount, int startChipCount, int blind)
        {
            TableName = tableName;
            MaxPlayerCount = maxPlayerCount;
            StartChipCount = startChipCount;
            Blind = blind;
        }
        public Table() // This constructor is required for SignalR
        {

        }
        public void DrawCards()
        {
            //CardFlag is used to check if card is already drawn (set false if drawn)

            //drawing on table
            for (int i = 0; i < 5; i++)
            {
                bool CardFlag = true;
                while (CardFlag)
                {
                    Cards[i] = Rand.Next(52);

                    CardFlag = false;
                    for (int j = 0; j < i; j++)
                    {
                        if (Cards[i] == Cards[j])
                        {
                            CardFlag = true;
                            break;
                        }
                    }
                }
            }

            //drawing for players in poker
            for (int i = 0; i < Players.Count; i++)
            {
                //first card
                bool CardFlag = true;
                while (CardFlag)
                {
                    Players[i].Cards[0] = Rand.Next(52);

                    //on table
                    CardFlag = false;
                    for (int j = 0; j < 5; j++)
                    {
                        if (Players[i].Cards[0] == Cards[j])
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
                        if (Players[i].Cards[0] == Players[j].Cards[0] || Players[i].Cards[0] == Players[j].Cards[1])
                        {
                            CardFlag = true;
                            break;
                        }
                    }
                }

                //second card
                CardFlag = true;
                while (CardFlag)
                {
                    Players[i].Cards[1] = Rand.Next(52);

                    if (Players[i].Cards[1] == Players[i].Cards[0])
                        continue;

                    //on table
                    CardFlag = false;
                    for (int j = 0; j < 5; j++)
                    {
                        if (Players[i].Cards[1] == Cards[j])
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
                        if (Players[i].Cards[1] == Players[j].Cards[0] || Players[i].Cards[1] == Players[j].Cards[1])
                        {
                            CardFlag = true;
                            break;
                        }
                    }

                    if (CardFlag)
                        continue;
                }
            }
        }
        public void StartRound(bool first = false)
        {
            DrawCards();

            if (first)
                Dealer = Rand.Next(Players.Count);
            else
            {
                Dealer = (Dealer + 1) % Players.Count;
                int pot = 0;
                foreach(var player in Players)
                {
                    pot += player.CurrentBid;
                    player.CurrentBid = 0;
                    player.Okej = false;
                    player.Check = false;
                    player.Fold = false;
                }
                Players[Winner].Chips += pot;
            }
            Stage = 0;

            //set blinds
            if (MaxPlayerCount == 2)
            {
                Players[Dealer].CurrentBid = Blind;
                Players[Dealer].Chips -= Blind;

                Players[(Dealer + 1) % Players.Count].CurrentBid = Blind * 2;
                Players[(Dealer + 1) % Players.Count].Chips -= Blind * 2;
                Players[(Dealer + 1) % Players.Count].Check = true;

                Turn = Dealer;
            }
            else
            {
                Players[(Dealer + 1) % Players.Count].CurrentBid = Blind;
                Players[(Dealer + 1) % Players.Count].Chips -= Blind;

                Players[(Dealer + 2) % Players.Count].CurrentBid = Blind * 2;
                Players[(Dealer + 2) % Players.Count].Chips -= Blind * 2;
                Players[(Dealer + 2) % Players.Count].Check = true;

                Turn = (Dealer + 3) % Players.Count;
            }

            GameStarted = true;
        }
        public void NextTurn()
        {
            do
            {
                Turn = (Turn + 1) % Players.Count;
            } while (Players[Turn].Fold);
        }
        public void SetStartTurn()
        {
            if (Players.Count == 2)
            {
                Turn = Dealer;
                while (Players[Turn].Fold)
                    Turn = (Turn + 1) % Players.Count;
            }
            else
            {
                Turn = (Dealer + 3) % Players.Count;
                while (Players[Turn].Fold)
                    Turn = (Turn + 1) % Players.Count;
            }
        }



        public void Fold(int index)
        {
            Players[index].Fold = true;



            //Check if there is only one player left
            bool foldFlag = false;
            for (int i = 0; i < Players.Count; i++)
            {
                if (!Players[i].Fold)
                {
                    Winner = i; //preemptively set the winner
                    if (foldFlag)
                    {
                        foldFlag = false;
                        break;
                    }
                    foldFlag = true;
                }
            }

            if (foldFlag)
            {
                Stage = 5;
                return;
            }



            //fold but stage remains the same
            foreach (var player in Players)
            {
                if (!player.Fold && !player.Check)
                {
                    NextTurn();
                    return;
                }
            }



            //fold but next stage
            Stage++;
            foreach (var player in Players)
                player.Check = false;

            SetStartTurn();
        }



        public void Check(int index, int bid)
        {
            Players[index].Check = true;



            //check but stage remains the same
            foreach (var player in Players)
            {
                if (!player.Fold && !player.Check)
                {
                    Players[index].CurrentBid += bid;
                    Players[index].Chips -= bid;

                    NextTurn();

                    return;
                }
            }



            //check but next stage
            Players[index].CurrentBid += bid;
            Players[index].Chips -= bid;
            Stage++;

            if (Stage == 4)
            {
                FindWinner();
                return;
            }

            foreach (var player in Players)
                player.Check = false;

            SetStartTurn();
            return;
        }



        public void Bid(int index, int bid)
        {
            Players[index].CurrentBid += bid;
            Players[index].Chips -= bid;

            foreach (var player in Players)
                player.Check = false;
            Players[index].Check = true;

            NextTurn();
        }



        public void FindWinner() //ADD SPLITTING
        {
            //cards are encoded as 52 bit integer where each bit set to 1 means card appeared, and 0 means it doesn't bit from left to right are A K Q J 10 9 8 7 6 5 4 3 2 and suits are ordered left to right as ♥♦♣♠

            //contains information about total cards each player can use
            long[] hand = new long[Players.Count];

            //add cards on table to each player's use
            for (int i = 0; i < Players.Count; i++)
                foreach (var card in Cards)
                    hand[i] ^= 1L << card;

            //add each player's cards to use
            for (int i = 0; i < Players.Count; i++)
            {
                hand[i] ^= 1L << Players[i].Cards[0];
                hand[i] ^= 1L << Players[i].Cards[1];

                Console.WriteLine($"Gracz {i} ma rękę {Convert.ToString(hand[i], 2)}");
            }

            WinningConfiguration = 410; // 1 best, higher worse
            Winner = -1;
            for (int i = 0; i < Players.Count; i++)
            {
                if(Players[i].Fold)
                    continue;

                bool continueFlag = false;
                long temp;

                //straight flush (1 - 10)
                temp = hand[i];
                for (int j = 0; j < 4; j++)
                {
                    if ((temp & 0b1_0000_0000_1111) == 0b1_0000_0000_1111 && WinningConfiguration > 10)
                    {
                        WinningConfiguration = 10;
                        Winner = i;
                        continueFlag = true;
                    }

                    for (int k = 0; k < 9; k++)
                    {
                        if ((temp & 0b11111) == 0b11111 && WinningConfiguration > 9 - k)
                        {
                            WinningConfiguration = 9 - k;
                            Winner = i;
                            continueFlag = true;
                        }
                        temp >>= 1;
                    }

                    temp >>= 4;
                }

                if (continueFlag)
                    continue;

                temp = hand[i];

                //four of a kind (11 - 23)
                for (int j = 0; j < 13; j++)
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 4 && WinningConfiguration > 11 + j)
                    {
                        WinningConfiguration = 11 + j;
                        Winner = i;
                        continueFlag = true;
                    }

                if (continueFlag)
                    continue;

                //full house (24 - 192)
                for (int j = 0; j < 13; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 3)
                    {
                        for (int k = 0; k < 13; k++)
                        {
                            if (k == j)
                                continue;

                            if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> k)) == 2 && WinningConfiguration > 24 + j * 13 + k)
                            {
                                WinningConfiguration = 24 + j * 13 + k;
                                Winner = i;
                                continueFlag = true;
                            }
                        }
                    }
                }

                if (continueFlag)
                    continue;

                //flush (193 - 205)
                for (int j = 0; j < 4; j++)
                {
                    if (long.PopCount(temp & (0b1_1111_1111_1111L << (13 * j))) >= 5)
                    {
                        for (int k = 0; k < 13; k++)
                        {
                            if ((temp & (0b1L << (13 * j + k))) != 0 && WinningConfiguration > 193 + k)
                            {
                                WinningConfiguration = 193 + k;
                                Winner = i;
                                continueFlag = true;
                            }
                        }
                    }
                }

                if (continueFlag)
                    continue;

                //straight (206 - 215)
                int length = 0, highest = 0;
                for (int j = 0; j < 9; j++)
                {
                    if ((temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) != 0)
                    {
                        highest = j;
                        length++;
                    }
                    else
                    {
                        if (length >= 5)
                            break;
                        length = 0;
                    }
                }
                if (length >= 5 && WinningConfiguration > 206 + highest)
                {
                    WinningConfiguration = 206 + highest;
                    Winner = i;
                    continue;
                }

                //three of a kind (216 - 228)
                for (int j = 0; j < 13; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 3 && WinningConfiguration > 216 + j)
                    {
                        WinningConfiguration = 216 + j;
                        Winner = i;
                        continueFlag = true;
                    }
                }

                if (continueFlag)
                    continue;

                //two pair (229 - 384)
                for (int j = 0; j < 12; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 2)
                    {
                        for (int k = j + 1; k < 13; k++)
                        {
                            if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> k)) == 2 && WinningConfiguration > 229 + j * 13 + k)
                            {
                                WinningConfiguration = 229 + j * 13 + k;
                                Winner = i;
                                continueFlag = true;
                                break;
                            }
                        }
                    }

                    if (continueFlag)
                        break;
                }

                if (continueFlag)
                    continue;

                //pair (385 - 397)
                for (int j = 0; j < 13; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 2 && WinningConfiguration > 385 + j)
                    {
                        WinningConfiguration = 385 + j;
                        Winner = i;
                        continueFlag = true;
                        break;
                    }

                    if (continueFlag)
                        break;
                }

                if (continueFlag)
                    continue;

                //high card (398 - 410)
                for (int j = 0; j < 13; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 1 && WinningConfiguration > 398 + j)
                    {
                        WinningConfiguration = 398 + j;
                        Winner = i;
                        break;
                    }
                }
            }
        }

        public string WinningCombinationString()
        {
            if(WinningConfiguration == 1)
                return "Poker Królewski!";
            else if (WinningConfiguration <= 10)
                return "Poker";
            else if (WinningConfiguration <= 23)
                return "Kareta";
            else if (WinningConfiguration <= 192)
                return "Full";
            else if (WinningConfiguration <= 205)
                return "Kolor";
            else if (WinningConfiguration <= 215)
                return "Strit";
            else if (WinningConfiguration <= 228)
                return "Trójka";
            else if (WinningConfiguration <= 384)
                return "Dwie pary";
            else if (WinningConfiguration <= 397)
                return "Para";
            else
                return "Wysoka karta";
        }
    }
}
