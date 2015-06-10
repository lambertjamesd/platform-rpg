using UnityEngine;
using System.Collections;

public class BuffVisualizer : MonoBehaviour {

	public TrailRenderer trailRenderer;
	public float rendererTimeScale = 1;

	private PlayerStats stats;

	// Use this for initialization
	void Start () {
		stats = GetComponent<PlayerStats>();
	}
	
	// Update is called once per frame
	void Update () {
		float moveSpeedScale = stats.GetStatScale("maxMoveSpeed");

		if (moveSpeedScale > 1.0f)
		{
			trailRenderer.enabled = true;
			trailRenderer.time = Mathf.Log(moveSpeedScale) * rendererTimeScale;
		}
		else
		{
			trailRenderer.enabled = false;
		}
	}
}
