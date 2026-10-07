namespace BatallaNaval.Models
{
    public class Ship
    {
        public string Name { get; }
        public int Size { get; }
        public char Symbol { get; }
        public int Hits { get; set; }
        public List<(int Row, int Col)> OccupiedTiles { get; } = new List<(int, int)>();

        public Ship(string name, int size, char symbol)
        {
            Name = name;
            Size = size;
            Symbol = symbol;
            Hits = 0;
        }

        public bool IsSunk => Hits >= Size;

        public static List<Ship> CreateStandardFleet()
        {
            return new List<Ship>
            {
                new Ship("Portaaviones", 5, 'P'),
                new Ship("Acorazado", 4, 'A'),
                new Ship("Crucero", 3, 'C'),
                new Ship("Submarino", 3, 'S'),
                new Ship("Destructor", 2, 'D')
            };
        }
    }
}
