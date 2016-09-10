using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Placed on a GameObject to turn it into a navigation mesh.
//
// At scene load time, this component enumerates all the mesh components on
// this component's GameObject and all its children, combines all of that
// mesh's geometry into a single NavigationMesh object, and adds that mesh
// to the scene's Navigation component.
//
public class Navigable : MonoBehaviour
{
    public bool DebugConstruction = false;

    public void Start()
    {
        // Find the scene's Navigation component
        UnityEngine.Object[] obj = UnityEngine.Object.FindObjectsOfType(typeof(Navigation));
        if (obj.Length != 1)
        {
            throw new Exception("Zero or multiple Navigation components in scene");
        }

        Navigation nav = (Navigation)obj[0];

        // Build a NavigationMesh from this object and its children's meshes
        NavigationMesh mesh = new NavigationMesh(GetComponent<Transform>());
        BuildNavigationMesh(mesh);

        mesh.Transform = GetComponent<Transform>();

        // Register the navigation mesh to the scene
        nav.Add(mesh);

        if (DebugConstruction)
        {
            DebugRenderNavMesh(mesh);
        }
    }

    private void BuildNavigationMesh(NavigationMesh navMesh)
    {
        BuildNavigationMesh(navMesh, GetComponent<Transform>());
    }

    private void BuildNavigationMesh(NavigationMesh navMesh, Transform node)
    {
        // Add this object's mesh to the NavigationMesh
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null)
        {
            BuildNavigationMesh(navMesh, mf.sharedMesh);
        }

