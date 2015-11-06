using UnityEngine;
using System.Collections;

public class FightStarter : MonoBehaviour {
	void Start () {
		FightSetup setup = Object.FindObjectOfType<FightSetup>();

		if (setup != null)
		{
			setup.StartGame();
		}

		Destroy(gameObject);
	}
}
