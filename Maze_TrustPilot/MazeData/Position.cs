
namespace Maze_TrustPilot.MazeData
{
    class Position
    {
        //Walls: true if the pony can advance towards that direction.
        public bool north { get; set; }
        public bool west { get; set; }
        public bool east { get; set; }
        public bool south { get; set; }
        public bool hasBeenChecked { get; set; }

        public Position()
        {
        }
    }
}
