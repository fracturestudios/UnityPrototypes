using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPoint : MonoBehaviour
{
    bool m_spawned;
    NavigationMesh m_mesh;
    int m_currentFace;

    void Start()
    {
        m_spawned = false;
        m_mesh = null;
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
        Vector3 worldPos = Vector3.up * 10f + Vector3.left * 2f;
        Vector3 objectPos = m_mesh.Transform.worldToLocalMatrix.MultiplyPoint3x4(worldPos);

        Vector3 objectSpawn;
        m_mesh.NearestPoint(objectPos, out m_currentFace, out objectSpawn);

        Vector3 worldSpawn = m_mesh.Transform.localToWorldMatrix.MultiplyPoint3x4(objectSpawn);
        transform.position = worldSpawn;
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

        float epsilon = 1e-6f;
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
        }
        else
        {
            t = -1f * Vector3.Dot(p - a, n) / Vector3.Dot(d, n);
        }

        if (t < 0f && -t < epsilon)
        {
            t = 0f;
        }

        if (t < 0f || float.IsInfinity(t))
        {
            intersection = Vector3.zero;
            return false;
        }

        // Things get interesting when p is exactly on the edge ab. 
        //
        // - If we're on the edge and trying to enter the triangle, this edge
        //   intersection is spurious, so we ignore it
        //
        // - If we're trying to leave the triangle, however, then we should be
        //   continuing to the next triangle, in which case we process this
        //   edge intersection as t = 0
        //
        if (t < epsilon)
        {
            Vector3 ca = c - a;
            if (Vector3.Dot(Vector3.Cross(ba, ca), Vector3.Cross(ba, d)) < 0)
            {
                // Leaving the edge
                intersection = p + t * d;
                return true;
            }
            else
            {
                // Entering the edge
                intersection = Vector3.zero;
                return false;
            }
        }

        // Since we assumed p and d are both in the plane contianing a and b,
        // the intersection point with the perpendicular plane is on the lane
        // of a and b.
        //
        // If this intersection point is between a and b, then the ray
        // intersects this edge.
        //
        Vector3 result = p + t * d;

        float ba2 = ba.sqrMagnitude;
        if ((result - a).sqrMagnitude - epsilon2 <= ba2 &&
            (result - b).sqrMagnitude - epsilon2 <= ba2)
        {
            intersection = result;
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

        // Based on input, determine how far to move
        const float speed = 12f;
        float dt = Time.deltaTime;

		Vector3 forward = transform.rotation * Vector3.forward;
		Vector3 right = transform.rotation * Vector3.right;

        Vector3 worldDelta = Vector3.zero;

		if (Input.GetButton("AnchorForward")) {
			worldDelta += speed * dt * forward;
		}

		if (Input.GetButton("AnchorBackward")) {
			worldDelta -= speed * dt * forward;
		}

		if (Input.GetButton("AnchorLeft")) {
			worldDelta -= speed * dt * right;
		}

		if (Input.GetButton("AnchorRight")) {
			worldDelta += speed * dt * right;
		}

        while (worldDelta.sqrMagnitude > Mathf.Epsilon)
        {
            Vector3 objectPos = m_mesh.Transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);
            Vector3 objectDelta = m_mesh.Transform.worldToLocalMatrix.MultiplyVector(worldDelta);
            Vector3 objectDir = objectDelta.normalized;

            // Figure out which edge we'll hit if we keep travelling this direction
            NavigationMesh.Face f = m_mesh.Faces[m_currentFace];
            Vector3 a = m_mesh.Vertices[f.A];
            Vector3 b = m_mesh.Vertices[f.B];
            Vector3 c = m_mesh.Vertices[f.C];

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
                worldDelta = Vector3.zero;

                break;
            }

            // We hit the edge of the triangle
            //
            // - Move the player to the edge
            // - Rotate the player around the edge to orient with the next
            //   face's normal
            // - Subtract the travelled distance from the total distance
            //   left to travel
            // - Continue on to the adjacent face
            //
            m_currentFace = neighbor;
            NavigationMesh.Face adjacent = m_mesh.Faces[m_currentFace];

            Vector3 objectMovement = edgeIntersect - objectPos;
            worldDelta = worldDelta - m_mesh.Transform.localToWorldMatrix.MultiplyVector(objectMovement);

            objectPos = edgeIntersect;
            transform.position = m_mesh.Transform.localToWorldMatrix.MultiplyPoint3x4(objectPos);

            Quaternion rotation = Quaternion.FromToRotation(f.Normal, adjacent.Normal);
            transform.rotation = rotation * transform.rotation;
            worldDelta = rotation * worldDelta;
        }
	}
}
