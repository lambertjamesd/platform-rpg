using UnityEngine;
using System.Collections;

public class FightStarter : MonoBehaviour {
	void Start () {
		Object.FindObjectOfType<FightSetup>().StartGame();
		Destroy(gameObject);
	}
}
