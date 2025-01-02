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
        public Table(string tableName, int maxPlayerCount, int startChipCount)
        {
            TableName = tableName;
            MaxPlayerCount = maxPlayerCount;
            StartChipCount = startChipCount;
            Players = [];
        }
        public Table() // This constructor is required for SignalR
        {

        }
    }
}
