using UnityEngine;
using System.Collections;

public class DebugMove : MonoBehaviour
{
	private bool CursorCaptured
	{
		get
		{
			return Cursor.lockState == CursorLockMode.Locked;
		}
		set
		{
			Cursor.visible = !value;
			Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}

	void Start()
	{
		CursorCaptured = true;
	}

	void Update()
	{
        Transform transform = GetComponent<Transform>();

        // Look
		if (CursorCaptured)
		{
			transform.RotateAround(transform.position,
			                       transform.rotation * new Vector3(1, 0, 0),
			                       -1.0f * Input.GetAxis("LookVertical"));

			transform.RotateAround(transform.position,
			                       transform.rotation * new Vector3(0, 1, 0),
			                       Input.GetAxis("LookHorizontal"));

			if (Input.GetButton("ReleaseCursor"))
			{
				CursorCaptured = false;
			}
		}
		else if (Input.GetButton("CaptureCursor"))
		{
			CursorCaptured = true;
		}

        // Move
        const float speed = 12f;
        float dt = Time.deltaTime;

        Vector3 pos = transform.position;

		Vector3 forward = transform.rotation * new Vector3(0, 0, 1);
		Vector3 right = transform.rotation * new Vector3(1, 0, 0);

		if (Input.GetButton("MoveForward")) {
			pos += speed * dt * forward;
		}

		if (Input.GetButton("MoveBackward")) {
			pos -= speed * dt * forward;
		}

		if (Input.GetButton("MoveLeft")) {
			pos -= speed * dt * right;
		}

		if (Input.GetButton("MoveRight")) {
			pos += speed * dt * right;
		}

        transform.position = pos;
	}
}
