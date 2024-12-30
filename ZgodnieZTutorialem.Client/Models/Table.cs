namespace ZgodnieZTutorialem.Client.Models
{
    public class Table
    {
        public string? TableName { get; set; }
        public int? TotalPlayerCount { get; set; }
        public int? ChipCount { get; set; }
        public int PlayerCount { get; set; }

        public Table(string tableName, int totalPlayerCount, int chipCount, int playerCount = 0)
        {
            TableName = tableName;
            TotalPlayerCount = totalPlayerCount;
            ChipCount = chipCount;
            PlayerCount = playerCount;
        }
    }
}
