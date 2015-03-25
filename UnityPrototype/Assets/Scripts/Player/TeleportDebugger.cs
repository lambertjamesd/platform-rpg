using UnityEngine;
using System.Collections;

public class TeleportDebugger : MonoBehaviour {

	public Vector3 centerOffset;
	public float height = 1.0f;
	public float radius = 0.25f;

	private TilemapOverlapCorrecter overlapCorrector;

	private Vector3 targetPosition;

	// Use this for initialization
	void Start () {
		overlapCorrector = gameObject.GetComponentWithAncestors<TilemapOverlapCorrecter>();
	}
	
	// Update is called once per frame
	void Update () {
		Ray mousePosition = Camera.main.ScreenPointToRay(Input.mousePosition);
		float distance = (transform.position.z - mousePosition.origin.z) / mousePosition.direction.z;
		
		targetPosition = mousePosition.GetPoint(distance);

		if (Input.GetKeyDown(KeyCode.Space))
		{
			Debug.Log(targetPosition.x.ToString() + ", " + targetPosition.y.ToString());
		}

		targetPosition = overlapCorrector.CorrectCapsuleOverlap(targetPosition, Vector3.up, height, radius);
	}

	void OnDrawGizmos() {
		float centerOffset = height * 0.5f - radius;
		GizmoHelper.DrawThickLine(targetPosition - Vector3.up * (centerOffset), targetPosition + Vector3.up * (centerOffset), radius, Color.green);
	}
}
