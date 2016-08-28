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

            // Project the position on this triangle
            Vector3 projected = pos - Vector3.Dot(pos, f.Normal) * f.Normal;

            // Convert the projected point into barycentric coordinates
            float area = .5f * Vector3.Cross(b - a, c - a).magnitude;
            float u = .5f * Vector3.Cross(b - projected, c - projected).magnitude / area;
            float v = .5f * Vector3.Cross(a - projected, c - projected).magnitude / area;
            float w = .5f * Vector3.Cross(a - projected, b - projected).magnitude / area;

            // Clamp the coordinates to all lie within the triangle
            u = Mathf.Clamp(u, 0f, 1f);
            v = Mathf.Clamp(v, 0f, 1f);
            w = Mathf.Clamp(w, 0f, 1f);
            
            // Convert the barycentric coordinates back into cartesian space
            Vector3 pt = u * a + v * b + w * c;
            
            // See if this is the closest we've gotten to 'pos' so far
            float distanceSq = (pt - projected).sqrMagnitude;
            if (!haveNearest || distanceSq < nearestDistanceSq)
            {
                haveNearest = true;
                nearestFace = i;
                nearestDistanceSq = distanceSq;
                nearestPoint = pt;
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
        return pos - Vector3.Dot(pos, f.Normal) * f.Normal;
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

            Vector3 n = f.Normal;

            // Find a point on the plane defined by the triangle the ray
            // intersects. If the t value is negative, the ray points the wrong way
            // and never intersects the triangle.
            //
            float t = -1f * Vector3.Dot(origin - a, n) / Vector3.Dot(direction, n);
            if (t < 0f)
            {
                continue;
            }

            Vector3 point = origin + t * direction;

            // Convert the point into barycentric coordinates. If any coordinate is
            // outside [0, 1], then the point is outside the triangle and doesn't
            // intersect
            //
            float area = .5f * Vector3.Cross(b - a, c - a).magnitude;
            float u = .5f * Vector3.Cross(b - point, c - point).magnitude / area;
            float v = .5f * Vector3.Cross(a - point, c - point).magnitude / area;
            float w = .5f * Vector3.Cross(a - point, b - point).magnitude / area;

            if (u < 0f || u > 1f || v < 0f || v > 1f || w < 0f || w > 1f)
            {
                continue;
            }

            // Otherwise we found and intersection; see if this is the first
            // triangle we would have hit of the triangles considered so far
            //
            if (!foundNearest || nearestDistance < t)
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

