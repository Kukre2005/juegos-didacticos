namespace BatallaNaval.Models
{
    public enum ShotResult
    {
        Invalid,
        AlreadyAttacked,
        Miss,
        Hit,
        Sunk,
        IslandHit,
        MineHit
    }

    public class Board
    {
        public const int BoardSize = 10;
        public Tile[,] Grid { get; } = new Tile[BoardSize, BoardSize];
        public List<Ship> Fleet { get; private set; } = new List<Ship>();

        public Board()
        {
            InitializeGrid();
        }

        private void InitializeGrid()
        {
            for (int r = 0; r < BoardSize; r++)
            {
                for (int c = 0; c < BoardSize; c++)
                {
                    Grid[r, c] = new Tile(r, c);
                }
            }
        }

        public void GenerateArchipelagoMap(int islandCount = 4)
        {
            Random rng = new Random();
            int count = 0;
            while (count < islandCount)
            {
                int r = rng.Next(0, BoardSize);
                int c = rng.Next(0, BoardSize);
                if (Grid[r, c].Status == TileStatus.Empty)
                {
                    Grid[r, c].Status = TileStatus.Island;
                    count++;
                }
            }
        }

        public void GenerateMinefieldMap(int mineCount = 3)
        {
            Random rng = new Random();
            int count = 0;
            while (count < mineCount)
            {
                int r = rng.Next(0, BoardSize);
                int c = rng.Next(0, BoardSize);
                if (Grid[r, c].Status == TileStatus.Empty)
                {
                    Grid[r, c].Status = TileStatus.Mine;
                    count++;
                }
            }
        }

        public bool CanPlaceShip(Ship ship, int startRow, int startCol, bool horizontal)
        {
            for (int i = 0; i < ship.Size; i++)
            {
                int r = horizontal ? startRow : startRow + i;
                int c = horizontal ? startCol + i : startCol;

                if (r < 0 || r >= BoardSize || c < 0 || c >= BoardSize)
                    return false;

                if (Grid[r, c].Status != TileStatus.Empty && Grid[r, c].Status != TileStatus.Mine)
                    return false;
            }
            return true;
        }

        public bool PlaceShip(Ship ship, int startRow, int startCol, bool horizontal)
        {
            if (!CanPlaceShip(ship, startRow, startCol, horizontal))
                return false;

            ship.OccupiedTiles.Clear();
            for (int i = 0; i < ship.Size; i++)
            {
                int r = horizontal ? startRow : startRow + i;
                int c = horizontal ? startCol + i : startCol;

                Grid[r, c].Status = TileStatus.Ship;
                Grid[r, c].OccupyingShip = ship;
                ship.OccupiedTiles.Add((r, c));
            }

            if (!Fleet.Contains(ship))
            {
                Fleet.Add(ship);
            }

            return true;
        }

        public void PlaceFleetRandomly(List<Ship>? customFleet = null)
        {
            Fleet.Clear();
            var fleetToPlace = customFleet ?? Ship.CreateStandardFleet();
            Random rng = new Random();

            foreach (var ship in fleetToPlace)
            {
                bool placed = false;
                int attempts = 0;
                while (!placed && attempts < 1000)
                {
                    attempts++;
                    int r = rng.Next(0, BoardSize);
                    int c = rng.Next(0, BoardSize);
                    bool horizontal = rng.Next(2) == 0;

                    if (PlaceShip(ship, r, c, horizontal))
                    {
                        placed = true;
                    }
                }
            }
        }

        public (ShotResult Result, Ship? HitShip) ReceiveShot(int row, int col)
        {
            if (row < 0 || row >= BoardSize || col < 0 || col >= BoardSize)
                return (ShotResult.Invalid, null);

            var tile = Grid[row, col];

            if (tile.WasAttacked)
                return (ShotResult.AlreadyAttacked, tile.OccupyingShip);

            if (tile.Status == TileStatus.Island)
            {
                return (ShotResult.IslandHit, null);
            }

            if (tile.Status == TileStatus.Mine)
            {
                tile.Status = TileStatus.Miss;
                return (ShotResult.MineHit, null);
            }

            if (tile.Status == TileStatus.Empty)
            {
                tile.Status = TileStatus.Miss;
                return (ShotResult.Miss, null);
            }

            if (tile.Status == TileStatus.Ship && tile.OccupyingShip != null)
            {
                var ship = tile.OccupyingShip;

                // Si es un Acorazado blindado en su casilla central
                if (ship.Name.Equals("Acorazado", StringComparison.OrdinalIgnoreCase) && tile.ArmorHits < 1)
                {
                    tile.ArmorHits++;
                    return (ShotResult.Hit, ship);
                }

                ship.Hits++;

                if (ship.IsSunk)
                {
                    foreach (var (r, c) in ship.OccupiedTiles)
                    {
                        Grid[r, c].Status = TileStatus.Sunk;
                    }
                    return (ShotResult.Sunk, ship);
                }
                else
                {
                    tile.Status = TileStatus.Hit;
                    return (ShotResult.Hit, ship);
                }
            }

            return (ShotResult.Invalid, null);
        }

        public bool AllShipsSunk()
        {
            return Fleet.Count > 0 && Fleet.All(s => s.IsSunk);
        }
    }
}
