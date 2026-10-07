using BatallaNaval.Models;

namespace BatallaNaval.Players
{
    public class Player
    {
        public string Name { get; }
        public Board OwnBoard { get; } = new Board();
        public Board RadarBoard { get; } = new Board();
        public int TotalShots { get; set; } = 0;
        public int TotalHits { get; set; } = 0;
        public bool IsAi { get; protected set; } = false;

        public Player(string name)
        {
            Name = name;
        }

        public double Accuracy => TotalShots == 0 ? 0 : Math.Round((double)TotalHits / TotalShots * 100, 1);

        public virtual (int Row, int Col) SelectTarget(Board enemyBoard)
        {
            return (-1, -1);
        }
    }
}
