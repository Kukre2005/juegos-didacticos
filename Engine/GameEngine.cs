using BatallaNaval.Models;
using BatallaNaval.Players;
using BatallaNaval.UI;

namespace BatallaNaval.Engine
{
    public class GameEngine
    {
        private Player _player1;
        private Player _player2;
        private bool _isVsAi;

        public GameEngine()
        {
            _player1 = new Player("Jugador 1");
            _player2 = new AiPlayer("Capitán IA");
            _isVsAi = true;
        }

        public void StartNewGame(string[]? args = null)
        {
            ConsoleUI.InitializeConsole();

            if (args != null && args.Contains("--test"))
            {
                RunAutomatedSimulation();
                return;
            }

            ShowMainMenu();
        }

        public void RunAutomatedSimulation()
        {
            Console.WriteLine("🤖 Ejecutando simulación de prueba de IA vs IA...");
            var ai1 = new AiPlayer("IA Alfa");
            var ai2 = new AiPlayer("IA Beta");

            ai1.OwnBoard.PlaceFleetRandomly();
            ai2.OwnBoard.PlaceFleetRandomly();

            Player current = ai1;
            Player opponent = ai2;
            int turns = 0;

            while (turns < 200)
            {
                turns++;
                var aiCurrent = (AiPlayer)current;
                var target = aiCurrent.SelectTarget(opponent.OwnBoard);
                var (result, hitShip) = opponent.OwnBoard.ReceiveShot(target.Row, target.Col);

                current.TotalShots++;
                if (result == ShotResult.Hit || result == ShotResult.Sunk) current.TotalHits++;

                aiCurrent.RegisterShotFeedback(target, result, hitShip);

                if (opponent.OwnBoard.AllShipsSunk())
                {
                    Console.WriteLine($"✅ SIMULACIÓN EXITOSA: {current.Name} ganó en {turns} turnos! Precisión: {current.Accuracy}%");
                    return;
                }

                var temp = current;
                current = opponent;
                opponent = temp;
            }

            Console.WriteLine("⚠️ La simulación terminó por límite de turnos sin fallos.");
        }

        private void ShowMainMenu()
        {
            while (true)
            {
                ConsoleUI.DrawHeader();
                Console.WriteLine($"{ConsoleUI.BrightWhite}{ConsoleUI.Bold}      MENÚ PRINCIPAL{ConsoleUI.Reset}\n");
                Console.WriteLine($"  1. 🤖 Jugador vs IA (Computadora Inteligente)");
                Console.WriteLine($"  2. 👥 Jugador vs Jugador (Multijugador Local)");
                Console.WriteLine($"  3. 📜 Reglas del Juego");
                Console.WriteLine($"  4. 🚪 Salir\n");
                Console.Write($"{ConsoleUI.BrightWhite}Selecciona una opción (1-4): {ConsoleUI.Reset}");

                string? choice = Console.ReadLine()?.Trim();
                switch (choice)
                {
                    case "1":
                        SetupGame(vsAi: true);
                        RunGameLoop();
                        break;
                    case "2":
                        SetupGame(vsAi: false);
                        RunGameLoop();
                        break;
                    case "3":
                        ShowRules();
                        break;
                    case "4":
                        Console.WriteLine($"\n¡Gracias por jugar Batalla Naval! Hasta pronto.\n");
                        return;
                    default:
                        Console.WriteLine($"{ConsoleUI.Red}Opción inválida. Presiona ENTER para intentar de nuevo.{ConsoleUI.Reset}");
                        Console.ReadLine();
                        break;
                }
            }
        }

        private void ShowRules()
        {
            ConsoleUI.DrawHeader();
            Console.WriteLine($"{ConsoleUI.BrightWhite}{ConsoleUI.Bold}📜 REGLAS DE LA BATALLA NAVAL{ConsoleUI.Reset}\n");
            Console.WriteLine(" 1. Cada jugador posee una flota de 5 barcos en una cuadrícula de 10x10.");
            Console.WriteLine(" 2. Flota disponible:");
            Console.WriteLine("    - Portaaviones (5 casillas - 'P')");
            Console.WriteLine("    - Acorazado (4 casillas - 'A')");
            Console.WriteLine("    - Crucero (3 casillas - 'C')");
            Console.WriteLine("    - Submarino (3 casillas - 'S')");
            Console.WriteLine("    - Destructor (2 casillas - 'D')");
            Console.WriteLine(" 3. En cada turno, ingresa las coordenadas de tu disparo (ej: B5).");
            Console.WriteLine(" 4. Un disparo exitoso revelará un IMPACTO (X) o HUNDIDO (#).");
            Console.WriteLine(" 5. El primer jugador en hundir TODOS los barcos enemigos gana la partida.");
            ConsoleUI.PressEnterToContinue();
        }

