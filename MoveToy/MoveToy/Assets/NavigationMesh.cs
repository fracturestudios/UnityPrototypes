using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Describes a surface that a character can navigate aCross.
// All vertex positions are defined in the navigation mesh's object space.
//
public class NavigationMesh
{
    public struct Face // A triangle, with vertices defined in winding order
    {
        public int A; // Index into mesh.Vertices for vertex A
        public int B; // Index into mesh.Vertices for vertex B
        public int C; // Index into mesh.Vertices for vertex C

        public int NormalA; // Index into mesh.Normals for normal at vertex A
        public int NormalB; // Index into mesh.Normals for normal at vertex B
        public int NormalC; // Index into mesh.Normals for normal at vertex C

        public int AdjacentAB; // Index into mesh.Faces for the face which shares edge AB
        public int AdjacentBC; // Index into mesh.Faces for the face which shares edge BC
        public int AdjacentCA; // Index into mesh.Faces for the face which shares edge CA

        public Vector3 FaceNormal; // Normal pointing out of this face

        // Computes the normal for this mesh at the given point
        public Vector3 NormalAt(Vector3 p, NavigationMesh mesh)
        {
            /*
            Vector3 a = mesh.Vertices[this.A];
            Vector3 b = mesh.Vertices[this.B];
            Vector3 c = mesh.Vertices[this.C];

            Vector3 na = mesh.Normals[this.NormalA];
            Vector3 nb = mesh.Normals[this.NormalB];
            Vector3 nc = mesh.Normals[this.NormalC];

            Vector3 v0 = b - a, v1 = c - a,v2 = p - a;
            float d00 = Vector3.Dot(v0, v0);
            float d01 = Vector3.Dot(v0, v1);
            float d11 = Vector3.Dot(v1, v1);
            float d20 = Vector3.Dot(v2, v0);
            float d21 = Vector3.Dot(v2, v1);
            float denom = d00 * d11 - d01 * d01;
            float v = (d11 * d20 - d01 * d21) / denom;
            float w = (d00 * d21 - d01 * d20) / denom;
            float u = 1.0f - v - w;

            return na * u + nb * v + nc * w;
            */

            return this.FaceNormal; // TODO DEBUG
        }
    }

    public List<Vector3> Vertices { get; set; } // All vertices in the mesh
    public List<Vector3> Normals { get; set; }  // All normals in the mesh
    public List<Face> Faces { get; set; }       // All triangles in this mesh
    public Transform Transform { get; set; }    // The object -> world transformation

    public NavigationMesh(Transform transform)
    {
        Vertices = new List<Vector3>();
        Normals = new List<Vector3>();
        Faces = new List<Face>();
        Transform = transform;
    }

    // Finds the point in this mesh closest to the given point.
    // The input position should be defined in this mesh's object space.
    //
    // Returns the index in this.Faces of the face which contains the nearest
    // point, as well as the nearest point in this mesh's object space.
    //
    public void NearestPoint(Vector3 pos, out int face, out Vector3 point)
    {
        // TODO FUTURE: This could be optimized using a spatial subdivision
        // data structure. March outward from the given position until finding
        // an occupied cell, and consider only triangles in that cell

        bool haveNearest = false;
        int nearestFace = 0;
        float nearestDistanceSq = 0;
        Vector3 nearestPoint = Vector3.zero;

        for (int i = 0; i < Faces.Count; ++i)
        {
            Face f = Faces[i];

            Vector3 a = Vertices[f.A];
            Vector3 b = Vertices[f.B];
            Vector3 c = Vertices[f.C];

            // Project the position on this triangle's plane
            Vector3 vec = pos - a;
            Vector3 projected = vec - Vector3.Dot(vec, f.FaceNormal) * f.FaceNormal + a;

            // Figure out if the projected point is inside the triangle or not.
            // If it's not, it'll be on the wrong side of exactly one edge.
            //
            bool isOutside = false;
            Vector3 e1 = Vector3.zero;
            Vector3 e2 = Vector3.zero;

            Vector3 ab = b - a;
            Vector3 bc = c - b;
            Vector3 ca = a - c;

            Vector3 ap = projected - a;
            Vector3 bp = projected - b;
            Vector3 cp = projected - c;

            if (Vector3.Dot(Vector3.Cross(ab, -ca), Vector3.Cross(ab, ap)) < 0)
            {
                isOutside = true;
                e1 = a;
                e2 = b;
            }

            if (Vector3.Dot(Vector3.Cross(bc, -ab), Vector3.Cross(bc, bp)) < 0)
            {
                isOutside = true;
                e1 = b;
                e2 = c;
            }

            if (Vector3.Dot(Vector3.Cross(ca, -bc), Vector3.Cross(ca, cp)) < 0)
            {
                isOutside = true;
                e1 = c;
                e2 = a;
            }

            // If the point is outside the triangle, move it to the nearest
            // point on the edge of the triangle
            //
            if (isOutside)
            {
                // Project the point onto the edge
                Vector3 edge = (e2 - e1).normalized;
                projected = Vector3.Dot(projected - e1, edge) * edge + e1;

                // If this put the projected point beyond the boundary of the
                // edge, move the point to the boundary
                //
                float edgeLen2 = (e2 - e1).sqrMagnitude;

                if ((projected - e1).sqrMagnitude > edgeLen2)
                {
                    projected = e2;
                }
                else if ((projected - e2).sqrMagnitude > edgeLen2)
                {
                    projected = e1;
                }
            }

            // See if this is the closest we've gotten to 'pos' so far
            float distanceSq = (projected - pos).sqrMagnitude;
            if (!haveNearest || distanceSq < nearestDistanceSq)
            {
                haveNearest = true;
                nearestFace = i;
                nearestDistanceSq = distanceSq;
                nearestPoint = projected;
            }
        }

        face = nearestFace;
        point = nearestPoint;
    }

