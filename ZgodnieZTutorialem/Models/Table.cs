namespace ZgodnieZTutorialem.Models
{
    public class Table
    {
        public string? TableName { get; set; }
        public int? PlayerCount { get; set; }
        public int? ChipCount { get; set; }
        public string?[] playerID;

        public Table(string tableName, int playerCount, int chipCount)
        {
            TableName = tableName;
            PlayerCount = playerCount;
            ChipCount = chipCount;
            playerID = new string?[playerCount];
        }
    }
}
