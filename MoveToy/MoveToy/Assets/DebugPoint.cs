using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPoint : MonoBehaviour
{
    bool m_spawned;
    NavigationMesh m_mesh;
    int m_currentFace;
    Quaternion m_moveDir;

    void Start()
    {
        m_spawned = false;
        m_mesh = null;
        m_currentFace = -1;
        m_moveDir = Quaternion.identity;
    }

    private static string VecStr(Vector3 v)
    {
        return string.Format("({0:f3}, {1:f3}, {2:f3})", v.x, v.y, v.z);
    }

    void Spawn()
    {
        // Get the test scene's navigation mesh
        UnityEngine.Object[] obj = UnityEngine.Object.FindObjectsOfType(typeof(Navigation));
        if (obj.Length != 1)
        {
            throw new Exception("Zero or multiple Navigation components in scene");
        }

        Navigation nav = (Navigation)obj[0];

        List<NavigationMesh> meshes = new List<NavigationMesh>(nav.Meshes);
        m_mesh = meshes[0];

        // Spawn the player anchor point at the top of the navigation mesh
        Vector3 worldPos = Vector3.up * 100f + Vector3.left * 20f;
        Vector3 objectPos = m_mesh.Transform.worldToLocalMatrix.MultiplyPoint3x4(worldPos);

        Vector3 objectSpawn;
        m_mesh.NearestPoint(objectPos, out m_currentFace, out objectSpawn);

        Vector3 worldSpawn = m_mesh.Transform.localToWorldMatrix.MultiplyPoint3x4(objectSpawn);
        transform.position = worldSpawn;

        // Rotate the player reference frame to match the current normal
        Vector3 objectNormal = m_mesh.Faces[m_currentFace].NormalAt(objectSpawn, m_mesh);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, objectNormal);
        transform.rotation = rotation * transform.rotation;

        // Orient the player WASD directions around the face normal
        Vector3 worldNormal = m_mesh.Transform.localToWorldMatrix.inverse.transpose.MultiplyVector(m_mesh.Faces[m_currentFace].FaceNormal);
        m_moveDir = Quaternion.FromToRotation(Vector3.up, worldNormal);
    }

    // Finds the point at which the ray starting at position p and moving in
    // direction d intersects the line segment with endpoints a and b. Assumes
    // d and (a - b) are in the same plane, +/- some epsilon distance due to
    // floating point inaccuracy.
    //
    // The return value indicates whether an intersection was found. If the
    // return value is true, the intersection out-param indicates the point at
    // which the ray intersects the given line segment. If the return value is
    // false, the out-param should not be used.
    //
    private static bool RayLineIntersect(Vector3 p, // Ray origin
                                         Vector3 d, // Ray direction
                                         Vector3 a, // Vertex on edge
                                         Vector3 b, // Vertex on edge
                                         Vector3 c, // Third vertex of triangle
                                         out Vector3 intersection)
    {
        // To do this intersection, we'll first construct a plane containing 
        // points a and b, perpendicular to the plane containing points a, b
        // and p. Once we've done that, the intersection point is just the
        // point at which the ray intersects with the plane.

        float epsilon = 1e-4f;
        float epsilon2 = epsilon * epsilon;
        
        // The plane normal is the component of pa perpendicular to ba
        Vector3 pa = p - a;
        Vector3 ba = b - a;
        Vector3 n = (pa - (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba) * ba)).normalized;

        // Find an intersection with the plane containing point a with normal n
        float t = 0;
        if (n.sqrMagnitude < epsilon2)
        {
            // There is no component of pa perpendicular to ba, so pa is
            // parallel to the edge. That implies the point is already on the
            // edge.
            //
            t = 0f;

            /*
            // TODO DEBUG there's a degenerate case here when the point is on
            // an edge AND the movement direction is along that edge. In that
            // case, the first intersection point is (correctly) at t=0, but
            // that causes us to jump between faces endlessly.
            //
            Vector3 v1 = ba.normalized;
            Vector3 v2 = d.normalized;
            if (Mathf.Abs(Mathf.Abs(Vector3.Dot(v1, v2)) - 1f) < epsilon)
            {
                // We're on the edge, and the movement direction is along the
                // edge, so just move to the endpoint we're traveling toward
                //
                if (Vector3.Dot(v1, v2) > 0f)
                {
                    Debug.Log("Moving along edge toward B");
                    intersection = b;
                    return true;
                }
                else
                {
                    Debug.Log("Moving along edge toward A");
                    intersection = a;
                    return true;
                }
            }
            */
        }
        else
        {
            t = -1f * Vector3.Dot(p - a, n) / Vector3.Dot(d, n);
        }

        if (t < 0f && -t < epsilon)
        {
            t = 0f;
        }

        if (t < 0f)
        {
            intersection = Vector3.zero;
            return false;
        }

        if (float.IsInfinity(t))
        {
            intersection = Vector3.zero;
            return false;
        }

        // Due to limited floating point precision, the ray may actually be
        // slightly outside the triangle pointing in. If this edge is being
        // intersected because the ray is entering the triangle, it's spurious
        // and can be ignored.
        //
        Vector3 ca = c - a;
        if (Vector3.Dot(Vector3.Cross(ba, ca), Vector3.Cross(ba, d)) > 0)
        {
            intersection = Vector3.zero;
            return false;
        }

        // Now we've found an intersection in the plane which contains a and b
        // and is perpendicular to the plane of the triangle. Due to limited
        // floating point precision, the intersection point might not actually
        // lie on the edge, so project the plane intersection point onto the
        // line of the edge
        //
        Vector3 planeIntersect = p + t * d;

        Vector3 v = planeIntersect - a;

        Vector3 edgeIntersect = a + Vector3.Dot(v, ba) / Vector3.Dot(ba, ba) * ba;

        // If this intersection point is between a and b, then the ray
        // intersects this triangle edge.
        //
        float ba2 = ba.sqrMagnitude;
        if ((edgeIntersect - a).sqrMagnitude - epsilon2 <= ba2 &&
            (edgeIntersect - b).sqrMagnitude - epsilon2 <= ba2)
        {
            intersection = edgeIntersect;
            return true;
        }
        else
        {
            intersection = Vector3.zero;
            return false;
        }
    }

	void Update()
    {
        Transform transform = GetComponent<Transform>();

        // TODO temporary hack to get around the fact that DebugPoint gets
        // initialized before the terrain object, which means during Start()
        // there are no meshes in the Navigation struct.
        //
        // There has to be a better way to do this :-)
        //
        if (!m_spawned)
        {
            Spawn();
            m_spawned = true;
        }

        // Based on input, determine what direction to move
        Vector3 playerDir = Vector3.zero;

		if (Input.GetButton("AnchorForward")) {
			playerDir += Vector3.forward;
		}

		if (Input.GetButton("AnchorBackward")) {
			playerDir -= Vector3.forward;
		}

		if (Input.GetButton("AnchorLeft")) {
			playerDir -= Vector3.right;
		}

		if (Input.GetButton("AnchorRight")) {
			playerDir += Vector3.right;
		}

        if (playerDir == Vector3.zero)
        {
            return;
        }

        // Move along the navigation mesh
        const float speed = 12f;
        float worldDistance = speed * Time.deltaTime;

        int iters = 0;
        while (Mathf.Abs(worldDistance) > Mathf.Epsilon)
        {
            Vector3 worldDir = m_moveDir * playerDir.normalized;

            Vector3 objectPos = m_mesh.Transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);
            Vector3 objectDelta = m_mesh.Transform.worldToLocalMatrix.MultiplyVector(worldDistance * worldDir);
            Vector3 objectDir = objectDelta.normalized;

            // Project the object space position and direction onto the plane of the
            // current face. This is not mathematically necessary, but helps with
            // limited floating point precision
            //
            NavigationMesh.Face f = m_mesh.Faces[m_currentFace];
            Vector3 a = m_mesh.Vertices[f.A];
            Vector3 b = m_mesh.Vertices[f.B];
            Vector3 c = m_mesh.Vertices[f.C];

            objectPos = objectPos - Vector3.Dot(objectPos - a, f.FaceNormal) * f.FaceNormal;
            objectDelta = objectDelta - Vector3.Dot(objectDelta, f.FaceNormal) * f.FaceNormal;
            objectDir = objectDelta.normalized;

            // Figure out which edge we'll hit if we keep travelling this direction
            Vector3 edgeIntersect = Vector3.zero;
            int neighbor = -1;

            if (RayLineIntersect(objectPos, objectDir, a, b, c, out edgeIntersect))
            {
                neighbor = f.AdjacentAB;
            }
            else if (RayLineIntersect(objectPos, objectDir, b, c, a, out edgeIntersect))
            {
                neighbor = f.AdjacentBC;
            }
            else if (RayLineIntersect(objectPos, objectDir, c, a, b, out edgeIntersect))
            {
                neighbor = f.AdjacentCA;
            }
            else
            {
                Debug.Log("Movement ray does not intersect any edge of current face");
                Debug.Log(string.Format("Position: {0} Direction: {1}", objectPos, objectDir));
                Debug.Log(string.Format("Face: {0} {1} {2}", a, b, c));
                break;
            }

            // If we didn't hit an edge, move the full distance and be done
            if (objectDelta.sqrMagnitude < (edgeIntersect - objectPos).sqrMagnitude)
            {
                transform.position = m_mesh.Transform.localToWorldMatrix.MultiplyPoint3x4(objectPos + objectDelta);
                worldDistance = 0f;

                break;
            }

            // We hit an edge of the triangle; continue onto the adjacent face
            m_currentFace = neighbor;
            NavigationMesh.Face adjacent = m_mesh.Faces[m_currentFace];

            // Move the anchor point to the edge between the triangles
            transform.position = m_mesh.Transform.localToWorldMatrix.MultiplyPoint3x4(edgeIntersect);

            // Decrement distance traveled from the total distance left to travel
            Vector3 worldDelta = m_mesh.Transform.localToWorldMatrix.MultiplyVector(edgeIntersect - objectPos);
            worldDistance -= worldDelta.magnitude;

            // Rotate the player reference frame around the new face normal
            Vector3 worldNormalBefore = m_mesh.Transform.localToWorldMatrix.inverse.transpose.MultiplyVector(f.FaceNormal);
            Vector3 worldNormalAfter = m_mesh.Transform.localToWorldMatrix.inverse.transpose.MultiplyVector(adjacent.FaceNormal);
            Quaternion rotation = Quaternion.FromToRotation(worldNormalBefore, worldNormalAfter);
            m_moveDir = rotation * m_moveDir;

            // Rotate the player to match the point normal on the new face
            transform.rotation = m_moveDir; // DEBUG

            if (++iters > 100)
            {
                // FUTURE: There's a degenerate case in this movement algorithm
                // when the anchor point is along an edge, and the movement
                // direction is along the same edge. When that happens,
                // RayLineIntersect (correctly) says the ray intersects the
                // edge at zero, and this method switches over to the next
                // face without moving. Unfortunately, on the next iteration,
                // we do the exact same thing, and end up right back on the
                // original face, ad nauseum.
                //
                // You can make this occur by creating a scene with a test
                // sphere, spawning the player right at the top of the sphere,
                // and locking the look directions to the XZ axis. Then the
                // player will be on a vertex at the top of the sphere, and
                // both movement axes will be aligned with the edges at the top
                // of the sphere.
                //
                // This edge case will be nearly impossible in actual gameplay,
                // since the player will be able to swing the look vector
                // around and has a vanishingly low probability of ever
                // perfectly aligning the look vector with an edge. However, if
                // we start running into this in the real world, we'll have to
                // do something about it.
                //
                // In the meantime, in the sphere test scene, you can rotate
                // the sphere slightly so its edges no longer align with the
                // look vector.
                //
                throw new Exception("Possible infinite loop?");
            }
        }
	}
}

