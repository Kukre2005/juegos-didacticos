using System.Text;
using BatallaNaval.Models;
using BatallaNaval.Players;

namespace BatallaNaval.UI
{
    public static class ConsoleUI
    {
        // Códigos ANSI para colores enriquecidos en consola
        public const string Reset = "\u001b[0m";
        public const string Bold = "\u001b[1m";
        public const string Cyan = "\u001b[36m";
        public const string Green = "\u001b[32m";
        public const string Red = "\u001b[31m";
        public const string Yellow = "\u001b[33m";
        public const string Blue = "\u001b[34m";
        public const string Magenta = "\u001b[35m";
        public const string DarkGray = "\u001b[90m";
        public const string BrightWhite = "\u001b[97m";
        public const string BgBlue = "\u001b[44m";
        public const string BgRed = "\u001b[41m";

        public static void InitializeConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.Title = "⚡ BATALLA NAVAL - NAVAL STRIKE ⚡";
        }

        public static void DrawHeader()
        {
            Console.Clear();
            Console.WriteLine($"{Cyan}{Bold}========================================================================{Reset}");
            Console.WriteLine($"{Magenta}{Bold}  ⚓  B A T A L L A   N A V A L  -  N A V A L   S T R I K E  ⚓{Reset}");
            Console.WriteLine($"{Cyan}{Bold}========================================================================{Reset}\n");
        }

        public static void DisplayDualBoards(Player currentPlayer, Player opponent, bool showOpponentShips = false)
        {
            DrawHeader();

            Console.WriteLine($" Turno actual: {BrightWhite}{Bold}{currentPlayer.Name}{Reset}");
            Console.WriteLine($" Estadísticas: Precisión {Green}{currentPlayer.Accuracy}%{Reset} | Disparos: {Yellow}{currentPlayer.TotalShots}{Reset} | Flota enemiga restante: {Red}{opponent.OwnBoard.Fleet.Count(s => !s.IsSunk)}{Reset}/{opponent.OwnBoard.Fleet.Count}\n");

            string leftHeader = "         TU FLOTA (DEFENSA)";
            string rightHeader = "       RADAR DE ATAQUE (OFENSIVA)";

            Console.WriteLine($"{Cyan}{Bold}{leftHeader,-40} {rightHeader}{Reset}");
            Console.WriteLine("    A B C D E F G H I J              A B C D E F G H I J");
            Console.WriteLine("  +---------------------+          +---------------------+");

            for (int r = 0; r < Board.BoardSize; r++)
            {
                // Tablero Izquierdo (Propio)
                StringBuilder leftRow = new StringBuilder();
                leftRow.Append($"{(r + 1),2}| ");

                for (int c = 0; c < Board.BoardSize; c++)
                {
                    Tile tile = currentPlayer.OwnBoard.Grid[r, c];
                    leftRow.Append(GetTileSymbol(tile, isRadar: false, showShips: true)).Append(" ");
                }
                leftRow.Append("|");

                // Tablero Derecho (Radar / Ataque)
                StringBuilder rightRow = new StringBuilder();
                rightRow.Append($"       {(r + 1),2}| ");

                for (int c = 0; c < Board.BoardSize; c++)
                {
                    Tile tile = opponent.OwnBoard.Grid[r, c];
                    rightRow.Append(GetTileSymbol(tile, isRadar: true, showShips: showOpponentShips)).Append(" ");
                }
                rightRow.Append("|");

                Console.WriteLine(leftRow.ToString() + rightRow.ToString());
            }

            Console.WriteLine("  +---------------------+          +---------------------+");
            DrawLegend();
        }

        private static string GetTileSymbol(Tile tile, bool isRadar, bool showShips)
        {
            switch (tile.Status)
            {
                case TileStatus.Empty:
                    return $"{Blue}~{Reset}";

                case TileStatus.Ship:
                    if (showShips)
                    {
                        char symbol = tile.OccupyingShip?.Symbol ?? 'O';
                        return $"{Green}{Bold}{symbol}{Reset}";
                    }
                    return $"{Blue}~{Reset}";

                case TileStatus.Miss:
                    return $"{Yellow}*{Reset}";

                case TileStatus.Hit:
                    return $"{Red}{Bold}X{Reset}";

                case TileStatus.Sunk:
                    return $"{BgRed}{BrightWhite}{Bold}#{Reset}";

                default:
                    return $"{Blue}~{Reset}";
            }
        }

