namespace BatallaNaval.Models
{
    public enum PowerUpType
    {
        None,
        Sonar,        // Escáner 3x3
        Airstrike,    // Disparo triple en línea (3 casillas)
        DepthBomb     // Impacto en cruz de 5 casillas
    }

    public class PowerUpStatus
    {
        public bool SonarAvailable { get; set; } = true;
        public bool AirstrikeAvailable { get; set; } = true;
        public bool DepthBombAvailable { get; set; } = true;
    }

    public class PowerUpRequest
    {
        public PowerUpType Type { get; set; }
        public int TargetRow { get; set; }
        public int TargetCol { get; set; }
        public bool Horizontal { get; set; } = true; // Para Airstrike
    }

    public class PowerUpResult
    {
        public PowerUpType Type { get; set; }
        public int TargetRow { get; set; }
        public int TargetCol { get; set; }
        public string Message { get; set; } = "";
        public int DetectedCount { get; set; } = 0; // Para Sonar
        public List<(int Row, int Col, ShotResult Result, Ship? HitShip)> AffectedTiles { get; set; } = new();
    }
}
