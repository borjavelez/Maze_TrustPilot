# Maze_TrustPilot

The proposed maze has only 1 possible path from the position of the pony to the end-point.
Firstly we must parse the JSON file and to create the different positions with the information received.
Each position will have coordinates (north, south, east and west) as booleans, being true those who are accesible (no walls).

In this solution, the program is going to calculate the path, before making any movements, using a trial and error algorithm.
Starting from the current position of the pony, the program is going to move until it reaches one point with no exit, then that position is going
to be marked as inaccesible by changing the coordinates information of the neighbor positions.
By doing that, all the ways that lead to a close end will be innacessible by our pony.
Once the path has been calculated the movements will be sent to the provided API, and the real pony is going to follow the given directions.

The Domokun is going to try to catch our pony, and depending on the difficulty is going to fail more or less. Since there is only one path 
to get to the end-point, if the Domokun gets between the pony and the end-point, it is going to be almost impossible to reach it.
To solve that, in case the Domokun gets into our path, the pony is going to calculate 5 random paths from its exact position and it is 
going to choose the longest one as an "escape path". The pony it is going to run through that path with the Domokun chasing it.
If the Domokun makes a mistake and leaves the end-point path, the Pony is going to stop and try to reach the end-point again.
To reach the end-point again, the Domokun must make 2 mistakes in a row, which is very unlikely.

**If the Domokun positions himself between the pony and the path, the pony is going to run away and it is going to go
back everytime the Domokun makes a mistake, to be ready in case a second mistake occurs. This means that the maze
may take a time to complete, because the pony will go back one position everytime the Domokun makes a mistake.**
