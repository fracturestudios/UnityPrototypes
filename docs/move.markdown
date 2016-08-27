
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

### Building

### Intersecting Meshes

### Supported Query Operations

### Data Structure

## Scene-Level Spatial Subdivision

## Unity Entity/Component Design

## Debug Harness

## Implementation Plan


