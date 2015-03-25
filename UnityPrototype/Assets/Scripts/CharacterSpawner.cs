using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CharacterSpawner : MonoBehaviour {

	public Player playerInstance;

	public void Awake () {
		if (playerInstance != null)
		{
			PlayerManager manager = gameObject.GetComponentWithAncestors<PlayerManager>();

			// The player needs to be instantiated disabled so OnEnable is not 
			// called before its parent is assigned the correct transform
			bool wasActive = playerInstance.gameObject.activeSelf;
			playerInstance.gameObject.SetActive(false);

			Player newPlayer = (Player)Instantiate(playerInstance, transform.position, transform.rotation);
			newPlayer.transform.parent = transform.parent;
			manager.AddPlayer(newPlayer);

			// Re-enable the instantiated player
			newPlayer.gameObject.SetActive(wasActive);
			playerInstance.gameObject.SetActive(wasActive);

			Destroy(gameObject);

		}
	}

	public void OnDrawGizmos()
	{
		CharacterController controller = playerInstance == null ? null : playerInstance.GetComponent<CharacterController>();

		Vector3 center = Vector3.up * 0.5f;
		Vector3 up = Vector3.up;
		float height = 1.0f;
		float radius = 0.25f;;
		Color color = Color.red;

		if (controller != null)
		{
			center = controller.center;
			up = Vector3.up;
			height = controller.height;
			radius = controller.radius;
			color = Color.green;
#if UNITY_EDITOR
			Handles.Label(transform.TransformPoint(center), playerInstance.name);
#endif
		}
			                        
		GizmoHelper.DrawCapsule(transform.TransformPoint(center), transform.TransformDirection(up), height, radius, color);
	}
}