    // Projects the given position on the given face along the face's normal.
    // Returns the resulting coordinate in cartesian coordinates
    //
    public Vector3 Project(Vector3 pos, int face)
    {
        Face f = Faces[face];
        return pos - Vector3.Dot(pos, f.FaceNormal) * f.FaceNormal;
    }

    // Finds the first point on this mesh the given ray intersects.
    // The origin/direction should be defined in this mesh's object space.
    //
    // If an intersection is found, this function returns true, the "face"
    // out-param contains the index in this.Faces of the first face
    // intersected, and the "intersect" out-param contains the point of
    // intersection.
    //
    // If no intersection was found, this function returns false, and both
    // out-params will have undefined values.
    //
    public bool Intersect(Vector3 origin,
                          Vector3 direction,
                          out int face,
                          out Vector3 intersect)
    {
        // TODO FUTURE: This could be optimized using a spatial subdivision
        // data structure. March the ray through the data structure and
        // consider only triangles found in the first occupied region.
        
        face = -1;
        intersect = Vector3.zero;

        bool foundNearest = false;
        int nearestFace = 0;
        Vector3 nearestPoint = Vector3.zero;
        float nearestDistance = 0;

        for (int i = 0; i < Faces.Count; ++i)
        {
            Face f = Faces[i];

            Vector3 a = Vertices[f.A];
            Vector3 b = Vertices[f.B];
            Vector3 c = Vertices[f.C];

            Vector3 n = f.FaceNormal;

            // Find a point on the plane defined by the triangle the ray
            // intersects. If the t value is negative, the ray points the wrong way
            // and never intersects the triangle.
            //
            float t = -1f * Vector3.Dot(origin - a, n) / Vector3.Dot(direction, n);
            if (t < 0f || float.IsInfinity(t))
            {
                continue;
            }

            Vector3 point = origin + t * direction;

            // If the point of intersection with the plane is outside the
            // triangle, skip this triangle
            //
            Vector3 ab = b - a;
            Vector3 bc = c - b;
            Vector3 ca = a - c;

            Vector3 ap = point - a;
            Vector3 bp = point - b;
            Vector3 cp = point - c;

            if (Vector3.Dot(Vector3.Cross(ab, -ca), Vector3.Cross(ab, ap)) < 0 ||
                Vector3.Dot(Vector3.Cross(bc, -ab), Vector3.Cross(bc, bp)) < 0 ||
                Vector3.Dot(Vector3.Cross(ca, -bc), Vector3.Cross(ca, cp)) < 0)
            {
                continue;
            }

            // Otherwise we found and intersection; see if this is the first
            // triangle we would have hit of the triangles considered so far
            //
            if (!foundNearest || t < nearestDistance)
            {
                foundNearest = true;
                nearestFace = i;
                nearestPoint = point;
                nearestDistance = t;
            }
        }

        face = nearestFace;
        intersect = nearestPoint;
        return foundNearest;
    }
}

