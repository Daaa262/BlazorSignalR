using System.Runtime.InteropServices;

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
    }
}
