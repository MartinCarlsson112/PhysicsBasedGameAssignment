# PhysicsBasedGameAssignment


This is my PhysicsBasedGameAssignment.
The objective of the game is to solve physics "puzzles".
There might be performance issues, I can provide a video representation if necessary.

Controls: 
WASD for movement.
Left mouse button to shoot a sphere.


Clear the puzzles by pushing off the red cubes from the platforms/out of the boxes.

The game was created in Unity 2020.2, but has been converted back to 2019.3.
The game uses custom made collision using the GJK algorithm and generates contacts with the EPA algorithm.
The game uses a physics simulation through a dynamics system which has 4 steps. (This can be found in DynamicsSystem.cs)

1 Apply gravity.

2. Collision Response using impulses. 
Each contact generates an impulse pushing the object away from the other object.
I attempted to also implement angular velocity, however I did not succeed.

3. Position Correction by calculating the minimum overlap solving vector.

4. Velocity Integration.
