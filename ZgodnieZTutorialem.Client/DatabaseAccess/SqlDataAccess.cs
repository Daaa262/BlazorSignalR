using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;

namespace ZgodnieZTutorialem.Components.DatabaseAccess
{
    public class SqlDataAccess
    {
        private readonly IConfiguration _config;
        public string ConnectionString { get; set; } = "Default";
        public SqlDataAccess(IConfiguration config)
        {
            _config = config;
        }

        public async Task<List<T>> LoadData<T, U>(string sql, U parameters)
        {
            string connectionString = _config.GetConnectionString(ConnectionString);

            using (IDbConnection connection = new SqlConnection(connectionString))
            {
                var data = await connection.QueryAsync<T>(sql, parameters);

                return data.ToList();
            }
        }

        public async Task SaveData<T>(string sql, T parameters)
        {
            string connectionString = _config.GetConnectionString(ConnectionString);

            using (IDbConnection connection = new SqlConnection(ConnectionString))
            {
                await connection.ExecuteAsync(sql, parameters);
            }
        }
    }
}
