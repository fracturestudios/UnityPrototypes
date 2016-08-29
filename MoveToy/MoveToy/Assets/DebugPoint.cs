using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugPoint : MonoBehaviour
{
	void Update()
    {
        // Get the test scene's navigation mesh
        UnityEngine.Object[] obj = UnityEngine.Object.FindObjectsOfType(typeof(Navigation));
        if (obj.Length != 1)
        {
            throw new Exception("Zero or multiple Navigation components in scene");
        }

        Navigation nav = (Navigation)obj[0];

        List<NavigationMesh> meshes = new List<NavigationMesh>(nav.Meshes);
        NavigationMesh mesh = meshes[0];

        // Convert the player position into the mesh's object space
        Transform player = GameObject.FindGameObjectsWithTag("Player")[0].GetComponent<Transform>();

        Vector3 objectSpace = mesh.Transform.worldToLocalMatrix.MultiplyPoint(player.position);

        // Find the closest position on that mesh to the player
        int face;
        Vector3 point;
        mesh.NearestPoint(objectSpace, out face, out point);
        
        // Translate that position back into world space and move the object
        Vector3 worldSpace = mesh.Transform.localToWorldMatrix.MultiplyPoint(point);

        Transform myTransform = GetComponent<Transform>();
        myTransform.position = worldSpace;
	}
}
