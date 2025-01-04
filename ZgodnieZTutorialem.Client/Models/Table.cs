using System.Runtime.InteropServices;
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
        public int Turn { get; set; }
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
    }
}
