using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages navigation geometry for the entire scene, and provides scene-level
// query support. To use navigation meshes, the scene must contain exactly one
// instance of this object.
//
public class Navigation : MonoBehaviour
{
    private List<NavigationMesh> meshes;

    public Navigation()
    {
        meshes = new List<NavigationMesh>();
    }

    // Adds a new navigation mesh to the scene
    public void Add(NavigationMesh mesh)
    {
        meshes.Add(mesh);
    }

    // Removes a navigation mesh from the scene
    public void Remove(NavigationMesh mesh)
    {
        meshes.Remove(mesh);
    }

    // Enumerates all registered meshes in the scene
    public IEnumerable<NavigationMesh> Meshes
    {
        get { return meshes; }
    }

    // Enumerates all navigation meshes in the scene which have at least one
    // point within the given distance of the given position. Both parameters
    // are defined in world space.
    //
    public IEnumerable MeshesNear(Vector3 pos, float distance)
    {
        // TODO FUTURE: This could be optimized using a spatial subdivision
        // data structure. Consider only cells that are within the given
        // distance from the given position.
        //
        // This could be further optimized by keeping and checking an
        // axis-aligned bounding box for each mesh, to fast-reject meshes whose
        // entire bounding box is too far away.

        foreach (NavigationMesh mesh in meshes)
        {
            // Convert the world-space position into the 
            Matrix4x4 trans = mesh.Transform.worldToLocalMatrix;
            Vector3 point = trans.MultiplyPoint3x4(pos);
            
            int nearestFace;
            Vector3 nearestPoint;
            mesh.NearestPoint(point, out nearestFace, out nearestPoint);

            if ((nearestPoint - point).sqrMagnitude < distance * distance)
            {
                yield return mesh;
            }
        }
    }
}

