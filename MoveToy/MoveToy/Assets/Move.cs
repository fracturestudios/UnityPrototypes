using UnityEngine;
using System.Collections;

public class Move : MonoBehaviour
{
	public float Speed = 4.0f;

	void Start() { }

	void Update()
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
}
