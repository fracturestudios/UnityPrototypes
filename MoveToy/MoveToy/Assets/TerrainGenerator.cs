using UnityEngine;
using System.Collections;

public class TerrainGenerator : MonoBehaviour
{
	public Transform terrainObject;

	void Start()
	{
		for (int i = 0; i < NUM_OBJECTS; ++i) {
			Transform item = Instantiate(terrainObject);

			item.position = new Vector3(Random.Range(X_MIN, X_MAX),
			                            Random.Range(Y_MIN, Y_MAX),
			                            Random.Range(Z_MIN, Z_MAX));

			item.eulerAngles = new Vector3(Random.Range(.0f, 360.0f),
			                               Random.Range(.0f, 360.0f),
			                               Random.Range(.0f, 360.0f));
		}
	}
	
	void Update()
	{
	}

	private static int NUM_OBJECTS = 40;

	private static float X_MIN = -50.0f;
	private static float X_MAX =  50.0f;
	private static float Y_MIN = -50.0f;
	private static float Y_MAX =  50.0f;
	private static float Z_MIN = -100.0f;
	private static float Z_MAX =  100.0f;
}