        private static void DrawLegend()
        {
            Console.WriteLine($"\n Leyenda: {Blue}~ Agua{Reset} | {Green}O/P/A/C/S/D Barco{Reset} | {Yellow}* Agua (Fallo){Reset} | {Red}X Impacto{Reset} | {BgRed}{BrightWhite}#{Reset} Hundido\n");
        }

        public static (int Row, int Col)? PromptCoordinate(string promptMessage)
        {
            while (true)
            {
                Console.Write($"{BrightWhite}{promptMessage} (ej: A5, J10, 'Q' para salir): {Reset}");
                string? input = Console.ReadLine()?.Trim().ToUpper();

                if (string.IsNullOrEmpty(input)) continue;
                if (input == "Q") return null;

                if (TryParseCoordinate(input, out int row, out int col))
                {
                    return (row, col);
                }

                Console.WriteLine($"{Red}⚠️ Coordenada inválida. Ingresa una letra (A-J) seguida de un número (1-10). Ej: C4{Reset}");
            }
        }

        public static bool TryParseCoordinate(string input, out int row, out int col)
        {
            row = -1;
            col = -1;

            if (input.Length < 2 || input.Length > 3) return false;

            char colChar = input[0];
            if (colChar < 'A' || colChar > 'J') return false;

            string numPart = input.Substring(1);
            if (!int.TryParse(numPart, out int rowNum) || rowNum < 1 || rowNum > 10) return false;

            col = colChar - 'A';
            row = rowNum - 1;

            return true;
        }

        public static bool PromptOrientation()
        {
            while (true)
            {
                Console.Write($"{BrightWhite}Selecciona orientación [(H)orizontal / (V)ertical]: {Reset}");
                string? input = Console.ReadLine()?.Trim().ToUpper();

                if (input == "H") return true;
                if (input == "V") return false;

                Console.WriteLine($"{Red}⚠️ Opción no válida. Escribe H o V.{Reset}");
            }
        }

        public static void ShowShotResult(ShotResult result, Ship? ship, string shooterName)
        {
            PlaySound(result);

            switch (result)
            {
                case ShotResult.Miss:
                    Console.WriteLine($"💥 {Yellow}{Bold}{shooterName} disparó al AGUA.*{Reset}");
                    break;
                case ShotResult.Hit:
                    Console.WriteLine($"🔥 {Red}{Bold}¡ IMPACTO DIRECTO ! {shooterName} ha acertado en un barco.{Reset}");
                    break;
                case ShotResult.Sunk:
                    Console.WriteLine($"☠️ {BgRed}{BrightWhite}{Bold} ¡ BARCO HUNDIDO ! {shooterName} ha destruido el {ship?.Name ?? "barco"}. {Reset}");
                    break;
                case ShotResult.AlreadyAttacked:
                    Console.WriteLine($"⚠️ {Yellow}Esa casilla ya había sido atacada anteriormente.{Reset}");
                    break;
                case ShotResult.Invalid:
                    Console.WriteLine($"⚠️ {Red}Coordenada fuera del tablero.{Reset}");
                    break;
            }
        }

        public static void PlaySound(ShotResult result)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    switch (result)
                    {
                        case ShotResult.Miss:
                            Console.Beep(300, 120);
                            break;
                        case ShotResult.Hit:
                            Console.Beep(800, 150);
                            break;
                        case ShotResult.Sunk:
                            Console.Beep(1000, 150);
                            Console.Beep(600, 200);
                            break;
                    }
                }
            }
            catch
            {
                // Ignorar si el sistema de audio de la consola no está disponible
            }
        }

        public static void PressEnterToContinue(string msg = "Presiona ENTER para continuar...")
        {
            Console.WriteLine($"\n{DarkGray}{msg}{Reset}");
            Console.ReadLine();
        }
    }
}
