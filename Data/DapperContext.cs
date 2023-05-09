
using MySql.Data.MySqlClient;
using Dapper;

namespace TaskManager.API.Data
{
    public class DapperContext
    {
        private readonly IConfiguration _configuration;
        public readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public MySqlConnection CreateConnection(){
            return new MySqlConnection(_configuration.GetConnectionString("Default"));
        }

        public async Task<T> GetFirstAsync<T>(string query, object parameters = null){
            var connection = new MySqlConnection(_configuration.GetConnectionString("Default"));
            var rs = await connection.QueryFirstAsync<T>(query, parameters);
            return rs;
        }

        public async Task<List<T>> GetListAsync<T>(string query, object parameters = null){
            var connection = new MySqlConnection(_configuration.GetConnectionString("Default"));
            var rs = await connection.QueryAsync<T>(query, parameters);
            return rs.ToList<T>();
        }
        
        public async Task<bool> UpdateAsync(string query, object parameters = null){
            using(var connection = new MySqlConnection(_configuration.GetConnectionString("Default"))){
                var rs = await connection.ExecuteAsync(query, parameters);
                return rs>0;
            }
        }

        public async Task<bool> DeleteAsync(string query, object parameters = null){
            using(var connection = new MySqlConnection(_configuration.GetConnectionString("Default"))){
                var rs = await connection.ExecuteAsync(query, parameters);
                return rs>0;
            }
        }

    }
}