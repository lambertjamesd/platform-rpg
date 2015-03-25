using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour {

	public PlayerManager playerManager; 
	public float cameraDistance = 10;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		transform.position = playerManager.CurrentPlayer.transform.position;
		transform.position += new Vector3 (0, 0, -cameraDistance);

	}
}
