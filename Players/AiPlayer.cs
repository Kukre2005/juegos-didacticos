using BatallaNaval.Models;

namespace BatallaNaval.Players
{
    public enum AiDifficulty
    {
        Easy,
        Normal,
        Impossible
    }

    public class AiPlayer : Player
    {
        private readonly Random _random = new Random();
        private readonly Queue<(int Row, int Col)> _targetQueue = new Queue<(int Row, int Col)>();
        public AiDifficulty Difficulty { get; set; } = AiDifficulty.Normal;

        public AiPlayer(string name = "Capitán IA", AiDifficulty difficulty = AiDifficulty.Normal) : base(name)
        {
            IsAi = true;
            Difficulty = difficulty;
        }

        public override (int Row, int Col) SelectTarget(Board enemyBoard)
        {
            switch (Difficulty)
            {
                case AiDifficulty.Easy:
                    return SelectEasyTarget(enemyBoard);
                case AiDifficulty.Impossible:
                    return SelectImpossibleHeatmapTarget(enemyBoard);
                case AiDifficulty.Normal:
                default:
                    return SelectNormalTarget(enemyBoard);
            }
        }

        #region Dificultad Fácil
        private (int Row, int Col) SelectEasyTarget(Board enemyBoard)
        {
            List<(int Row, int Col)> validTiles = new List<(int, int)>();
            for (int r = 0; r < Board.BoardSize; r++)
            {
                for (int c = 0; c < Board.BoardSize; c++)
                {
                    if (!enemyBoard.Grid[r, c].WasAttacked)
                        validTiles.Add((r, c));
                }
            }

            if (validTiles.Count > 0)
                return validTiles[_random.Next(validTiles.Count)];

            return (0, 0);
        }
        #endregion

        #region Dificultad Normal (Hunt & Target)
        private (int Row, int Col) SelectNormalTarget(Board enemyBoard)
        {
            while (_targetQueue.Count > 0)
            {
                var target = _targetQueue.Dequeue();
                if (IsValidAndUnshot(target.Row, target.Col, enemyBoard))
                {
                    return target;
                }
            }

            List<(int Row, int Col)> huntCandidates = new List<(int, int)>();
            List<(int Row, int Col)> secondaryCandidates = new List<(int, int)>();

            for (int r = 0; r < Board.BoardSize; r++)
            {
                for (int c = 0; c < Board.BoardSize; c++)
                {
                    if (IsValidAndUnshot(r, c, enemyBoard))
                    {
                        if ((r + c) % 2 == 0)
                            huntCandidates.Add((r, c));
                        else
                            secondaryCandidates.Add((r, c));
                    }
                }
            }

            var pool = huntCandidates.Count > 0 ? huntCandidates : secondaryCandidates;
            if (pool.Count > 0)
            {
                return pool[_random.Next(pool.Count)];
            }

            return (0, 0);
        }
        #endregion

        #region Dificultad Implacable (Heatmap Probability Density Function)
        private (int Row, int Col) SelectImpossibleHeatmapTarget(Board enemyBoard)
        {
            int size = Board.BoardSize;
            int[,] probabilityMap = new int[size, size];

            // Obtener barcos enemigos que siguen vivos
            var aliveShips = enemyBoard.Fleet.Where(s => !s.IsSunk).ToList();
            if (aliveShips.Count == 0)
            {
                aliveShips = Ship.CreateStandardFleet();
            }

            // Simular colocación de cada barco vivo en todas las posiciones posibles
            foreach (var ship in aliveShips)
            {
                for (int r = 0; r < size; r++)
                {
                    for (int c = 0; c < size; c++)
                    {
                        // Probar Horizontal
                        if (CanShipFitForSimulation(enemyBoard, ship.Size, r, c, horizontal: true))
                        {
                            for (int i = 0; i < ship.Size; i++)
                                probabilityMap[r, c + i]++;
                        }

                        // Probar Vertical
                        if (CanShipFitForSimulation(enemyBoard, ship.Size, r, c, horizontal: false))
                        {
                            for (int i = 0; i < ship.Size; i++)
                                probabilityMap[r + i, c]++;
                        }
                    }
                }
            }

            // Si hay un impacto no hundido (Hit), multiplicar drásticamente los pesos de sus vecinos inmediatos
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (enemyBoard.Grid[r, c].Status == TileStatus.Hit)
                    {
                        (int, int)[] neighbors = { (r - 1, c), (r + 1, c), (r, c - 1), (r, c + 1) };
                        foreach (var (nr, nc) in neighbors)
                        {
                            if (nr >= 0 && nr < size && nc >= 0 && nc < size && !enemyBoard.Grid[nr, nc].WasAttacked)
                            {
                                probabilityMap[nr, nc] += 50; // Bonificación masiva de atracción
                            }
                        }
                    }
                }
            }

            // Buscar la casilla sin disparar con el mayor peso de probabilidad
            int maxWeight = -1;
            List<(int Row, int Col)> bestCandidates = new List<(int, int)>();

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    if (!enemyBoard.Grid[r, c].WasAttacked)
                    {
                        int weight = probabilityMap[r, c];
                        if (weight > maxWeight)
                        {
                            maxWeight = weight;
                            bestCandidates.Clear();
                            bestCandidates.Add((r, c));
                        }
                        else if (weight == maxWeight)
                        {
                            bestCandidates.Add((r, c));
                        }
                    }
                }
            }

            if (bestCandidates.Count > 0)
            {
                return bestCandidates[_random.Next(bestCandidates.Count)];
            }

            return SelectNormalTarget(enemyBoard);
        }

        private bool CanShipFitForSimulation(Board board, int shipSize, int startRow, int startCol, bool horizontal)
        {
            int size = Board.BoardSize;
            for (int i = 0; i < shipSize; i++)
            {
                int r = horizontal ? startRow : startRow + i;
                int c = horizontal ? startCol + i : startCol;

                if (r < 0 || r >= size || c < 0 || c >= size)
                    return false;

                TileStatus status = board.Grid[r, c].Status;
                // Si la casilla ya fue atacada y fue Agua o Hundido, el barco no puede ocupar este espacio
                if (status == TileStatus.Miss || status == TileStatus.Sunk)
                    return false;
            }
            return true;
        }
        #endregion

        public void RegisterShotFeedback((int Row, int Col) shotPos, ShotResult result, Ship? hitShip)
        {
            if (result == ShotResult.Hit)
            {
                var (r, c) = shotPos;
                (int, int)[] neighbors = { (r - 1, c), (r + 1, c), (r, c - 1), (r, c + 1) };
                foreach (var n in neighbors)
                {
                    if (n.Item1 >= 0 && n.Item1 < Board.BoardSize && n.Item2 >= 0 && n.Item2 < Board.BoardSize)
                    {
                        if (!_targetQueue.Contains(n))
                        {
                            _targetQueue.Enqueue(n);
                        }
                    }
                }
            }
            else if (result == ShotResult.Sunk)
            {
                _targetQueue.Clear();
            }
        }

        private bool IsValidAndUnshot(int r, int c, Board enemyBoard)
        {
            if (r < 0 || r >= Board.BoardSize || c < 0 || c >= Board.BoardSize)
                return false;

            return !enemyBoard.Grid[r, c].WasAttacked;
        }
    }
}
