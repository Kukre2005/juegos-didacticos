namespace BatallaNaval.Models
{
    public enum TileStatus
    {
        Empty,   // Agua no atacada
        Ship,    // Barco no atacado
        Miss,    // Disparo al agua (*)
        Hit,     // Impacto en barco (X)
        Sunk,    // Barco hundido (#)
        Island,  // Isla/Arrecife de piedra (▲)
        Mine     // Mina naval oculta
    }

    public class Tile
    {
        public int Row { get; }
        public int Col { get; }
        public TileStatus Status { get; set; } = TileStatus.Empty;
        public Ship? OccupyingShip { get; set; } = null;
        public int ArmorHits { get; set; } = 0;

        public Tile(int row, int col)
        {
            Row = row;
            Col = col;
        }

        public bool WasAttacked => Status == TileStatus.Miss || Status == TileStatus.Hit || Status == TileStatus.Sunk;
    }
}
