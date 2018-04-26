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
    /* 
     * Borja Velez
     * */
    class Program
    {
        static int weight = 15;
        static int height = 25;
        static bool repeat = true;
        static bool ponyDied = false;
        static String json;
        static Maze maze;
        private static readonly HttpClient httpClient = new HttpClient();
        private static string mazeId = "";
        private static string mazeUri = "";

        static void Main(string[] args)
        {
            while (mazeId != "quit")
            {
                while (repeat)
                {
                    repeat = false;
                    Console.WriteLine("Input the maze Id: (Write 'quit' to exit)");
                    mazeId = Console.ReadLine();
                    mazeUri = "https://ponychallenge.trustpilot.com/pony-challenge/maze/" + mazeId;
                    try
                    {
                        maze = new Maze();
                        maze = getMaze();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
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

        //Calculates the path from the current pony position to the end-point and returns an array of 
        //ints with the positions the pony must follow
        public static int[] calculatePath()
        {
            List<int> temp = new List<int>();
            maze = getMaze();
            while (maze.ponyPosition != maze.endPoint)
            {
                int pos = maze.ponyPosition;
                bool flag = true;
                if (maze.positions[pos].coordinates.Count() > 0)
                {

                    foreach (KeyValuePair<string, int> x in maze.positions[pos].coordinates)
                    {
                        if (!maze.positions[x.Value].hasBeenChecked)
                        {
                            maze.positions[pos].hasBeenChecked = true;
                            maze.ponyPosition = x.Value;
                            temp.Add(maze.ponyPosition);
                            flag = false;
                            break;
                        }
                    }

                    if (flag)
                    {
                        //The way has a close end
                        foreach (KeyValuePair<string, int> x in maze.positions[pos].coordinates)
                        {
                            string posToDelete = maze.positions[x.Value].coordinates.FirstOrDefault(i => i.Value == pos).Key;
                            maze.positions[x.Value].coordinates.Remove(posToDelete);
                            maze.ponyPosition = x.Value;
                        }
                        temp.Remove(pos);
                    }
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
            json = new WebClient().DownloadString(mazeUri);
            dynamic d = JObject.Parse(json);
            Position[] positions = new Position[weight * height];

            #region Reading the walls
            //Reading the walls contained in the API
            for (int i = 0; i < (15 * 25); i++)
            {
                positions[i] = new Position();
                positions[i].coordinates = new Dictionary<string, int>();
                positions[i].coordinates.Add("north", i - 15);
                positions[i].coordinates.Add("west", i - 1);
                positions[i].hasBeenChecked = false;

                if (d.data[i].Count > 0 && d.data[i][0] == "west")
                {
                    positions[i].coordinates.Remove("west");
                }
                if (d.data[i].Count > 0 && d.data[i][0] == "north")
                {
                    positions[i].coordinates.Remove("north");
                }
                if (d.data[i].Count > 1 && d.data[i][1] == "north")
                {
                    positions[i].coordinates.Remove("north");
                }
            }
            #endregion

            #region Completing the walls
            //Completing the rest of the walls according to the JSON file
            for (int i = 0; i < (15 * 25); i++)
            {
                //East
                if (i < ((height * weight) - 1) && positions[i + 1].coordinates.ContainsKey("west"))
                {
                    positions[i].coordinates.Add("east", i + 1);
                }

                //South
                if (i < ((height * weight) - 16) && positions[i + 15].coordinates.ContainsKey("north"))
                {
                    positions[i].coordinates.Add("south", i + 15);
                }
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
            httpClient.DefaultRequestHeaders
             .Accept
             .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using (var content = new StringContent(str, Encoding.UTF8, "application/json"))
            {
                var result = await httpClient.PostAsync($"{mazeUri}", content).ConfigureAwait(false);
                if (result.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
                else
                {
                    Console.WriteLine(result.ReasonPhrase);
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

        //This method it is going to calculate the longest path from the current position out of 5 different paths, 
        //so the pony can run away the Domokun
        public static int[] calculateEscape(int direction)
        {
            int counter = 0;
            List<int> escapePath = new List<int>();
            while (counter <= 5)
            {
                maze = getMaze();
                int pos = maze.ponyPosition;
                string posToDelete = maze.positions[pos].coordinates.FirstOrDefault(i => i.Value == direction).Key;

                maze.positions[pos].coordinates.Remove(posToDelete);

                List<int> temp = new List<int>();

                while (true)
                {

                    if (maze.positions[pos].coordinates.Count == 0)
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
                        Random rnd = new Random();
                        int random = rnd.Next(0, maze.positions[pos].coordinates.Count);
                        KeyValuePair<string, int> x = maze.positions[pos].coordinates.ElementAt(random);
                        posToDelete = maze.positions[x.Value].coordinates.FirstOrDefault(i => i.Value == pos).Key;
                        maze.positions[x.Value].coordinates.Remove(posToDelete);
                        pos = x.Value;
                        temp.Add(pos);
                    }
                }
            }
            Console.WriteLine("Alternative escape path calculated in " + escapePath.Count + " steps");
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
            int[] possiblePositions = { north, south, east, west };

            if (possiblePositions.Contains(pos))
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
