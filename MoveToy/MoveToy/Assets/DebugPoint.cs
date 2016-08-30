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
    private static bool RayLineIntersect(Vector3 p,
                                         Vector3 d,
                                         Vector3 a,
                                         Vector3 b,
                                         out Vector3 intersection)
    {
        // To do this intersection, we'll first construct a plane containing 
        // points A and B, perpendicular to the plane containing points a, b
        // and p. Once/ we've done that, the intersection point is just the
        // point at which the ray intersects with the plane.
        
        // The plane normal is the component of pa perpendicular to ba
        Vector3 pa = p - a;
        Vector3 ba = b - a;
        Vector3 n = (pa - (Vector3.Dot(pa, ba) / Vector3.Dot(ba, ba) * ba)).normalized;

        // Find the point of intersection with the plane
        float t = -1f * Vector3.Dot(p - a, n) / Vector3.Dot(d, n);
        if (t < 0f || float.IsInfinity(t))
        {
            intersection = Vector3.zero;
            return false;
        }

        Vector3 result = p + t * d;

        // We found an intersection of the point is between a and b
        float ba2 = ba.sqrMagnitude;
        if ((result - a).sqrMagnitude <= ba2 && (result - b).sqrMagnitude <= ba2)
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

        if (worldDelta != Vector3.zero)
        {
            Quaternion netRotation = Quaternion.identity;

            Vector3 objectPos = m_mesh.Transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);
            Vector3 objectDelta = m_mesh.Transform.worldToLocalMatrix.MultiplyVector(worldDelta);
            Vector3 objectDir = objectDelta.normalized;

            for (;;)
            {
                // Figure out which edge we'll hit if we keep travelling this direction
                NavigationMesh.Face f = m_mesh.Faces[m_currentFace];
                Vector3 a = m_mesh.Vertices[f.A];
                Vector3 b = m_mesh.Vertices[f.B];
                Vector3 c = m_mesh.Vertices[f.C];

                Vector3 edgeIntersect = Vector3.zero;
                int neighbor = -1;

                if (RayLineIntersect(objectPos, objectDir, a, b, out edgeIntersect))
                {
                    neighbor = f.AdjacentAB;
                }
                else if (RayLineIntersect(objectPos, objectDir, b, c, out edgeIntersect))
                {
                    neighbor = f.AdjacentBC;
                }
                else if (RayLineIntersect(objectPos, objectDir, c, a, out edgeIntersect))
                {
                    neighbor = f.AdjacentCA;
                }
                else
                {
                    throw new Exception("Movement ray does not intersect any edge of current face");
                }

                // If we didn't hit an edge, move the full distance and be done
                if (objectDelta.sqrMagnitude < (edgeIntersect - objectPos).sqrMagnitude)
                {
                    objectPos = objectPos + objectDelta;
                    break;
                }

                // We hit the edge of the triangle
                //
                // - Move the player to the edge
                // - Rotate the player around the edge to orient with the next
                //   face's normal
                // - Subtract the travelled distance from the total distance
                //   left to travel
                //
                NavigationMesh.Face adjacent = m_mesh.Faces[neighbor];

                Quaternion rotation = Quaternion.FromToRotation(f.Normal, adjacent.Normal);
                float distanceTravelled = (edgeIntersect - objectPos).magnitude;

                objectPos = edgeIntersect;
                objectDir = rotation * objectDir;
                objectDelta = objectDir * (objectDelta.magnitude - distanceTravelled);

                netRotation = rotation * netRotation;
                m_currentFace = neighbor;
            }

            transform.position = m_mesh.Transform.localToWorldMatrix.MultiplyPoint3x4(objectPos);
            transform.rotation = netRotation * transform.rotation;
        }
	}
}