        private void SetupGame(bool vsAi)
        {
            _isVsAi = vsAi;

            ConsoleUI.DrawHeader();
            Console.Write($"{ConsoleUI.BrightWhite}Ingresa el nombre del Jugador 1: {ConsoleUI.Reset}");
            string p1Name = Console.ReadLine()?.Trim() ?? "";
            if (string.IsNullOrEmpty(p1Name)) p1Name = "Jugador 1";
            _player1 = new Player(p1Name);

            if (vsAi)
            {
                _player2 = new AiPlayer("Capitán IA");
            }
            else
            {
                Console.Write($"{ConsoleUI.BrightWhite}Ingresa el nombre del Jugador 2: {ConsoleUI.Reset}");
                string p2Name = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrEmpty(p2Name)) p2Name = "Jugador 2";
                _player2 = new Player(p2Name);
            }

            // Fase de Colocación de Barcos
            SetupFleet(_player1);
            SetupFleet(_player2);
        }

        private void SetupFleet(Player player)
        {
            if (player is AiPlayer ai)
            {
                ai.OwnBoard.PlaceFleetRandomly();
                return;
            }

            ConsoleUI.DrawHeader();
            Console.WriteLine($"⚓ FASE DE COLOCACIÓN DE FLOTA - {ConsoleUI.BrightWhite}{ConsoleUI.Bold}{player.Name}{ConsoleUI.Reset}\n");
            Console.WriteLine(" ¿Cómo deseas colocar tus barcos?");
            Console.WriteLine(" 1. 🎲 Colocación Aleatoria Automática (Recomendado)");
            Console.WriteLine(" 2. 🧩 Colocación Manual Coordenada a Coordenada");
            Console.Write($"\n{ConsoleUI.BrightWhite}Opción (1 o 2): {ConsoleUI.Reset}");

            string? choice = Console.ReadLine()?.Trim();
            if (choice == "2")
            {
                PlaceFleetManually(player);
            }
            else
            {
                player.OwnBoard.PlaceFleetRandomly();
                ConsoleUI.DrawHeader();
                Console.WriteLine($"{ConsoleUI.Green}¡Flota colocada aleatoriamente con éxito!{ConsoleUI.Reset}");
                ConsoleUI.PressEnterToContinue();
            }
        }

        private void PlaceFleetManually(Player player)
        {
            var fleet = Ship.CreateStandardFleet();

            foreach (var ship in fleet)
            {
                bool placed = false;
                while (!placed)
                {
                    ConsoleUI.DrawHeader();
                    Console.WriteLine($" Colocando barco: {ConsoleUI.Green}{ConsoleUI.Bold}{ship.Name}{ConsoleUI.Reset} ({ship.Size} casillas)\n");
                    
                    // Mostrar tablero actual
                    Console.WriteLine(" Tablero actual:");
                    for (int r = 0; r < Board.BoardSize; r++)
                    {
                        Console.Write($"{(r + 1),2}| ");
                        for (int c = 0; c < Board.BoardSize; c++)
                        {
                            var tile = player.OwnBoard.Grid[r, c];
                            if (tile.Status == TileStatus.Ship)
                                Console.Write($"{ConsoleUI.Green}{tile.OccupyingShip?.Symbol ?? 'O'}{ConsoleUI.Reset} ");
                            else
                                Console.Write($"{ConsoleUI.Blue}~{ConsoleUI.Reset} ");
                        }
                        Console.WriteLine("|");
                    }
                    Console.WriteLine();

                    var coord = ConsoleUI.PromptCoordinate($"Coordenada inicial para el {ship.Name}");
                    if (!coord.HasValue) return;

                    bool horizontal = ConsoleUI.PromptOrientation();

                    if (player.OwnBoard.PlaceShip(ship, coord.Value.Row, coord.Value.Col, horizontal))
                    {
                        placed = true;
                    }
                    else
                    {
                        Console.WriteLine($"\n{ConsoleUI.Red}⚠️ Posición inválida. El barco no cabe o se sobrepone con otro.{ConsoleUI.Reset}");
                        ConsoleUI.PressEnterToContinue("Presiona ENTER para reintentar...");
                    }
                }
            }
        }

