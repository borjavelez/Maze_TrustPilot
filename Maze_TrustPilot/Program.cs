using Maze_TrustPilot.MazeData;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Maze_TrustPilot
{
    /* The proposed maze has only 1 possible path from the position of the pony to the end-point.
     * Firstly I am going to parse the JSON file and to create the different positions with the information received.
     * Each position will have coordinates as booleans, being true those who are accesible (no walls).
     * With my solution, I am going to calculate the path, before making any movements, using the Trémaux's algorithm.
     * I am going to move the pony until it reaches one point with no exit, then mark that point as inaccessible.
     * By doing that, all the ways that lead to a close end will be innacessible by our pony.
     * Once the path has been calculated I will send the movements to the API provided.
     * The Domokun is going to try to catch our pony. Since there is only one path to get to the end-point, if the 
     * Domokun gets between the pony and the end-point, it is going to be almost impossible to reach it.
     * In case the Domokun gets into our path, the pony is going to calculate 10 random paths from its exact position
     * and it is going to choose the longest one. The pony it is going to run through that path with the Domokun chasing it.
     * If the Domokun makes a mistake and leaves the path, the Pony is going to stop and try to reach the end-point again.
     * To reach the end-point again, the Domokun must make 2 mistakes in a row, which is very unlikely.
     * */
    class Program
    {
        static int weight = 15;
        static int height = 25;
        static bool repeat = true;
        static bool ponyDied = false;
        static String json;
        static Maze maze;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static string _mazeId = "";
        private static string _mazeUri = "";

        static void Main(string[] args)
        {
            while (_mazeId != "quit")
            {
                while (repeat)
                {
                    repeat = false;
                    Console.WriteLine("Input the maze Id: (Write 'quit' to exit)");
                    _mazeId = Console.ReadLine();
                    _mazeUri = "https://ponychallenge.trustpilot.com/pony-challenge/maze/" + _mazeId;
                    try
                    {
                        maze = new Maze();
                        maze = getMaze();
                    }
                    catch (Exception e)
                    {
                        repeat = true;
                        Console.WriteLine("Wrong id! Try again");
                    }
                }
                Console.WriteLine("Maze loaded");
                Console.WriteLine("Solving...");
                int[] path = calculatePath();
                sendDirections(path);
                maze = getMaze();
                if (maze.endPoint == maze.ponyPosition)
                {
                    Console.WriteLine("Yay! The pony reached the end-point succesfully");
                }
                else
                {
                    Console.WriteLine("Oh no! Our pony has no escape. Let's not make more movements so it can live forever! :)");
                }
                repeat = true;
            }
            Console.ReadLine();
        }

        //Calculates the path from the current pony position to the end-point and returns a string
        //with the positions that the pony must follow.
        public static int[] calculatePath()
        {
            List<int> temp = new List<int>();
            maze = getMaze();
            while (maze.ponyPosition != maze.endPoint)
            {
                int pos = maze.ponyPosition;

                //Try North
                if (maze.positions[pos].north && !maze.positions[pos - 15].hasBeenChecked)
                {
                    maze.positions[pos].hasBeenChecked = true;
                    maze.ponyPosition = pos - 15;
                    temp.Add(maze.ponyPosition);
                }
                //Try West
                else if (maze.positions[pos].west && !maze.positions[pos - 1].hasBeenChecked)
                {
                    maze.positions[pos].hasBeenChecked = true;
                    maze.ponyPosition = pos - 1;
                    temp.Add(maze.ponyPosition);
                }
                //Try East
                else if (maze.positions[pos].east && !maze.positions[pos + 1].hasBeenChecked)
                {
                    maze.positions[pos].hasBeenChecked = true;
                    maze.ponyPosition = pos + 1;
                    temp.Add(maze.ponyPosition);
                }
                //Try South
                else if (maze.positions[pos].south && !maze.positions[pos + 15].hasBeenChecked)
                {
                    maze.positions[pos].hasBeenChecked = true;
                    maze.ponyPosition = pos + 15;
                    temp.Add(maze.ponyPosition);
                }
                else
                {
                    //The way has a close end

                    if (maze.positions[pos].north)
                    {
                        maze.positions[pos - 15].south = false;
                        maze.ponyPosition = pos - 15;
                    }

                    if (maze.positions[pos].west)
                    {
                        maze.positions[pos - 1].east = false;
                        maze.ponyPosition = pos - 1;
                    }

                    if (maze.positions[pos].east)
                    {
                        maze.positions[pos + 1].west = false;
                        maze.ponyPosition = pos + 1;
                    }

                    if (maze.positions[pos].south)
                    {
                        maze.positions[pos + 15].north = false;
                        maze.ponyPosition = pos + 15;
                    }

                    //Those positions that lead to an end close will be removed from the list
                    temp.Remove(pos);

                }

            }
            return temp.ToArray();
        }

        //It is going to read and parse the JSON file and return an object Maze.
        //It is not only going to read the content of the JSON, but also calculate the available
        //coordinates of each position, not only north and west.
        public static Maze getMaze()
        {
            Maze m = maze;

            //Parse 
            json = new WebClient().DownloadString(_mazeUri);
            dynamic d = JObject.Parse(json);
            Position[] positions = new Position[weight * height];

            #region Reading the walls
            //Reading the walls contained in the API
            for (int i = 0; i < (15 * 25); i++)
            {
                positions[i] = new Position();
                positions[i].north = true;
                positions[i].west = true;
                positions[i].east = true;
                positions[i].south = true;
                positions[i].hasBeenChecked = false;
                if (d.data[i].Count > 0 && d.data[i][0] == "west")
                {
                    positions[i].west = false;
                }
                if (d.data[i].Count > 0 && d.data[i][0] == "north")
                {
                    positions[i].north = false;
                }
                if (d.data[i].Count > 1 && d.data[i][1] == "north")
                {
                    positions[i].north = false;
                }
            }
            #endregion

            #region Completing the walls
            //Completing the rest of the walls according to the JSON file
            for (int i = 0; i < (15 * 25); i++)
            {
                //East
                if (i < ((height * weight) - 1))
                {
                    positions[i].east = positions[i + 1].west;
                }

                //South
                if (i < ((height * weight) - 16))
                {
                    positions[i].south = positions[i + 15].north;
                }

                //Last row
                if (i >= (15 * 25) - 15)
                {
                    positions[i].south = false;
                }

                //Last tile
                positions[(height * weight) - 1].east = false;
            }
            #endregion

            m.positions = positions;

            //Getting the pony's position
            m.ponyPosition = d.pony[0];

            //Getting the domokun's position
            m.domokunPosition = d.domokun[0];

            //Getting the EndPoint
            m.endPoint = d["end-point"][0];
            return m;
        }

        //It is going to send the next coordinate as a string to the API via POST
        public static async Task makeMove(int position)
        {
            Console.WriteLine("Pony: " + maze.ponyPosition);
            string direction = getCoordinate(position);

            string str = "{\"direction\":\"" + direction + "\"}";
            _httpClient.DefaultRequestHeaders
             .Accept
             .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using (var content = new StringContent(str, Encoding.UTF8, "application/json"))
            {
                var result = await _httpClient.PostAsync($"{_mazeUri}", content).ConfigureAwait(false);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
                else
                {
                }
            }


        }

        //It is going to analyze a path of directions, to determine whether the next position is safe or not.
        //In case it is not safe, it is going to call recursively this method, with a new path to follow.
        public static void sendDirections(int[] path)
        {
            Console.WriteLine("Sending directions to the pony...");
            foreach (int i in path)
            {
                maze = getMaze();

                //If the pony has died, exit the recursion
                if (ponyDied)
                {
                    return;
                }

                //Check if the Domokun is 1 movement away from the pony
                if (isItSafe(i))
                {
                    makeMove(i).Wait();
                }
                else
                {
                    //In case the domokun is close, we will calculate an escape path, and send the positions to the API.
                    //If the path is safe again (the domokun has made a mistake) we will calculate the path to the endpoint 
                    //and send it recursively to this method.
                    Console.WriteLine("The domokun is close!!");
                    int[] temp = calculateEscape(i);
                    foreach (int x in temp)
                    {

                        int[] endPath = calculatePath();
                        if (isItSafe(endPath[0]))
                        {
                            Console.WriteLine("Path to the end-point calculated in " + temp.Count() + " steps");
                            sendDirections(endPath);
                            break;
                        }
                        else
                        {
                            makeMove(x).Wait();
                        }
                    }
                }
            }

            maze = getMaze();



        }

        //This method it is going to calculate the longest path from the current position out of 10 different paths, 
        //so the pony can walk away the Domokun
        public static int[] calculateEscape(int direction)
        {
            int counter = 0;
            List<int> escapePath = new List<int>();
            while (counter <= 10)
            {
                maze = getMaze();
                maze.positions[direction].hasBeenChecked = true;
                int pos = maze.ponyPosition;
                List<int> temp = new List<int>();

                while (true)
                {
                    Random rnd = new Random();
                    int random = rnd.Next(1, 5);

                    bool north = maze.positions[pos].north && !maze.positions[pos - 15].hasBeenChecked;
                    bool west = maze.positions[pos].west && !maze.positions[pos - 1].hasBeenChecked;
                    bool east = maze.positions[pos].east && !maze.positions[pos + 1].hasBeenChecked;
                    bool south = maze.positions[pos].south && !maze.positions[pos + 15].hasBeenChecked;
                    if (!north && !west && !east && !south)
                    {
                        if (temp.Count > escapePath.Count)
                        {
                            escapePath = temp;
                        }
                        counter++;
                        break;
                    }
                    else
                    {
                        switch (random)
                        {
                            case 1:
                                if (north)
                                {
                                    maze.positions[pos].hasBeenChecked = true;
                                    pos = pos - 15;
                                    temp.Add(pos);
                                }
                                break;
                            case 2:
                                if (west)
                                {
                                    maze.positions[pos].hasBeenChecked = true;
                                    pos = pos - 1;
                                    temp.Add(pos);
                                }
                                break;
                            case 3:
                                if (east)
                                {
                                    maze.positions[pos].hasBeenChecked = true;
                                    pos = pos + 1;
                                    temp.Add(pos);
                                }
                                break;
                            case 4:
                                if (south)
                                {
                                    maze.positions[pos].hasBeenChecked = true;
                                    pos = pos + 15;
                                    temp.Add(pos);
                                }
                                break;
                        }
                    }
                }
            }
            Console.WriteLine("Alternative escaping path calculated in " + escapePath.Count + " steps");
            return escapePath.ToArray();

        }

        //According to the pony and domokun's positions, it is going to calculate whether the next direction is safe or not.
        public static bool isItSafe(int pos)
        {
            maze = getMaze();

            //Possible positions of the Domokun
            int north = maze.domokunPosition - 15;
            int south = maze.domokunPosition + 15;
            int east = maze.domokunPosition + 1;
            int west = maze.domokunPosition - 1;

            if (pos == north || pos == south || pos == east || pos == west ||
                maze.ponyPosition == north || maze.ponyPosition == south ||
                maze.ponyPosition == east || maze.ponyPosition == west)
            {
                return false;

            }
            else
            {
                return true;
            }
        }

        //According to the pony's positions, gets a new position and returns the corresponding coordinate as a string.
        public static string getCoordinate(int position)
        {
            maze = getMaze();
            int north = maze.ponyPosition - 15;
            int south = maze.ponyPosition + 15;
            int east = maze.ponyPosition + 1;
            int west = maze.ponyPosition - 1;
            string direction = "";

            if (position == north) direction = "north";
            if (position == south) direction = "south";
            if (position == east) direction = "east";
            if (position == west) direction = "west";
            if (direction == "")
            {
                ponyDied = true;
            }

            return direction;
        }

    }
}
