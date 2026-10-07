using Microsoft.AspNetCore.Mvc;
using BatallaNaval.Models;
using BatallaNaval.Players;
using BatallaNaval.Services;

namespace BatallaNaval.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameController : ControllerBase
    {
        private static Player? _player1;
        private static Player? _player2;
        private static Player? _currentPlayer;
        private static Player? _opponent;
        private static string _gameStatus = "none";
        private static bool _isVsAi = true;
        private static AiDifficulty _aiDifficulty = AiDifficulty.Normal;
        private static string _mapType = "Standard";
        private static int _campaignLevel = 0;
        private static PowerUpStatus _p1PowerUps = new();
        private static PowerUpStatus _p2PowerUps = new();
        private static bool _p1UsedSonarHit = false;

        private readonly LeaderboardService _leaderboardService;

        public GameController(LeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        [HttpPost("start")]
        public IActionResult StartGame([FromBody] StartGameRequest request)
        {
            _isVsAi = request.VsAi;
            _aiDifficulty = Enum.TryParse<AiDifficulty>(request.Difficulty, true, out var diff) ? diff : AiDifficulty.Normal;
            _mapType = request.MapType ?? "Standard";
            _campaignLevel = request.CampaignLevel;

            if (_campaignLevel > 0)
            {
                ApplyCampaignSettings(_campaignLevel);
            }

            _player1 = new Player(string.IsNullOrWhiteSpace(request.Player1Name) ? "Comandante" : request.Player1Name);
            _p1PowerUps = new PowerUpStatus();
            _p1UsedSonarHit = false;

            if (_mapType == "Archipelago")
            {
                _player1.OwnBoard.GenerateArchipelagoMap(4);
            }
            else if (_mapType == "Minefield")
            {
                _player1.OwnBoard.GenerateMinefieldMap(3);
            }

            if (_isVsAi)
            {
                var ai = new AiPlayer("Capitán IA", _aiDifficulty);
                if (_mapType == "Archipelago") ai.OwnBoard.GenerateArchipelagoMap(4);
                else if (_mapType == "Minefield") ai.OwnBoard.GenerateMinefieldMap(3);

                ai.OwnBoard.PlaceFleetRandomly();
                _player2 = ai;
                _p2PowerUps = new PowerUpStatus();
            }
            else
            {
                _player2 = new Player(string.IsNullOrWhiteSpace(request.Player2Name) ? "Rival" : request.Player2Name);
                if (_mapType == "Archipelago") _player2.OwnBoard.GenerateArchipelagoMap(4);
                else if (_mapType == "Minefield") _player2.OwnBoard.GenerateMinefieldMap(3);

                _p2PowerUps = new PowerUpStatus();
            }

            _currentPlayer = _player1;
            _opponent = _player2;
            _gameStatus = "setup";

            return Ok(GetStateResponse());
        }

        private void ApplyCampaignSettings(int level)
        {
            switch (level)
            {
                case 1:
                    _isVsAi = true;
                    _aiDifficulty = AiDifficulty.Easy;
                    _mapType = "Standard";
                    break;
                case 2:
                    _isVsAi = true;
                    _aiDifficulty = AiDifficulty.Normal;
                    _mapType = "Archipelago";
                    break;
                case 3:
                    _isVsAi = true;
                    _aiDifficulty = AiDifficulty.Normal;
                    _mapType = "Minefield";
                    break;
                case 4:
                    _isVsAi = true;
                    _aiDifficulty = AiDifficulty.Normal;
                    _mapType = "Standard";
                    break;
                case 5:
                    _isVsAi = true;
                    _aiDifficulty = AiDifficulty.Impossible;
                    _mapType = "Archipelago";
                    break;
            }
        }

        [HttpPost("auto-place")]
        public IActionResult AutoPlaceFleet()
        {
            if (_currentPlayer == null || _gameStatus != "setup")
                return BadRequest("No se puede colocar la flota en este estado.");

            _currentPlayer.OwnBoard.PlaceFleetRandomly();

            if (_isVsAi)
            {
                _gameStatus = "playing";
            }
            else if (_currentPlayer == _player1)
            {
                _currentPlayer = _player2;
                _opponent = _player1;
            }
            else
            {
                _currentPlayer = _player1;
                _opponent = _player2;
                _gameStatus = "playing";
            }

            return Ok(GetStateResponse());
        }

        [HttpPost("place-ship")]
        public IActionResult PlaceShip([FromBody] PlaceShipRequest req)
        {
            if (_currentPlayer == null || _gameStatus != "setup")
                return BadRequest("Estado de juego inválido.");

            var shipToPlace = Ship.CreateStandardFleet().FirstOrDefault(s => s.Name.Equals(req.ShipName, StringComparison.OrdinalIgnoreCase));
            if (shipToPlace == null)
                return BadRequest("Barco no encontrado.");

            bool placed = _currentPlayer.OwnBoard.PlaceShip(shipToPlace, req.Row, req.Col, req.Horizontal);
            if (!placed)
                return BadRequest("No se puede colocar el barco en esa posición.");

            if (_currentPlayer.OwnBoard.Fleet.Count >= 5)
            {
                if (_isVsAi)
                {
                    _gameStatus = "playing";
                }
                else if (_currentPlayer == _player1)
                {
                    _currentPlayer = _player2;
                    _opponent = _player1;
                }
                else
                {
                    _currentPlayer = _player1;
                    _opponent = _player2;
                    _gameStatus = "playing";
                }
            }

            return Ok(GetStateResponse());
        }

        [HttpPost("use-powerup")]
        public IActionResult UsePowerUp([FromBody] PowerUpRequest req)
        {
            if (_currentPlayer == null || _opponent == null || _gameStatus != "playing")
                return BadRequest("El juego no está activo.");

            var pUps = _currentPlayer == _player1 ? _p1PowerUps : _p2PowerUps;

            if (req.Type == PowerUpType.Sonar)
            {
                if (!pUps.SonarAvailable) return BadRequest(new { Message = "Sonar no disponible." });
                pUps.SonarAvailable = false;

                int detected = 0;
                for (int r = req.TargetRow - 1; r <= req.TargetRow + 1; r++)
                {
                    for (int c = req.TargetCol - 1; c <= req.TargetCol + 1; c++)
                    {
                        if (r >= 0 && r < Board.BoardSize && c >= 0 && c < Board.BoardSize)
                        {
                            if (_opponent.OwnBoard.Grid[r, c].Status == TileStatus.Ship)
                            {
                                detected++;
                            }
                        }
                    }
                }

                if (detected > 0 && _currentPlayer == _player1) _p1UsedSonarHit = true;

                return Ok(new
                {
                    Type = "Sonar",
                    Message = $"Escáner de Sonar completado. {detected} casilla(s) con barcos detectada(s) en la zona 3x3.",
                    DetectedCount = detected,
                    State = GetStateResponse()
                });
            }

            if (req.Type == PowerUpType.Airstrike)
            {
                if (!pUps.AirstrikeAvailable) return BadRequest(new { Message = "Ataque Aéreo no disponible." });
                pUps.AirstrikeAvailable = false;

                List<ShotFeedback> feedbacks = new();
                for (int i = -1; i <= 1; i++)
                {
                    int r = req.Horizontal ? req.TargetRow : req.TargetRow + i;
                    int c = req.Horizontal ? req.TargetCol + i : req.TargetCol;

                    if (r >= 0 && r < Board.BoardSize && c >= 0 && c < Board.BoardSize)
                    {
                        var (result, hitShip) = _opponent.OwnBoard.ReceiveShot(r, c);
                        if (result != ShotResult.AlreadyAttacked && result != ShotResult.Invalid)
                        {
                            _currentPlayer.TotalShots++;
                            if (result == ShotResult.Hit || result == ShotResult.Sunk) _currentPlayer.TotalHits++;

                            feedbacks.Add(new ShotFeedback
                            {
                                Shooter = _currentPlayer.Name,
                                Row = r,
                                Col = c,
                                Result = result.ToString(),
                                HitShipName = hitShip?.Name
                            });
                        }
                    }
                }

                return ProcessTurnEnd(feedbacks);
            }

            if (req.Type == PowerUpType.DepthBomb)
            {
                if (!pUps.DepthBombAvailable) return BadRequest(new { Message = "Bomba de Profundidad no disponible." });
                pUps.DepthBombAvailable = false;

                (int R, int C)[] cross = { (req.TargetRow, req.TargetCol), (req.TargetRow - 1, req.TargetCol), (req.TargetRow + 1, req.TargetCol), (req.TargetRow, req.TargetCol - 1), (req.TargetRow, req.TargetCol + 1) };
                List<ShotFeedback> feedbacks = new();

                foreach (var (r, c) in cross)
                {
                    if (r >= 0 && r < Board.BoardSize && c >= 0 && c < Board.BoardSize)
                    {
                        var (result, hitShip) = _opponent.OwnBoard.ReceiveShot(r, c);
                        if (result != ShotResult.AlreadyAttacked && result != ShotResult.Invalid)
                        {
                            _currentPlayer.TotalShots++;
                            if (result == ShotResult.Hit || result == ShotResult.Sunk) _currentPlayer.TotalHits++;

                            feedbacks.Add(new ShotFeedback
                            {
                                Shooter = _currentPlayer.Name,
                                Row = r,
                                Col = c,
                                Result = result.ToString(),
                                HitShipName = hitShip?.Name
                            });
                        }
                    }
                }

                return ProcessTurnEnd(feedbacks);
            }

            return BadRequest("Power-up desconocido.");
        }

        [HttpPost("shoot")]
        public IActionResult Shoot([FromBody] ShootRequest req)
        {
            if (_currentPlayer == null || _opponent == null || _gameStatus != "playing")
                return BadRequest("El juego no está activo.");

            var (result, hitShip) = _opponent.OwnBoard.ReceiveShot(req.Row, req.Col);

            if (result == ShotResult.AlreadyAttacked || result == ShotResult.Invalid)
            {
                return BadRequest(new { Message = "Disparo inválido o casilla ya atacada." });
            }

            _currentPlayer.TotalShots++;
            bool bonusExtraTurn = false;

            if (result == ShotResult.Hit || result == ShotResult.Sunk)
            {
                _currentPlayer.TotalHits++;
                // Bono de turno extra si se impacta con un Destructor Rápido
                if (hitShip != null && hitShip.Name.Equals("Destructor", StringComparison.OrdinalIgnoreCase))
                {
                    bonusExtraTurn = true;
                }
            }

            ShotFeedback playerShot = new ShotFeedback
            {
                Shooter = _currentPlayer.Name,
                Row = req.Row,
                Col = req.Col,
                Result = result.ToString(),
                HitShipName = hitShip?.Name
            };

            return ProcessTurnEnd(new List<ShotFeedback> { playerShot }, bonusExtraTurn);
        }

        private IActionResult ProcessTurnEnd(List<ShotFeedback> playerShots, bool bonusExtraTurn = false)
        {
            List<string> unlockedAchievements = new();

            if (_opponent!.OwnBoard.AllShipsSunk())
            {
                _gameStatus = "gameover";
                int lostShips = _currentPlayer!.OwnBoard.Fleet.Count(s => s.IsSunk);
                unlockedAchievements = _leaderboardService.RecordMatchResult(_currentPlayer.Name, _opponent.Name, _currentPlayer.Accuracy, _currentPlayer.TotalShots, _aiDifficulty.ToString(), lostShips, _p1UsedSonarHit);

                return Ok(new { State = GetStateResponse(), PlayerShots = playerShots, AiShot = (ShotFeedback?)null, Winner = _currentPlayer.Name, Achievements = unlockedAchievements });
            }

            ShotFeedback? aiShot = null;

            // Si no hay bono de turno extra y es turno de la IA
            if (!bonusExtraTurn && _isVsAi && _opponent is AiPlayer ai)
            {
                var aiTarget = ai.SelectTarget(_currentPlayer!.OwnBoard);
                var (aiResult, aiHitShip) = _currentPlayer.OwnBoard.ReceiveShot(aiTarget.Row, aiTarget.Col);

                ai.TotalShots++;
                if (aiResult == ShotResult.Hit || aiResult == ShotResult.Sunk) ai.TotalHits++;

                ai.RegisterShotFeedback(aiTarget, aiResult, aiHitShip);

                aiShot = new ShotFeedback
                {
                    Shooter = ai.Name,
                    Row = aiTarget.Row,
                    Col = aiTarget.Col,
                    Result = aiResult.ToString(),
                    HitShipName = aiHitShip?.Name
                };

                if (_currentPlayer.OwnBoard.AllShipsSunk())
                {
                    _gameStatus = "gameover";
                    int lostShips = ai.OwnBoard.Fleet.Count(s => s.IsSunk);
                    _leaderboardService.RecordMatchResult(ai.Name, _currentPlayer.Name, ai.Accuracy, ai.TotalShots, _aiDifficulty.ToString(), lostShips, false);

                    return Ok(new { State = GetStateResponse(), PlayerShots = playerShots, AiShot = aiShot, Winner = ai.Name, Achievements = unlockedAchievements });
                }
            }

            return Ok(new { State = GetStateResponse(), PlayerShots = playerShots, AiShot = aiShot, BonusTurn = bonusExtraTurn });
        }

        [HttpGet("leaderboard")]
        public IActionResult GetLeaderboard()
        {
            return Ok(_leaderboardService.GetTopEntries());
        }

        [HttpGet("achievements")]
        public IActionResult GetAchievements()
        {
            return Ok(_leaderboardService.GetAchievements());
        }

        [HttpGet("state")]
        public IActionResult GetState()
        {
            if (_player1 == null) return Ok(new { Status = "none" });
            return Ok(GetStateResponse());
        }

        private object GetStateResponse()
        {
            var pUps = _currentPlayer == _player1 ? _p1PowerUps : _p2PowerUps;

            return new
            {
                GameStatus = _gameStatus,
                IsVsAi = _isVsAi,
                AiDifficulty = _aiDifficulty.ToString(),
                MapType = _mapType,
                CampaignLevel = _campaignLevel,
                CurrentPlayer = _currentPlayer?.Name,
                Player1 = FormatPlayerData(_player1),
                Player2 = FormatPlayerData(_player2),
                PowerUps = pUps,
                AvailableShips = Ship.CreateStandardFleet().Select(s => new { s.Name, s.Size, s.Symbol })
            };
        }

        private object? FormatPlayerData(Player? player)
        {
            if (player == null) return null;

            int size = Board.BoardSize;
            int[][] playerGrid = new int[size][];
            string?[][] shipNames = new string?[size][];

            for (int r = 0; r < size; r++)
            {
                playerGrid[r] = new int[size];
                shipNames[r] = new string?[size];
                for (int c = 0; c < size; c++)
                {
                    Tile tile = player.OwnBoard.Grid[r, c];
                    playerGrid[r][c] = (int)tile.Status;
                    shipNames[r][c] = tile.OccupyingShip?.Name;
                }
            }

            return new
            {
                player.Name,
                player.Accuracy,
                player.TotalShots,
                player.TotalHits,
                Fleet = player.OwnBoard.Fleet.Select(s => new { s.Name, s.Size, s.Symbol, s.Hits, s.IsSunk }),
                Grid = playerGrid,
                ShipNames = shipNames
            };
        }
    }

    public class StartGameRequest
    {
        public bool VsAi { get; set; } = true;
        public string Difficulty { get; set; } = "Normal";
        public string MapType { get; set; } = "Standard";
        public int CampaignLevel { get; set; } = 0;
        public string Player1Name { get; set; } = "Comandante";
        public string Player2Name { get; set; } = "Rival";
    }

    public class PlaceShipRequest
    {
        public string ShipName { get; set; } = "";
        public int Row { get; set; }
        public int Col { get; set; }
        public bool Horizontal { get; set; } = true;
    }

    public class ShootRequest
    {
        public int Row { get; set; }
        public int Col { get; set; }
    }

    public class ShotFeedback
    {
        public string Shooter { get; set; } = "";
        public int Row { get; set; }
        public int Col { get; set; }
        public string Result { get; set; } = "";
        public string? HitShipName { get; set; }
    }
}
