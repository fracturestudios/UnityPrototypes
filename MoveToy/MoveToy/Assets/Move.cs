using UnityEngine;
using System.Collections;

public class Move : MonoBehaviour
{
	public float Speed = 4.0f;

	void Start()
	{
	}

	void Update()
	{
		Transform transform = GetComponent<Transform>();

		Vector3 forward = transform.rotation * new Vector3(0, 0, 1);
		Vector3 right = transform.rotation * new Vector3(1, 0, 0);

		Vector3 velocity = Vector3.zero;

		if (Input.GetButton(BTN_FORWARD)) {
			velocity += Speed * forward;
		}

		if (Input.GetButton(BTN_BACK)) {
			velocity -= Speed * forward;
		}

		if (Input.GetButton(BTN_LEFT)) {
			velocity -= Speed * right;
		}

		if (Input.GetButton(BTN_RIGHT)) {
			velocity += Speed * right;
		}

		GetComponent<Rigidbody>().velocity = velocity;
	}

	void OnCollisionEnter(Collision c)
	{
		Transform t = GetComponent<Transform>();
		Rigidbody rb = GetComponent<Rigidbody>();

		Vector3 v = rb.velocity;
		foreach (ContactPoint pt in c.contacts) {
			Vector3 normal = Vector3.Normalize(pt.point - t.position);
			v = Vector3.Reflect(v, normal);
		}

		rb.velocity = v;
	}

	private static string BTN_FORWARD = "MoveForward";
	private static string BTN_BACK    = "MoveBackward";
	private static string BTN_LEFT    = "MoveLeft";
	private static string BTN_RIGHT   = "MoveRight";
}
