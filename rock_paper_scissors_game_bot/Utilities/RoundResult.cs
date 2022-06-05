namespace rock_paper_scissors_game_bot.Utilities
{
    public struct RoundResult
    {
        public string text;
        public StatisticsParameter? result;
        public RoundResult(string text, StatisticsParameter? result = null)
        {
            this.text = text;
            this.result = result;
        }
    }
}

