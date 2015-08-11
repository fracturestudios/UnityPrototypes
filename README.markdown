
# Unity Prototypes

This repo has some rough prototypes for NullZERO based on Unity.
The idea is a crawl-walk-run progression to get going in Unity and start
implementing more complex features NullZERO needs (like a custom gravity model,
and controlling multiple pawns per player).

I've never used Unity before, so expect sloppy code and anti-patterns :)

Since I expect sloppiness as I learn, I want to design the final game codebase
separately after I've conquered some of the more complex prototypes.

## Prototypes

### MoveToy

The goal of this prototype is to set up the model for how the player controls
the pawn, and how the player pawn interacts with the environment:

* Player controls look direction with the mouse
* Player can walk/strafe the pawn with WASD
* Player can jump to a new surface by using picking with a mouse
* Player realistically lands on surfaces automatically
* Player can use weaponry, and projectiles interact with the environment
* Player can crouch, run and jump (may look goofy with lo-fi graphics :D)

Out of scope:

* Any sort of networking
* Any sort of physically-correct 3D model / animation
* Any actual gameplay logic
* Controlling/switching between multiple pawns

#### Gravity Model

When not jumping between surfaces, the player is always attached to a single
surface, which is a single polygon on a single object. The direction of gravity
for the player is simply the reverse of the normal vector of the player's
attached surface. To account for curvature, this normal is interpolates between
normals at the different vertices of the surface.

When the player walks across a surface, the game switches the player's attached
surface to be whichever polygon the player would intersect with first if the
player were to move in the direction of the current gravity vector. This allows
the player to walk across concave and convex curves realistically, as long as
they're fairly large compared to the size of the player.

With the above strategy, the direction of gravity changes gradually as the
character moves. As the player walks, to compensate with the change of
gravity's direction, the game reorients the player accordingly, so that when
the player moves, the player's orientation relative to gravity remains the same
(even if a curve in gravity prompted a curve in the player's orientation).

To account for disjoint edges/intersecting polygons, we limit how quickly the
automatic reorientation can occur. This way, even on a hard edge, the player's
orientation seems to react smoothly.

When the player jumps between surfaces, the prototype does not correct the
player's orientation. In the final game we may implement things so the model
twists to land on its feet, but we should not automatically change the player's
orientation due to a flight between surfaces.

### DirectorToy

DirectorToy builds on MoveToy to allow the player to manipulate multiple pawns:

* By default, player has direct control of the pawn (a la MoveToy)
* Player can bring up a Director HUD with RTS-style commands
* From the Director, player can switch between pawns
* Player can issue 'move to a target' commands
* Pawns respond by automatically finding paths to their targets
* Player can issue 'attack target' commands
* Pawns respond by pursuing the target and attacking when in range
* Player can issue 'hold position' commands
* Pawns respond by staying in a single spot and attacking enemies in range
* Player can queue multiple commands
* Player can cancel commands
* Player can quickly inspect the command queue in the Director
* Idle pawns (no focus, no queued commands) automatically attack/evade

Out of scope:

* Any sort of networking
* Any sort of physically-correct 3D model / animation
* Any actual gameplay logic

For this toy we'd need a few AI-related primitives like

* Pathfinding (if I'm pursuing an enemy, how do I move toward it?)
* Visibility determination (can I see the enemy?)
* Position projection (if I lose sight of the target, where do I think it is?
  what direction should I fire to not miss the target?)

This is a good prototype to add more advanced HUD features, in addition to the
RTS/command-specific features

* Alerting the player to nearby allies / enemies
* Color-coded bezier curves to visualize the command queue

One thing that might be useful for gameplay in the future is to implement a
sort of 'global' view of the game, where we visualize the map in a 3D wireframe
with markers for player pawns and visible enemies. Users would be able to
rotate/zoom in the map to issue commands. A fog of war would visualize areas in
the map which are visible, have been explored, and have never been explored.

### NetworkToy

NetworkToy reimplements DirectorToy from the ground up using a
network-compatible client/server model:

* The server implements gameplay
* The server produces state snapshots and sends them to the clients
* Clients interpolate server snapshots to replay server state
* Clients send input events to server
* Server incorporates client input events into the simulation
* Clients predict player pawn positions using logic from the server
* Clients interpolate between predicted and authoritative state
* Players can host and connect to games locally
* Players can host and connect to games online using a broker server

Unity helps us with a bunch of this with its NetworkManager class, but notably
it doesn't have any built-in support for client-side prediction, which is a
must have for a twich-based networked multiplayer game. The hope is to be able
to extend NetworkManager to suit our needs, but we may need to reimplement
swaths of it.

Since the focus of this prototype is debugging network code, we actually won't
be interested in integrating any real gameplay, a la MoveToy or DirectorToy.
Instead, we'll test using a very simple simulation (probably 2D, movement based
directly on input, no physics, no collisions, no obstacles).

### MultiplayerToy

MultiplayerToy reimplements DirectorToy using the networked multiplayer
client-server model implemented in NetworkToy.

This prototype will be a placeholder until we know more about networking
specifics. We'll design a networking model that supports gameplay specifics
(and, in hopefully rare cases, change gameplay specifics to ease up
networking).

### KinematicsToy

This is a placeholder toy for the future. The goal is to integrate an IK-rigged
player pawn model into the game, and integrate some prefabbed animations with
physics / IK data.

### GraphicsToy

This is a placeholder toy for the future, when we start doing fancypants
graphics shit.

