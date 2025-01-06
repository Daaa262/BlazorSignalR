using System;
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
        public List<int> Winner { get; set; } = [];
        public int WinningConfiguration { get; set; }
        public int Turn { get; set; }
        private Random Rand { get; set; } = new();
        public int Pot { get; set; }
        public List<int> PlayerToRemove { get; set; } = [];
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
        private void DrawCards()
        {
            if (DebugInfo.debug)
                Console.WriteLine($"DrawCards called in {TableName}");

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
        private void CalculatePot()
        {
            if (DebugInfo.debug)
                Console.WriteLine($"CalculatePot called in {TableName}");

            Pot = 0;
            foreach (var player in Players)
                Pot += player.CurrentBid;
        }
        public void StartRound(bool first = false)
        {
            if (DebugInfo.debug)
                Console.WriteLine($"StartRound called in {TableName}");

            if (first)
                Dealer = Rand.Next(Players.Count);
            else
            {
                Dealer = (Dealer + 1) % Players.Count;
                foreach(var player in Players)
                {
                    player.CurrentBid = 0;
                    player.Okej = false;
                    player.Check = false;
                    player.Fold = false;
                }

                int remainder = Pot % Winner.Count;
                foreach(var winner in Winner)
                {
                    Players[winner].Chips += Pot / Winner.Count; // tutaj błąd XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
                }

                int index = 0;
                while(remainder > 0)
                {
                    index++;
                    if (Players[(Dealer + index) % Players.Count].Fold)
                        continue;

                    Players[(Dealer + index) % Players.Count].Chips++;
                    remainder--;
                }
            }
            Stage = 0;
            GameStarted = true;

            int min = Blind * 2;
            foreach (var player in Players)
            {
                if(player.Chips < min)
                    min = player.Chips;
            }

            if(min < Blind * 2)
            {
                foreach(var player in Players)
                {
                    player.CurrentBid = min;
                    Stage = 4;
                    return;
                }
            }

            //set blinds
            if (Players.Count == 2)
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

            DrawCards();
        }
        public void RemovePlayers()
        {
            foreach (var index in PlayerToRemove)
                Players.RemoveAt(index);
            MaxPlayerCount--;

            foreach (var player in Players)
                player.Okej = false;

            if (Players.Count == 1)
            {
                Players[0].CurrentBid = 0;
                Players[0].Chips += Pot;
                Stage = 7;
            }

            else StartRound();
        }
        public void EndRound()
        {
            if (DebugInfo.debug)
                Console.WriteLine($"EndRound called in {TableName}");

            //check if player without chips won
            PlayerToRemove.Clear();
            for(int i = 0; i < Players.Count; i++)
            {
                Players[i].Okej = false;

                if (Players[i].Chips == 0)
                {
                    bool flag = true;
                    for(int j = 0; j < Winner.Count; j++)
                    {
                        if (i == Winner[j])
                        {
                            flag = false;
                            break;
                        }
                    }

                    //player lost
                    if(flag)
                    {
                        if (DebugInfo.debug)
                            Console.WriteLine($"Player to remove in {TableName} has index {i}");

                        Stage = 6;
                        PlayerToRemove.Add(i);
                    }
                }
            }

            if (Stage != 6)
                StartRound();
        }
        public void NextTurn()
        {
            if (DebugInfo.debug)
                Console.WriteLine($"NextTurn called in {TableName}");

            do
            {
                Turn = (Turn + 1) % Players.Count;
            } while (Players[Turn].Fold);
        }
        private void SetStartTurn()
        {
            if (DebugInfo.debug)
                Console.WriteLine($"SetStartTurn called in {TableName}");

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
            if (DebugInfo.debug)
                Console.WriteLine($"Fold called in {TableName} with index {index}");

            Players[index].Fold = true;



            //Check if there is only one player left
            bool foldFlag = false;
            int winnerIndex = 0;
            for (int i = 0; i < Players.Count; i++)
            {
                if (!Players[i].Fold)
                {
                    winnerIndex = i; //preemptively set the winner
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
                Winner.Clear();
                Winner.Add(winnerIndex);
                CalculatePot();

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
            if (DebugInfo.debug)
                Console.WriteLine($"Check called in {TableName} with index {index} and bid {bid}");

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
                CalculatePot();
                return;
            }

            foreach (var player in Players)
                player.Check = false;

            SetStartTurn();
            return;
        }



        public void Bid(int index, int bid)
        {
            if (DebugInfo.debug)
                Console.WriteLine($"Bid called in {TableName} with index {index} and bid {bid}");

            Players[index].CurrentBid += bid;
            Players[index].Chips -= bid;

            foreach (var player in Players)
                player.Check = false;
            Players[index].Check = true;

            NextTurn();
        }


        private void SetWinningConfiguration(int configuration, int index, ref bool continueFlag)
        {
            if (DebugInfo.debug)
                Console.WriteLine($"SetWinningConfiguration called in {TableName} with config {configuration}, index {index}");

            if (WinningConfiguration > configuration)
                Winner.Clear();
            WinningConfiguration = configuration;
            Winner.Add(index);
            continueFlag = true;
        }

        private void FindWinner()
        {
            if (DebugInfo.debug)
                Console.WriteLine($"FindWinner called in {TableName}");

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
            Winner.Clear();
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
                    if ((temp & 0b1_0000_0000_1111) == 0b1_0000_0000_1111 && WinningConfiguration >= 10)
                    {
                        SetWinningConfiguration(10, i, ref continueFlag);
                    }

                    for (int k = 0; k < 9; k++)
                    {
                        if ((temp & 0b11111) == 0b11111 && WinningConfiguration > 9 - k)
                        {
                            SetWinningConfiguration(9 - k, i, ref continueFlag);
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
                        SetWinningConfiguration(11 + j, i, ref continueFlag);
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
                                SetWinningConfiguration(24 + j * 13 + k, i, ref continueFlag);
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
                                SetWinningConfiguration(193 + k, i, ref continueFlag);
                            }
                        }
                    }
                }

                if (continueFlag)
                    continue;

                //straight (206 - 218)
                int length = 0, highest = -1, highestBuffer = -1;
                for (int j = 0; j < 13; j++)
                {
                    if ((temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) != 0)
                    {
                        if(highestBuffer == -1)
                            highestBuffer = j;
                        length++;
                    }
                    else
                    {
                        if (length >= 5)
                        {
                            highest = highestBuffer;
                            break;
                        }

                        highestBuffer = -1;
                        length = 0;
                    }
                }
                if (length >= 5 && WinningConfiguration > 206 + highest)
                {
                    if(highest == -1)
                        highest = highestBuffer;

                    SetWinningConfiguration(10, i, ref continueFlag);
                    continue;
                }

                //three of a kind (218 - 230)
                for (int j = 0; j < 13; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 3 && WinningConfiguration > 218 + j)
                    {
                        SetWinningConfiguration(218 + j, i, ref continueFlag);
                    }
                }

                if (continueFlag)
                    continue;

                //two pair (231 - 386)
                for (int j = 0; j < 12; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 2)
                    {
                        for (int k = j + 1; k < 13; k++)
                        {
                            if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> k)) == 2 && WinningConfiguration > 231 + j * 13 + k)
                            {
                                SetWinningConfiguration(231 + j * 13 + k, i, ref continueFlag);
                                break;
                            }
                        }
                    }

                    if (continueFlag)
                        break;
                }

                if (continueFlag)
                    continue;

                //pair (387 - 399)
                for (int j = 0; j < 13; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 2 && WinningConfiguration > 387 + j)
                    {
                        SetWinningConfiguration(387 + j, i, ref continueFlag);
                        break;
                    }

                    if (continueFlag)
                        break;
                }

                if (continueFlag)
                    continue;

                //high card (400 - 412)
                for (int j = 0; j < 13; j++)
                {
                    if (long.PopCount(temp & (0b1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000___1_0000_0000_0000L >> j)) == 1 && WinningConfiguration > 400 + j)
                    {
                        SetWinningConfiguration(400 + j, i, ref continueFlag);
                        break;
                    }
                }
            }
        }

        public string WinningCombinationString()
        {
            if (DebugInfo.debug)
                Console.WriteLine($"WinningCombinationString called in {TableName}");

            if (WinningConfiguration == 1)
                return "Poker Królewski!";
            else if (WinningConfiguration <= 10)
                return "Poker";
            else if (WinningConfiguration <= 23)
                return "Kareta";
            else if (WinningConfiguration <= 192)
                return "Full";
            else if (WinningConfiguration <= 205)
                return "Kolor";
            else if (WinningConfiguration <= 218)
                return "Strit";
            else if (WinningConfiguration <= 230)
                return "Trójka";
            else if (WinningConfiguration <= 386)
                return "Dwie pary";
            else if (WinningConfiguration <= 399)
                return "Para";
            else
                return "Wysoka karta";
        }
    }
}
