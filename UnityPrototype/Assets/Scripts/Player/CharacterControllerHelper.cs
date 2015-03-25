using UnityEngine;
using System.Collections;

public class CharacterControllerHelper : MonoBehaviour {

	// Use this for initialization
	void Start () {
		CharacterController characterController = GetComponent<CharacterController>();
		Collider[] colliders = GetComponentsInChildren<Collider>();

		foreach (Collider collider in colliders)
		{
			if (characterController != collider)
			{
				Physics.IgnoreCollision(characterController, collider);
			}
		}
	}
}