        // Recursively iterate over children
        foreach (Transform child in node)
        {
            BuildNavigationMesh(navMesh, child);
        }
    }

    // Help to map each edge in the mesh to the faces along that edge.
    // Used to efficiently compute face adjacency information.
    //
    private class EdgeTable
    {
        // Maps each edge to a list of faces which contain that edge.
        //
        // { Vertex1 -> { Vertex2 -> [Face1, Face2, ...] } }
        //
        // Each value is an index into the NavigationMesh's vertex/face lists.
        //
        // To create a consistent mapping, the outer vertex index (Vertex1)
        // should be smaller than the inner vertex index (Vertex2)
        //
        private Dictionary<int, Dictionary<int, List<int>>> map;

        public EdgeTable()
        {
            map = new Dictionary<int, Dictionary<int, List<int>>>();
        }

        public void Add(int vertex1, int vertex2, int face)
        {
            int a = (vertex1 < vertex2) ? vertex1 : vertex2;
            int b = (vertex1 < vertex2) ? vertex2 : vertex1;

            if (!map.ContainsKey(a))
            {
                map[a] = new Dictionary<int, List<int>>();
            }

            if (!map[a].ContainsKey(b))
            {
                map[a][b] = new List<int>();
            }

            map[a][b].Add(face);
        }

        public List<int> GetFaces(int vertex1, int vertex2)
        {
            int a = (vertex1 < vertex2) ? vertex1 : vertex2;
            int b = (vertex1 < vertex2) ? vertex2 : vertex1;

            return map[a][b];
        }
    }

    private void BuildNavigationMesh(NavigationMesh navMesh, Mesh mesh)
    {
        // Add the mesh's vertices/normals to the navigation mesh directly
        navMesh.Vertices.AddRange(mesh.vertices);
        navMesh.Normals.AddRange(mesh.normals);

        // Add the mesh's triangles to the navigation mesh
        if (mesh.triangles.Length % 3 != 0)
        {
            throw new Exception(string.Format("Invalid index buffer: have {0} indices, should be a multiple of 3",
                                              mesh.triangles.Length));
        }

        for (int i = 0; i < mesh.triangles.Length / 3; ++i)
        {
            int a = mesh.triangles[3 * i + 0];
            int b = mesh.triangles[3 * i + 1];
            int c = mesh.triangles[3 * i + 2];

            NavigationMesh.Face f;

            f.A = a;
            f.B = b;
            f.C = c;

            f.NormalA = a;
            f.NormalB = b;
            f.NormalC = c;

            f.AdjacentAB = -1;
            f.AdjacentBC = -1;
            f.AdjacentCA = -1;

            Vector3 va = mesh.vertices[a];
            Vector3 vb = mesh.vertices[b];
            Vector3 vc = mesh.vertices[c];

            f.FaceNormal = Vector3.Cross(vb - va, vc - va).normalized;

            navMesh.Faces.Add(f);
        }

        // Sometimes mesh generators create duplicate vertices with the same
        // coordinates. This is frequently by design (e.g. for cubes, each
        // corner will have three vertices with the same position, but
        // different normals). 
        //
        // We compute face adjacency by looking for faces which share vertices.
        // To that end, we'll delete duplicate vertices at the same position,
        // so that adjacent edges do actually share vertices.
        //
        // To simplify this logic, we don't actually remove duplicate vertices
        // from the vertex array; we just fix all faces to reference the
        // correct vertex.
        //
        for (int i = 0; i < navMesh.Vertices.Count; ++i)
        {
            Vector3 v1 = navMesh.Vertices[i];

            for (int j = i + 1; j < navMesh.Vertices.Count; ++j)
            {
                Vector3 v2 = navMesh.Vertices[j];
                if ((v1 - v2).sqrMagnitude < Mathf.Epsilon * Mathf.Epsilon)
                {
                    for (int k = 0; k < navMesh.Faces.Count; ++k)
                    {
                        NavigationMesh.Face f = navMesh.Faces[k];

                        if (f.A == j) f.A = i;
                        if (f.B == j) f.B = i;
                        if (f.C == j) f.C = i;

                        navMesh.Faces[k] = f;
                    }
                }
            }
        }

        // Build a table mapping each edge to faces containing that edge
        EdgeTable edgeTable = new EdgeTable();
        for (int i = 0; i < navMesh.Faces.Count; ++i)
        {
            NavigationMesh.Face f = navMesh.Faces[i];

            edgeTable.Add(f.A, f.B, i);
            edgeTable.Add(f.B, f.C, i);
            edgeTable.Add(f.C, f.A, i);
        }

        // For each edge of each face, find the adjacent face along that edge
        for (int i = 0; i < navMesh.Faces.Count; ++i)
        {
            NavigationMesh.Face f = navMesh.Faces[i];

            int a = f.A;
            int b = f.B;
            int c = f.C;

            // Adjacency for edge (A, B);
            List<int> faces = edgeTable.GetFaces(a, b);
            if (faces.Count != 2)
            {
                throw new Exception(string.Format("Edge [{0}, {1}] has {2} face(s), expected 2",
                                                  a, b, faces.Count));
            }

            f.AdjacentAB = (faces[0] == i) ? faces[1] : faces[0];

            // Adjacency for edge (B, C)
            faces = edgeTable.GetFaces(b, c);
            if (faces.Count != 2)
            {
                throw new Exception(string.Format("Edge [{0}, {1}] has {2} face(s), expected 2",
                                                  b, c, faces.Count));
            }

            f.AdjacentBC = (faces[0] == i) ? faces[1] : faces[0];

            // Adjacency for edge (C, A)
            faces = edgeTable.GetFaces(c, a);
            if (faces.Count != 2)
            {
                throw new Exception(string.Format("Edge [{0}, {1}] has {2} face(s), expected 2",
                                                  c, a, faces.Count));
            }

            f.AdjacentCA = (faces[0] == i) ? faces[1] : faces[0];

            navMesh.Faces[i] = f;
        }
    }

    void DebugRenderNavMesh(NavigationMesh navMesh)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < navMesh.Faces.Count; ++i)
        {
            NavigationMesh.Face f = navMesh.Faces[i];

            vertices.Add(navMesh.Vertices[f.A]);
            vertices.Add(navMesh.Vertices[f.B]);
            vertices.Add(navMesh.Vertices[f.C]);

            normals.Add(f.FaceNormal);
            normals.Add(f.FaceNormal);
            normals.Add(f.FaceNormal);

            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);
        }

        Mesh mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;

        mesh.vertices = vertices.ToArray();
        mesh.normals = normals.ToArray();
        mesh.triangles = triangles.ToArray();
    }
}
