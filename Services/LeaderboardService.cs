using System.Text.Json;

namespace BatallaNaval.Services
{
    public class LeaderboardEntry
    {
        public string PlayerName { get; set; } = "";
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public double BestAccuracy { get; set; } = 0;
        public int BestTurns { get; set; } = 999;
    }

    public class Achievement
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Icon { get; set; } = "🏅";
        public bool IsUnlocked { get; set; } = false;
        public string? UnlockedAt { get; set; } = null;
    }

    public class LeaderboardData
    {
        public List<LeaderboardEntry> Entries { get; set; } = new();
        public List<Achievement> Achievements { get; set; } = new();
    }

    public class LeaderboardService
    {
        private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "leaderboard.json");
        private LeaderboardData _data = new();

        public LeaderboardService()
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    string json = File.ReadAllText(_filePath);
                    _data = JsonSerializer.Deserialize<LeaderboardData>(json) ?? new LeaderboardData();
                }
                else
                {
                    InitializeDefaultAchievements();
                    SaveData();
                }
            }
            catch
            {
                InitializeDefaultAchievements();
            }
        }

        private void InitializeDefaultAchievements()
        {
            _data.Achievements = new List<Achievement>
            {
                new Achievement { Id = "SHARPSHOOTER", Title = "Tirador de Élite", Description = "Obtén una precisión de disparo superior al 50% en una victoria.", Icon = "🎯" },
                new Achievement { Id = "LIGHTNING_WIN", Title = "Victoria Relámpago", Description = "Gana una partida en 30 turnos o menos.", Icon = "⚡" },
                new Achievement { Id = "SONAR_MASTER", Title = "Maestro del Sonar", Description = "Detecta al menos 2 barcos enemigos con el escáner de sonar.", Icon = "📡" },
                new Achievement { Id = "UNSTOPPABLE", Title = "Implacable", Description = "Derrota a la IA en la dificultad Implacable.", Icon = "🔥" },
                new Achievement { Id = "FLAWLESS", Title = "Defensa Inexpugnable", Description = "Gana una partida perdiendo 1 barco o menos.", Icon = "🛡️" }
            };
        }

        public void SaveData()
        {
            try
            {
                string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
                File.ReadAllText(_filePath); // Check access
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Fallback silencioso por permisos de archivo
            }
        }

        public List<LeaderboardEntry> GetTopEntries()
        {
            return _data.Entries.OrderByDescending(e => e.Wins).ThenByDescending(e => e.BestAccuracy).Take(10).ToList();
        }

        public List<Achievement> GetAchievements()
        {
            return _data.Achievements;
        }

        public List<string> RecordMatchResult(string winnerName, string loserName, double accuracy, int turns, string difficulty, int winnerLostShips, bool usedSonarHit)
        {
            List<string> newlyUnlocked = new();

            // Actualizar o crear entrada del ganador
            var entry = _data.Entries.FirstOrDefault(e => e.PlayerName.Equals(winnerName, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
            {
                entry = new LeaderboardEntry { PlayerName = winnerName };
                _data.Entries.Add(entry);
            }

            entry.Wins++;
            if (accuracy > entry.BestAccuracy) entry.BestAccuracy = accuracy;
            if (turns < entry.BestTurns) entry.BestTurns = turns;

            // Actualizar entrada del perdedor si no es IA
            if (!loserName.Contains("IA", StringComparison.OrdinalIgnoreCase))
            {
                var loserEntry = _data.Entries.FirstOrDefault(e => e.PlayerName.Equals(loserName, StringComparison.OrdinalIgnoreCase));
                if (loserEntry == null)
                {
                    loserEntry = new LeaderboardEntry { PlayerName = loserName };
                    _data.Entries.Add(loserEntry);
                }
                loserEntry.Losses++;
            }

            // Comprobar logros
            if (accuracy >= 50.0 && UnlockAchievement("SHARPSHOOTER")) newlyUnlocked.Add("Tirador de Élite 🎯");
            if (turns <= 30 && UnlockAchievement("LIGHTNING_WIN")) newlyUnlocked.Add("Victoria Relámpago ⚡");
            if (usedSonarHit && UnlockAchievement("SONAR_MASTER")) newlyUnlocked.Add("Maestro del Sonar 📡");
            if (difficulty.Equals("Impossible", StringComparison.OrdinalIgnoreCase) && UnlockAchievement("UNSTOPPABLE")) newlyUnlocked.Add("Implacable 🔥");
            if (winnerLostShips <= 1 && UnlockAchievement("FLAWLESS")) newlyUnlocked.Add("Defensa Inexpugnable 🛡️");

            SaveData();
            return newlyUnlocked;
        }

        private bool UnlockAchievement(string id)
        {
            var ach = _data.Achievements.FirstOrDefault(a => a.Id == id);
            if (ach != null && !ach.IsUnlocked)
            {
                ach.IsUnlocked = true;
                ach.UnlockedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
                return true;
            }
            return false;
        }
    }
}
