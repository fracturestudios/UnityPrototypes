using UnityEngine;
using System.Collections;

public class Look : MonoBehaviour
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
		if (CursorCaptured)
		{
			Transform transform = GetComponent<Transform>();

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
	}
}
