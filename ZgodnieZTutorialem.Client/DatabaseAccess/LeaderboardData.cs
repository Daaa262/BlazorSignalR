using ZgodnieZTutorialem.Client.Models;

namespace ZgodnieZTutorialem.Components.DatabaseAccess
{
    public class LeaderboardData
    {
        private readonly SqlDataAccess dataAccess;

        public LeaderboardData(SqlDataAccess dataAccess)
        {
            if (DebugInfo.debug)
                Console.WriteLine("LeaderboardData Constructor Called");

            this.dataAccess = dataAccess;
        }
        public Task<List<Record>> GetRecords()
        {
            if (DebugInfo.debug)
                Console.WriteLine("LeaderboardData GetRecords Called");

            return dataAccess.LoadData<Record, dynamic>(@"select * from dbo.WinnersDatabase", new { });
        }
        public Task AddRecord(Record record)
        {
            if (DebugInfo.debug)
                Console.WriteLine("LeaderboardData AddRecord Called");

            return dataAccess.SaveData(@"insert into dbo.WinnersDatabase (Nick, TableName) values (@Nick, @TableName)", record);
        }
    }
}
