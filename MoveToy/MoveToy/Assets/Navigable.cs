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
    public void Awake() { }
}
