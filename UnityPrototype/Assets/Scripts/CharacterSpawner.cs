using UnityEngine;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CharacterSpawner : MonoBehaviour {
	public Player playerInstance;
	public int team;
	public int playerIndex;
	public float healthAmount = 1.0f;

	public void Awake () {
		FightSetup fightSetup = Object.FindObjectOfType<FightSetup>();

		if (fightSetup != null && fightSetup.IsStarted)
		{
			playerInstance = fightSetup.NextPlayer(team);
		}

		if (playerInstance != null)
		{
			PlayerManager manager = gameObject.GetComponentWithAncestors<PlayerManager>();

			// The player needs to be instantiated disabled so OnEnable is not 
			// called before its parent is assigned the correct transform
			bool wasActive = playerInstance.gameObject.activeSelf;
			playerInstance.gameObject.SetActive(false);

			Player newPlayer = (Player)Instantiate(playerInstance, transform.position, transform.rotation);
			newPlayer.Team = team;
			newPlayer.PlayerIndex = playerIndex;
			newPlayer.transform.parent = transform.parent;
			manager.AddPlayer(newPlayer);

			Damageable damageable = newPlayer.gameObject.GetComponent<Damageable>();

			if (damageable != null)
			{
				damageable.startingHealth = healthAmount;
			}

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
			color = TeamColors.GetColor(team);
#if UNITY_EDITOR
			Handles.Label(transform.TransformPoint(center), playerInstance.name);
#endif
		}
			                        
		GizmoHelper.DrawCapsule(transform.TransformPoint(center), transform.TransformDirection(up), height, radius, color);
	}
}
