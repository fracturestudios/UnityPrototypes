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

		if (Input.GetButton(BTN_FORWARD)) {
			transform.position += Speed * Time.deltaTime * forward;
		}

		if (Input.GetButton(BTN_BACK)) {
			transform.position -= Speed * Time.deltaTime * forward;
		}

		if (Input.GetButton(BTN_LEFT)) {
			transform.position -= Speed * Time.deltaTime * right;
		}

		if (Input.GetButton(BTN_RIGHT)) {
			transform.position += Speed * Time.deltaTime * right;
		}
	}

	private static string BTN_FORWARD = "MoveForward";
	private static string BTN_BACK    = "MoveBackward";
	private static string BTN_LEFT    = "MoveLeft";
	private static string BTN_RIGHT   = "MoveRight";
}
