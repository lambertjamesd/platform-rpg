using UnityEngine;
using System.Collections;

[System.Serializable]
public class PlayerSettings {

	public string characterName = "";
	public Texture characterImage;

	public float moveAcceleration = 4.0f;

	public float airAcceleration = 0.5f;

	public float wallAngleTolerance = 20.0f;

	public float wallSlideDamping = 0.5f;

	public float wallStickTime = 0.1f;

	private float cosWallAngleTolerance;

	public void RecalculateDerivedValues()
	{
		cosWallAngleTolerance = Mathf.Cos(Mathf.Deg2Rad * wallAngleTolerance);
	}

	public float CosWallAngleTolerance
	{
		get
		{
			return cosWallAngleTolerance;
		}
	}
}
