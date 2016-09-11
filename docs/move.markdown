
# MoveToy

This doc describes how we plan to implement player navigation in the move toy.

## Navigation

Navigation in nullZERO is a little atypical, since there's no gravity to orient the map around.

Navigation is a state machine with two distinct modes, based on whether or not the player is attached to a particular surface.
The player controls when to transition between these two states and how.

### Walking

When the player is "walking," they are attached to a surface, and move over the surface.
Players in a "walking" state always stay attached to the surface, and when they walk off an edge, they continue onto the adjacent face along that edge.
Players never "fall off" an object while in the walking state.

This is done using an anchor point projected onto a face of the navigation surface.
When the player moves, we internally move the anchor point along the face, rolling over to adjacent faces as needed.
The movement direction is calculated by projecting the player's look vector onto the edge which houses the anchor point.

When the player walks over an edge, the anchor point is moved to the equivalent point on the adjacent face.
We subtract the distance the player already moved to get to the edge, and process the remaining distance along the new face.
This happens as many times as necessary until the player has moved the complete distance.

After moving the player, we compute the normal from the surface at the anchor point, and use that to orient the player.
This surface normal is computed by interpolating the face's vertex normals.
As a result, if the player walks across a curve surface, the player orientation matches the curve.

When the player walks across a sharp edge (i.e. the normal at the same edge is different for the two adjacent faces, like the edge of a cube), the player's orientation smoothly interpolates around the edge until the player is oriented with the edge on the next face.
This means the player rounds corners smoothly.

Note the pawn's look direction is relative to the pawn's orientation, as if the look direction were the head and the pawn's orientation were the body.
When the player rounds a corner, the look direction rounds the corner too, but the look direction is the same relative to the player pawn.

### Jumping

While a player is walking, they can press the jump button to propel themselves off the surface in the direction they were looking.
In the transitionary state, the player accelerates away from the anchor point at a rate inversely proportional to the distance from the anchor point.
(As the player gets farther from the anchor point, the rate of acceleration falls off; if the player started far from the anchor point, they don't accelerate much.)

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
This behavior allows advanced players to use nearby objects to catapult themselves forward.

### Dynamic Bodies

The discussion above assumes all surfaces the player attaches to / accelerates off are static / immovable.
We can also support dynamic bodies, which have small enough mass to be moved by the player when jumping / landing.
This could be used in gameplay to allow the player to interact with smaller objects / debris.

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

Each navigation mesh also has an axis-aligned bounding box in its own object space.
To create its world-space axis-aligned bounding box, the navigation mesh transforms its object space AABB into world space and uses the resulting vertices as the bounds of its world-aligned AABB.
This means the world-space AABB may not be as tight as the object-space AABB, but computing the final AABB is computationally very cheap.

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

## Unity Integration

### `Navigation` and `NavigationMesh`

Each scene has a single `Naviagation` component which owns navigation for the scene.
Components which want to read navigation information find this component and query it.
GameObjects which are navigable are created with a `Navigable` component, which submits the object's mesh geometry to `Navigation` to be added to the scene.

The `Navigation` component has a collection of `NavigationMesh` objects, each of which is a separate mesh in the scene.
At scene load time, each `Navigable` component submits its mesh geometry to `Navigation`.
`Navigation` generates a `NavigationMesh` from that geometry, and adds it the scene.
It checks whether the new `NavigationMesh` intersects any other meshes and, if it does, combines the meshes together.

Every scene update, `Navigation` builds an octree containing each `NavigationMesh` in the scene, using the mesh's axis-aligned bounding box.
This octree is used to accelerate queries to find certain meshes (e.g. the set of meshes within a certain distance from the player).

Components that need navigation information, like `PlayerNavigation` query the `Navigation` object to find individual `NavigationMesh` objects.
The object can cache the `NavigationMesh` by reference and use it on subsequent game frame updates.

### `Navigable`

Navigable is a simple component used to submit a mesh to the `Navigation` component for processing at scene load time.
It can be added to rendered and non-rendered meshes.
For high-poly meshes, it's generally preferable to create a separate lower-res navigation mesh for that component, add `Navigable`, and then remove rendering from the mesh.
That low-poly mesh contains the low-poly navigation surface for the high-poly display mesh.

### `PlayerNavigation`

Processes user input and moves the player accordingly, per the first section in this document.
Queries the `Navigation` component to discover scene navigation geometry.

## Implementation Plan

### Design changes for merging meshes

In order to support dynamic meshes, we're going to need to have some `Transform` in the scene which corresponds with the `NavigationMesh`.
This is a problem with the current plan if we want the dynamic body to also support complex geometry that was merged from multiple primitive meshes.

I think the solution here is to only merge meshes under a single subtree of the NavigationMesh component.
That is, you have to parent/prefab all the sub-meshes to a single GameObject with a single `Navigable` component.
Thus we only run the merge logic per `Navigable`.

What's interesting is, if we do that, we can have `NetworkMesh` be a component, and don't have need for a `Navigable`.
On the other hand, I still like that our current design gives us the flexibility to do one whole-scene spatial acceleration DS if we wanted to.

### Basic Navigation Meshes

* `NavigationMesh` object with stubbed query APIs
* `Navigation` mesh with registration APIs
* `Navigable` component that gathers all meshes in its subtree and submits them to the `Navigation` component
* Set up a test scene with a single flat box that was made into a navigation mesh
* Implement the navigation component / navigation mesh types
* Visual debugging to manually verify the different types of queries are working

### Walking

* Compute the player anchor point, visual debugging to verify this works
* Anchor point movement on the box, debugging to verify this works
* Anchor point movement around the box, debugging to verify this works
* Normal vector calculation based on face normal
* Visual debugging to make sure the normal vector is working
* Smooth normal vector by sweeping out multiple points and averaging the normals

### Navigation Mesh Merging

* Test scene with two boxes making a convex "V" shape, player is inside the V
* Implement mesh merging for a single mesh being submitted by a `Navigable` component
* Verify the anchor point walks across the V and automatically gets on the right side of the mesh

### Jumping

* Simple test scene consising of a giant box made from navigable surfaces
* State machine in `PlayerNavigation`
* Implementation for jumping-off state
* Implementation for jumping / in-flight state
* Implementation for landing state

### Spatial Acceleration

* Make a very large test scene with a ton of randomly-generated navigable geometry
* K-D tree per mesh
* Octree per scene
* Verify we're significantly improving performance

### Dynamic Bodies

* Test scene with a single dynamic object
* Wire up `Navigation` to track the `Transform` of any dynamic `NavigationMesh`

### Movement HUD

* Spec
* Prototype

### Full Demo

Unifieid demo scene that includes everything we've done so far

* Large enclosed space with an irregular shape
* Generate static geometry in a regular pattern, where all static bodies intersect with the walls
* Generate dynamic geometry randomly, in the middle of the room

