using Mono.Unix;
using Mono.Unix.Native;
namespace rock_paper_scissors_game_bot.Utilities
{
    public class UnixExitSignal
    {
        private event EventHandler OnExecute;
        private readonly UnixSignal[] signals = new UnixSignal[]{
        new UnixSignal(Signum.SIGTERM),
    };
        public UnixExitSignal(EventHandler OnExecute)
        {
            this.OnExecute = OnExecute;
        }
        public void Wait()
        {
            _ = Task.Factory.StartNew(() =>
            {
                _ = UnixSignal.WaitAny(signals, -1);
                OnExecute(null, EventArgs.Empty);
            });
        }
    }
}

