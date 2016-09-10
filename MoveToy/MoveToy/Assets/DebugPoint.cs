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
        rotation = Quaternion.FromToRotation(Vector3.up, m_mesh.Faces[m_currentFace].FaceNormal);
        m_moveDir = rotation * m_moveDir;
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
            Debug.Log("Point is on the edge, T: 0");
        }
        else
        {
            t = -1f * Vector3.Dot(p - a, n) / Vector3.Dot(d, n);
            Debug.Log(string.Format("T: {0}", t));
        }

        if (t < 0f && -t < epsilon)
        {
            t = 0f;
            Debug.Log("T is negative but small, T: 0");
        }

        if (t < 0f)
        {
            Debug.Log(string.Format("T is negative: {0}", t));
            intersection = Vector3.zero;
            return false;
        }

        if (float.IsInfinity(t))
        {
            Debug.Log("T is infinity");
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
            Debug.Log("Entering triangle via this edge");
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
            Debug.Log(string.Format("Result point {0} not between edge {1} {2}: |RA|: {3} |RB|: {4} |AB|: {5}",
                                    edgeIntersect, a, b, (edgeIntersect - a).magnitude, (edgeIntersect - b).magnitude, (b - a).magnitude));
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

        Vector3 worldDelta = Vector3.zero;

		if (Input.GetButton("AnchorForward")) {
			worldDelta += speed * dt * Vector3.forward;
		}

		if (Input.GetButton("AnchorBackward")) {
			worldDelta -= speed * dt * Vector3.forward;
		}

		if (Input.GetButton("AnchorLeft")) {
			worldDelta -= speed * dt * Vector3.right;
		}

		if (Input.GetButton("AnchorRight")) {
			worldDelta += speed * dt * Vector3.right;
		}

        worldDelta = m_moveDir * worldDelta;

        Vector3 objectPos = m_mesh.Transform.worldToLocalMatrix.MultiplyPoint3x4(transform.position);

        while (worldDelta.sqrMagnitude > Mathf.Epsilon)
        {
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
            // - Rotate the player's movement directions around the new face's
            //   face normal
            // - Subtract the travelled distance from the total distance
            //   left to travel
            // - Continue on to the adjacent face
            //
            m_currentFace = neighbor;
            NavigationMesh.Face adjacent = m_mesh.Faces[m_currentFace];

            Debug.Log(string.Format("Moving from {0} {1} {2} to {3} {4} {5}",
                                    m_mesh.Vertices[f.A],
                                    m_mesh.Vertices[f.B],
                                    m_mesh.Vertices[f.C],
                                    m_mesh.Vertices[adjacent.A],
                                    m_mesh.Vertices[adjacent.B],
                                    m_mesh.Vertices[adjacent.C]));

            float deltaRatio = (worldDelta.magnitude - m_mesh.Transform.localToWorldMatrix.MultiplyVector(edgeIntersect - objectPos).magnitude)
                             / worldDelta.magnitude;
            worldDelta *= deltaRatio;

            objectPos = edgeIntersect;
            transform.position = m_mesh.Transform.localToWorldMatrix.MultiplyPoint3x4(objectPos);

            Quaternion rotation = Quaternion.FromToRotation(f.NormalAt(objectPos, m_mesh),
                                                            adjacent.NormalAt(objectPos, m_mesh));
            transform.rotation = rotation * transform.rotation;
            worldDelta = rotation * worldDelta;

            rotation = Quaternion.FromToRotation(m_moveDir * Vector3.up, adjacent.FaceNormal);
            m_moveDir = rotation * m_moveDir;
        }
	}
}

