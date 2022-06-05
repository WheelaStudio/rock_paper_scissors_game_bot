using Npgsql;
namespace rock_paper_scissors_game_bot
{
    public class Database
    {
        private readonly NpgsqlConnection connection;
        private static Database? _instance;
        public Database()
        {
            connection = new NpgsqlConnection(GetConnectionString());
            connection.Open();
        }
        public static Database Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new();
                return _instance;
            }
        }
        public void Reconnect()
        {
            connection.Close();
            connection.ConnectionString = GetConnectionString();
            connection.Open();
        }
        private string GetConnectionString()
        {
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL") ?? "";
            var databaseUri = new Uri(databaseUrl);
            var userInfo = databaseUri.UserInfo.Split(':');
            var builder = new NpgsqlConnectionStringBuilder
            {
                Host = databaseUri.Host,
                Port = databaseUri.Port,
                Username = userInfo[0],
                Password = userInfo[1],
                Database = databaseUri.LocalPath.TrimStart('/')
            };
            return builder.ToString();
        }
        public NpgsqlCommand CreateCommand(string commandText)
        {
            var command = connection.CreateCommand();
            command.Connection = connection;
            command.CommandText = commandText;
            return command;
        }
    }
}

