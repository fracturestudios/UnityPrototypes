
# MoveToy

This doc describes how we plan to implement player navigation in the move toy.

## Navigation

Navigation is a state machine with two distinct modes, based on whether or not the player is attached to a particular surface.
The player controls when to transition between these two states and how.

### Walking

When the player is "walking," they are attached to a surface, and move over the surface.
Players in a "walking" state always stay attached to the surface, and when they walk off an edge, they walk onto the adjacent edge.
Players never "fall off" an object while in the walking state.

This is done using an anchor point projected onto the navigation surface.
When the player moves, we internally move the anchor point along the surface, rolling over to the adjacent edge as needed.
The movement direction is calculated by projecting the player's look vector onto the edge which houses the anchor point.

When moving his way moves the player over an edge, the anchor point is moved to equivalent point on the adjacent edge.
We subtract the distance the player already moved to get to the edge, and process the remaining distance on the new edge.
This happens as many times as necessary until the player has moved the complete distance required.

After moving the player, we compute the normal from the surface at the anchor point, and use that to position and orient the player.

To smooth the player orientation when walking over a sharp edge, we sweep out multiple points around the anchor point and compute all those normals.
We then average together all those normals to produce a final normal for the player pawn.
As the player nears the edge, we thus smoothly interpolate the pawn's orientation around the edge.

Note the pawn's look direction is relative to the pawn's orientation, as if the look direction were the head and the pawn's orientation were the body.

### Jumping

