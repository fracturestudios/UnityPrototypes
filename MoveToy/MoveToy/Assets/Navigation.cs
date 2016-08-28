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
        throw new NotImplementedException();
    }

    // Removes a navigation mesh from the scene
    public void Remove(NavigationMesh mesh)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
    }
}

