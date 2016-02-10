using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MoveSignal : MonoBehaviour {
	private List<SignalMoved> listeners = new List<SignalMoved>();
	private List<MoveSignal> children = new List<MoveSignal>();
	private MoveSignal parent = null;

	public delegate void SignalMoved();

	private void SetParent(MoveSignal value)
	{
		if (parent != value)
		{
			if (parent != null)
			{
				parent.children.Remove (this);
			}

			parent = value;

			if (parent != null)
			{
				parent.children.Add(this);
			}
		}
	}

	void OnEnable()
	{
		CheckParent();
	}

	void OnDisable()
	{
		SetParent(null);
	}

	public void Listen(SignalMoved listener)
	{
		listeners.Add(listener);
	}

	public void Unlisten(SignalMoved listener)
	{
		listeners.Remove(listener);
	}

	public void CheckParent()
	{
		GameObject parentObject = gameObject.GetParent();
		MoveSignal newParent = null;

		if (parentObject != null)
		{
			newParent = parentObject.GetComponent<MoveSignal>();
		}

		SetParent(newParent);
	}

	public static void CheckParent(GameObject target)
	{
		MoveSignal signal = target.GetComponent<MoveSignal>();

		if (signal != null)
		{
			signal.CheckParent();
		}
	}

	public void Moved()
	{
		foreach (SignalMoved signal in listeners)
		{
			signal();
		}

		foreach (MoveSignal child in children)
		{
			child.Moved();
		}
	}

	public static void Moved(GameObject target)
	{
		MoveSignal signal = target.GetComponent<MoveSignal>();
		
		if (signal != null)
		{
			signal.Moved();
		}
	}
}
