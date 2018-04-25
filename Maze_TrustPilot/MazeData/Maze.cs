using System;
using System.Collections.Generic;
using System.Text;

namespace Maze_TrustPilot.MazeData
{
    class Maze
    {
        public Position[] positions { get; set; }
        public int ponyPosition { get; set; }
        public int domokunPosition { get; set; }
        public int endPoint { get; set; }

        public Maze()
        {
        }
    }
}
