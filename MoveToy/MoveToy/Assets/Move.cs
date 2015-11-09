using UnityEngine;
using System.Collections;

public class Move : MonoBehaviour
{
	public float Speed = 4.0f;

	void Start() { }

	// TODO
	// - Keep using DebugMove always, don't apply gravity, do print gravity
	// - Get gravity vector computation working
	//   - Currently it's definitely wonky -- take a look at the print output without even moving the
	//     player pawn from its spawn position
	// - Then extend it to including multiple triangles
	// - Then add gravity to player movement
	// - Then change movement to work within the plane of gravity

	void Update()
	{
		Vector3 gravity;
		if (ComputeGravity(out gravity)) {
			Rigidbody rb = GetComponent<Rigidbody>();
			rb.AddForce(gravity);

			Vector3 n = gravity.normalized;
			Debug.Log(string.Format("Gravity: {0} {1} {2}",
			                        n.x,
			                        n.y,
			                        n.z));
		}
		else {
		}

		DebugMove();
	}

	private void DebugMove()
	{
		Transform transform = GetComponent<Transform>();

		Vector3 forward = transform.rotation * new Vector3(0, 0, 1);
		Vector3 right = transform.rotation * new Vector3(1, 0, 0);

		Vector3 force = Vector3.zero;

		if (Input.GetButton(BTN_FORWARD)) {
			force += Speed * forward;
		}

		if (Input.GetButton(BTN_BACK)) {
			force -= Speed * forward;
		}

		if (Input.GetButton(BTN_LEFT)) {
			force -= Speed * right;
		}

		if (Input.GetButton(BTN_RIGHT)) {
			force += Speed * right;
		}

		GetComponent<Rigidbody>().AddForce(force);
	}

	private static string BTN_FORWARD = "MoveForward";
	private static string BTN_BACK    = "MoveBackward";
	private static string BTN_LEFT    = "MoveLeft";
	private static string BTN_RIGHT   = "MoveRight";

	private bool ComputeGravity(out Vector3 gravity)
	{
		Transform transform = GetComponent<Transform>();

		// Find the closest geometry point in the scene and use its normal as
		// the direction of gravity (i.e. gravity is the opposite of the normal).
		//
		// In a real game, we'd want to accelerate this lookup using a spatial
		// subdivision data structure like an octree or kd-tree. However, in this
		// demo we have very few (<1000) terrain triangles, so we can get away
		// with the really inefficient traversal over every object in the scene :-)
		//
		Vector3 nearestNormal = Vector3.zero;
		float nearestDistance = float.MaxValue;
		bool foundNormal = false;

		foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Terrain")) {
			MeshFilter mf = obj.GetComponent<MeshFilter>();
			if (mf == null) {
				continue;
			}

			for (int i = 2; i < mf.mesh.triangles.Length; i += 3) {
				int ia = mf.mesh.triangles[i - 2];
				int ib = mf.mesh.triangles[i - 1];
				int ic = mf.mesh.triangles[i];

				Vector3 pa = mf.mesh.vertices[ia];
				Vector3 pb = mf.mesh.vertices[ib];
				Vector3 pc = mf.mesh.vertices[ic];

				Vector3 planeNormal = Vector3.Cross(pa - pb, pa - pc).normalized;
				Vector3 vecToPlane = Vector3.Project(transform.position - pa, planeNormal);
				float distanceToPlane = Mathf.Abs(vecToPlane.magnitude);

				if (distanceToPlane < nearestDistance && distanceToPlane < GRAVITY_MAX_DISTANCE) {
					/* TODO DEBUG: just use the nearest normal for now
					Vector3 posOnPlane = transform.position - vecToPlane;
					Vector2 uv = InverseBilinear(pa, pb, pc, posOnPlane);

					Vector3 na = mf.mesh.normals[ia];
					Vector3 nb = mf.mesh.normals[ib];
					Vector3 nc = mf.mesh.normals[ic];

					nearestNormal = Bilinear(na, nb, nc, uv);
					*/
					nearestNormal = mf.mesh.normals[ia];

					nearestDistance = distanceToPlane;
					foundNormal = true;
				}
			}
		}

		if (foundNormal) {
			gravity = -GRAVITY * nearestNormal;
			return true;
		}
		else {
			gravity = Vector3.zero;
			return false;
		}
	}

	/// <summary>
	/// Finds the point of intersection between the line passing through a and b,
	/// and the line passing through c and d. Assumes the two lines are not 
	/// parallel.
	/// </summary>
	private static Vector3 Intersect(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
	{
		// Due to floating point precision/inequality, the two lines may be 
		// extremely close to intersecting without actually intersecting.
		// Instead of using plain intersection, we'll instead find the point
		// on AB which is closest to CD

		// This was basically ripped directly from ClosestPointsOnTwoLines
		// on http://wiki.unity3d.com/index.php/3d_Math_functions

		Vector3 ab = b - a;
		Vector3 cd = c - d;

		float A = Vector3.Dot(ab, ab);
		float B = Vector3.Dot(ab, cd);
		float E = Vector3.Dot(cd, cd);
 
		float D = A * E - B * B;
 
		Vector3 R = a - c;
		float C = Vector3.Dot(ab, R);
		float F = Vector3.Dot(cd, R);

		float T = (A * F - C * B) / D;
		return c + cd * T;
	}

	/// <summary>
	/// Computes the value t such that p = Vector3.Lerp(a, b, t).
	/// </summary>
	/// <remarks>
	/// a, b and p must be colinear -- otherwise, the method will return a value,
	/// but the value will be meaningless.
	/// </remarks>
	private static float InverseLerp(Vector3 a, Vector3 b, Vector3 p)
	{
		return (p - a).magnitude / (b - a).magnitude;
	}

	private static Vector2 InverseBilinear(Vector3 a, Vector3 b, Vector3 c, Vector3 p)
	{
		Vector3 q = Intersect(p, a, b, c);
		float u = InverseLerp(b, c, q);
		float v = InverseLerp(a, q, p);

		return new Vector2(u, v);
	}

	private static Vector3 Bilinear(Vector3 a, Vector3 b, Vector3 c, Vector2 uv)
	{
		float u = uv.x;
		float v = uv.y;

		Vector3 q = Vector3.Lerp(b, c, u);
		Vector3 p = Vector3.Lerp(a, q, v);

		return p;
	}

	private static float GRAVITY = 9.8f;
	private static float GRAVITY_MAX_DISTANCE = 2.0f;
}
