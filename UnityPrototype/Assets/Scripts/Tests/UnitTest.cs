using UnityEngine;
using System.Collections;
using System;

public class UnitTest : MonoBehaviour {

	protected static void Assert(bool condition, string message)
	{
		if (!condition)
		{
			throw new Exception(message);
		}
	}

	// Use this for initialization
	void Start () {
		RunTest();
	}

	protected virtual void Run()
	{

	}

	public void RunTest()
	{
		try
		{
			Run ();
			GetComponent<Renderer>().material.color = Color.green;
		}
		catch (Exception e)
		{
			Debug.LogError(e.ToString() + e.StackTrace);
			GetComponent<Renderer>().material.color = Color.red;
		}
	}
}
