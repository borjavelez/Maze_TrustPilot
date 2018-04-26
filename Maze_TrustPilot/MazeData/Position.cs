
using System.Collections.Generic;

namespace Maze_TrustPilot.MazeData
{
    class Position
    {
        //In a dictionary we will store the available directions as ints (positions) and
        //the coordinates as strings
        public Dictionary<string, int> coordinates { get; set; }
        public bool hasBeenChecked { get; set; }

        public Position()
        {
        }
    }
}
