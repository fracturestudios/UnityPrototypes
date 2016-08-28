using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Describes a surface that a character can navigate across.
// All vertex positions are defined in the navigation mesh's object space.
//
public class NavigationMesh
{
    public struct Face // A triangle, with vertices defined in winding order
    {
        public int A; // Index into mesh.Vertices for vertex A
        public int B; // Index into mesh.Vertices for vertex B
        public int C; // Index into mesh.Vertices for vertex C

        public int AdjacentAB; // Index into mesh.Faces for the face which shares edge AB
        public int AdjacentBC; // Index into mesh.Faces for the face which shares edge BC
        public int AdjacentCA; // Index into mesh.Faces for the face which shares edge CA

        public Vector3 Normal; // Cached normal vector pointing out of the face's front
    }

    public List<Vector3> Vertices; // All vertices in the mesh
    public List<Face> Faces;       // All triangles in this mesh
    public Transform Transform;    // The object -> world transformation

    // Finds the point in this mesh closest to the given point.
    // The input position should be defined in this mesh's object space.
    //
    // Returns the index in this.Faces of the face which contains the nearest
    // point, as well as the nearest point in this mesh's object space.
    //
    public void FindNearest(Vector3 pos,
                            out int face,
                            out Vector3 intersect)
    {
        throw new NotImplementedException(); // TODO
    }

    // Projects the given position on the given face along the face's normal.
    // Returns the resulting coordinate in cartesian coordinates
    //
    public Vector3 Project(Vector3 pos, int face)
    {
        throw new NotImplementedException(); // TODO
    }

    // Finds the first point on this mesh the given ray intersects.
    // The origin/direction should be defined in this mesh's object space.
    //
    // If an intersection is found, the "intersected" out-param will be true,
    // the "face" out-parameter will contain index in this.Faces of the first
    // face intersected, and the "intersect" out-param will contain the point
    // of intersection in this mesh's object space.
    //
    // If no intersection is found, the "intersected" out-parameter will be
    // false, and the values of the other out-parameters will be undefined.
    //
    public void Intersect(Vector3 origin,
                          Vector3 direction,
                          out bool intersected,
                          out int face,
                          out Vector3 intersect)
    {
        throw new NotImplementedException(); // TODO
    }
}

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

// Placed on a GameObject to turn it into a navigation mesh.
//
// At scene load time, this component enumerates all the mesh components on
// this component's GameObject and all its children, combines all of that
// mesh's geometry into a single NavigationMesh object, and adds that mesh
// to the scene's Navigation component.
//
public class Navigable : MonoBehaviour
{
    public void Awake() { }
}
