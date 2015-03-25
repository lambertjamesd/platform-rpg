using UnityEngine;
using System.Collections;

public class KnockbackHandler : MonoBehaviour, IKnockbackReciever {
	private Vector3 currentImpulse;

	public void Knockback(Vector3 impulse)
	{
		currentImpulse += impulse;
	}

	public Vector3 CurrentImpulse
	{
		get
		{
			return currentImpulse;
		}
	}

	public void ClearImpulse()
	{
		currentImpulse = Vector3.zero;
	}
}
