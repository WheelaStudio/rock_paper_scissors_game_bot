namespace rock_paper_scissors_game_bot
{
    public enum StatisticsParameter
    {
        Win, Lose
    }
    public class DataManager
    {
        private Database dataBase;
        private static DataManager? _instance = null;
        private const int WinOrdinal = 1;
        private const int LoseOrdinal = 2;
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new();
                return _instance;
            }
        }
        public DataManager()
        {
            dataBase = Database.Instance;
        }
        public bool Registration(long id)
        {
            var isExists = IsUserExists(id);
            if (isExists)
                return true;
            else
            {
                var command = dataBase.CreateCommand($"INSERT INTO players_scores (ID,WIN,LOSE) VALUES ({id},0,0)");
                _ = command.ExecuteNonQuery();
            }
            return false;
        }
        public void ResetStatistics(long id)
        {
            var command = dataBase.CreateCommand($"UPDATE players_scores SET (WIN,LOSE)=(0,0) WHERE ID={id}");
            _ = command.ExecuteNonQuery();
        }
        public void Reconnect()
        {
            dataBase.Reconnect();
        }
        public void IncreaseStatisticsParameter(long id, StatisticsParameter statisticsParameter)
        {
            var increasedParameter = GetStatisticsParameter(id, statisticsParameter) + 1;
            var command =
                dataBase.CreateCommand($"UPDATE players_scores SET {(statisticsParameter == StatisticsParameter.Win ? "WIN" : "LOSE")} = {increasedParameter} WHERE ID = {id}");
            _ = command.ExecuteNonQuery();
        }
        public int GetStatisticsParameter(long id, StatisticsParameter statisticsParameter)
        {
            var command = dataBase.CreateCommand($"SELECT * FROM players_scores WHERE ID = {id}");
            using (var reader = command.ExecuteReader())
            {
                return reader.Read() ? reader.GetInt32(statisticsParameter == StatisticsParameter.Win ? WinOrdinal : LoseOrdinal) : -1;
            }
        }
        private bool IsUserExists(long id)
        {
            var command = dataBase.CreateCommand($"SELECT count(*) FROM players_scores WHERE ID = {id}");
            int count = Convert.ToInt32(command.ExecuteScalar());
            return count != 0;
        }
        public long[] GetAllUsersID()
        {
            var list = new List<long>();
            var command = dataBase.CreateCommand($"SELECT * FROM players_scores");
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(reader.GetInt64(0));
                }
            }
            return list.ToArray();
        }
    }
}