        private void RunGameLoop()
        {
            Player currentPlayer = _player1;
            Player opponent = _player2;

            while (true)
            {
                if (!currentPlayer.IsAi)
                {
                    ConsoleUI.DisplayDualBoards(currentPlayer, opponent);
                }

                (int Row, int Col) targetCoord;

                if (currentPlayer is AiPlayer ai)
                {
                    Console.WriteLine($"\n🤖 {ConsoleUI.Cyan}Turno de la IA ({ai.Name}). Calculando disparo...{ConsoleUI.Reset}");
                    Thread.Sleep(1000);
                    targetCoord = ai.SelectTarget(opponent.OwnBoard);
                }
                else
                {
                    var inputCoord = ConsoleUI.PromptCoordinate($"[{currentPlayer.Name}] Ingrese coordenada de disparo");
                    if (!inputCoord.HasValue)
                    {
                        Console.WriteLine($"{ConsoleUI.Yellow}Partida cancelada por el jugador.{ConsoleUI.Reset}");
                        return;
                    }
                    targetCoord = inputCoord.Value;
                }

                // Ejecutar disparo
                var (result, hitShip) = opponent.OwnBoard.ReceiveShot(targetCoord.Row, targetCoord.Col);

                if (result == ShotResult.AlreadyAttacked)
                {
                    if (!currentPlayer.IsAi)
                    {
                        ConsoleUI.ShowShotResult(result, hitShip, currentPlayer.Name);
                        ConsoleUI.PressEnterToContinue();
                    }
                    continue; // Repetir turno sin contar estadísticas ni cambiar jugador
                }

                // Registrar estadísticas
                currentPlayer.TotalShots++;
                if (result == ShotResult.Hit || result == ShotResult.Sunk)
                {
                    currentPlayer.TotalHits++;
                }

                // Si es la IA, registrar retroalimentación para afinar su algoritmo Hunt/Target
                if (currentPlayer is AiPlayer aiPlayer)
                {
                    aiPlayer.RegisterShotFeedback(targetCoord, result, hitShip);
                }

                // Mostrar resultado del disparo
                string coordStr = $"{(char)('A' + targetCoord.Col)}{targetCoord.Row + 1}";
                Console.WriteLine($"\n🎯 Disparo a {ConsoleUI.BrightWhite}{ConsoleUI.Bold}{coordStr}{ConsoleUI.Reset}:");
                ConsoleUI.ShowShotResult(result, hitShip, currentPlayer.Name);

                // Comprobar condición de victoria
                if (opponent.OwnBoard.AllShipsSunk())
                {
                    ShowGameOverScreen(currentPlayer, opponent);
                    return;
                }

                ConsoleUI.PressEnterToContinue();

                // Alternar turno
                var temp = currentPlayer;
                currentPlayer = opponent;
                opponent = temp;
            }
        }

        private void ShowGameOverScreen(Player winner, Player loser)
        {
            ConsoleUI.DrawHeader();
            Console.WriteLine($"{ConsoleUI.Magenta}{ConsoleUI.Bold}========================================================================{ConsoleUI.Reset}");
            Console.WriteLine($"{ConsoleUI.Green}{ConsoleUI.Bold}🏆 ¡ V I C T O R I A !   G A N A D O R :  {winner.Name.ToUpper()} 🏆{ConsoleUI.Reset}");
            Console.WriteLine($"{ConsoleUI.Magenta}{ConsoleUI.Bold}========================================================================{ConsoleUI.Reset}\n");

            Console.WriteLine($"📊 {ConsoleUI.BrightWhite}{ConsoleUI.Bold}ESTADÍSTICAS FINALES DE LA PARTIDA:{ConsoleUI.Reset}\n");
            Console.WriteLine($" 🥇 GANADOR: {ConsoleUI.Green}{winner.Name}{ConsoleUI.Reset}");
            Console.WriteLine($"    - Precisión de disparo: {ConsoleUI.Green}{winner.Accuracy}%{ConsoleUI.Reset}");
            Console.WriteLine($"    - Total de disparos realizados: {winner.TotalShots}");
            Console.WriteLine($"    - Impactos certeros: {winner.TotalHits}\n");

            Console.WriteLine($" 🥈 DERROTADO: {ConsoleUI.Red}{loser.Name}{ConsoleUI.Reset}");
            Console.WriteLine($"    - Precisión de disparo: {loser.Accuracy}%");
            Console.WriteLine($"    - Barcos restantes: 0/{loser.OwnBoard.Fleet.Count}\n");

            ConsoleUI.PressEnterToContinue("Presiona ENTER para regresar al Menú Principal...");
        }
    }
}