While a player is walking, they can press the jump button to propel themselves off the surface in the direction they were looking.
In the transitionary state, the player accelerates away from the anchor point at a rate inversely proportional to the distance from the anchor point.
(As the player gets farther from the anchor point, the rate of acceleration falls off; if the player started far from the anchor point, they don't accelerate a ton.)

After the player is a fixed difference away from the anchor point, the player is no longer accelerating and is in the "jumping" state.
If the player releases the jump button, the player stops acclerating immediately and enters the "jumping" state.

If the player changes their look direction while accelerating, the acceleration direction changes for the rest of the acceleration, but doesn't change their previous acceleration.
In other words, during every frame while the player accelerates, the acceleration direction is the look direction, even if the look direction changes.
Changing directions while jumping does not introduce rotational velocity.

The rate of acceleration when jumping off is proportional to the dot product of the look direction and the anchor point normal.
If these two vectors are perpendicular or opposing when the player presses the jump button, the pawn detaches from the anchor point, but does not accelerate away.

While in the jumping state, the player pawn just moves via momentum.
If the pawn hits something, the pawn bounces off the object and continues along its new course.
Based on the move direction and the angle at which the player bounces off the object, bouncing may introduce rotational velocity.
In real gameplay, this could also damage the player proportionally to their velocity at the time of impact.

When the player presses the jump button again, the pawn tries to attract to the point where the player is looking.
The player pawn accelerates towards the anchor point and reorients its up vector to match the anchor point normal, at a rate inversely proportional to the distance to the anchor point.
Because the landing point is guided by looking, the look vector changes inversely with the orientation change, so that the look vector does not change as the player's orientation does.

If the player lets go of the jump button before the pawn hits the anchor point, the pawn goes back to the jumping state.
Otherwise, if the pawn hits the anchor point while the user is holding the jump button, the pawn enters a walking state.

### Dynamic Bodies

The discussion above assumes all surfaces the player attaches to / accelerates off are static / immovable.
We can also support dynamic bodies, which have small enough mass to be moved by the player when jumping / landing.
From a gameplay perspective, this is like landing on / jumping off debris in zero-g.

When the player is attaching to a dynamic body, we compute the anchor point the player is attaching to, and pull the body toward the player from the anchor point.
We compute the body's linear and rotational acceleration from the anchor point and the center of mass, using some basic Newtonian mechanics.
Note that since the player guides the anchor point by looking, the anchor point can change as the mass turns.

At the point when the player lands on the body, we stop pulling on the body.
At that point, the linear and rotational inertia of the body take over.
Newtonian dynamics on the body are then computed based on the combination of the body and the player as a single unit.

As the player walks on a dynamic body, the center of mass changes as the player moves.
The player moving on a dynamic body does not otherwise exert force on the body.

When the player jumps off a dynamic body, the body undergoes similar dynamics to when the player lands, except the anchor point repels the body from the pawn instead of attracting.
The force by which the player repels the anchor point is one again inversely proportional the distance between the pawn and the anchor point.
Unlike landing, during jumping the anchor point does not change as the player jumps.

## Navigation Mesh

Each object in the scene that the player can attach to is described by a navigation mesh.
A navigation mesh caches geometry information about the object which is used to efficiently compute anchor points and their movement across the surface.

### Supported Query Operations

* Given an arbitrary point, find the nearest face
* Given an arbitrary point and a face, find the nearest point on that face
* Given an origin point and a direction, find the first face that ray intersects, as well as the intersection point
* Given an origin point on a face and a direction along the face, find the edge that direction vector intersects.
* Given a face and an edge, find the adjacent face on that edge

### Data Structure

The navigation mesh has three lists:

* `Vertices` - A list of vertex positions containing 3D coordinates in the mesh's object space
* `Edges` - A list of edges, where each edge is a pair of indicates of the two vertices in the `Vertices` list. 
  No constraints are made on the ordering of edges.
* `Faces` - A list of triangles in the mesh.

The faces list is the one clients call into most often.
Each face contains

* Three vertices which define the face, specified in the winding order.
  These are stored as indices into the `Vertices` array

* A normal vector, cached for efficiency purposes.
  This could be generated on-the-fly using the face's vertices.

* Three edges which define a boundary of the triangle, each stored as an index into the `Edges` array

* For each edge, a reference to the neighboring face that shares that edge.
  Stored as an index into the `Faces` array.

A valid navigation mesh has exactly two adjacent faces per edge.

Each navigation mesh also has a k-d tree which spatially subdivides the mesh's faces, in mesh space.
When trying to project a point onto the mesh, or trace a ray through the mesh, this k-d tree is used to efficiently reject unwanted faces.

### Usage

Player pawns using a navigation mesh typically cache

* Which mesh they're anchored to
* The index of the face in the mesh they're anchored to
* The anchor point, in barycentric coordinates relative to the anchor face
* A cached copy of the anchor point in world space.

During movement along the mesh, player pawns make queries relative to the anchor point, and then update the anchor point accordingly.

### Building

The level designer controls the navigation meshes for a map.
They can either manually build navigation meshes from primitives or generate them from visible geometry in the level.
In the latter case, the level designer can interactively decimate the geometry to produce a simpler mesh.

When generating a mesh from geometry, the generation process eliminates vertices within an epsilon distance from each other and shares them in the final mesh.
This is used to reliably determine which faces are "adjacent": just pick faces which share two connected verices, and those vertices are the joining edge.
Care is taken to make sure the resulting mesh is a valid navigation mesh (i.e. every edge has two adjoining faces).

When the scene is saved in the Unity editor, the editor extension for navigation meshes checks for overlapping mesh geometry and combines them.
It does this by checking the faces for each mesh for collisions; where a collision is found, a new edge is created, and extra geometry from each of the colliding faces is removed along the edge.
Which section to cut off is determined by the normal vectors for the two faces, assuming all faces are one-sided.

After this, the Unity editor generates a k-d tree for the final mesh, to allow efficient queries at runtime.

All of the above data is loaded from the scene during the load process.

## Scene-Level Spatial Subdivision

Navigation meshes can get large and complex for real game maps/levels.
As already discussed above, we compute a fine-grained spatial subdivision data structure up front to speed up queries against a particular mesh.
However, we do not combine these meshes into a single scene-level spatial subdivision data structure.
This could be done if all objects in the scene were static, but for dyanmic bodies, it would be costly to try and update or regenerate this data structure on-the-fly.

Instead, we cache the spatial subdivision structure per navigation mesh, since each individual mesh is itself static.
For each mesh, we also compute and cache an axis-aligned bounding box.
Dynamic meshes update their AABB when they move.

Then, once per frame, these bounding boxes are placed into a scene-level octree.
During the frame, any time a character needs to query the nearest navigation mesh(es), the octree is used to fast-reject meshes that are too far away to consider.
The query then runs on each navigation mesh that wasn't rejected to find the correct answer to the query.

This octree is built one per frame, regardless of whether it will be needed or not; we not wait to generate it on-demand.
That's because we want the CPU budget for building this octree to be accounted for in every frame.
Frames which need this octree should not glitch / lag / skip more often than frames which do not need an octree.
If our octree implementation is slow, it should drop our framerate uniformly, so that we can tweak it to run more quickly uniformly.

## Unity Entity/Component Design

## Debug Harness

## Implementation Plan


