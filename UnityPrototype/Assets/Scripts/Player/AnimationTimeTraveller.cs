using UnityEngine;
using System.Collections;

public class AnimationTimeTraveller : MonoBehaviour, ITimeTravelable {

	private AnimatorStateSaver saver;
	private TimeManager timeManager;

	void Start () {
		timeManager = timeManager ?? gameObject.GetComponentWithAncestors<TimeManager>();
		timeManager.AddTimeTraveler(this);
		saver = new AnimatorStateSaver(GetComponent<Animator>());
	}
	
	public virtual object GetCurrentState()
	{
		return saver.GetCurrentState();
	}
	
	public virtual void RewindToState(object state)
	{
		if (state == null)
		{
			Destroy(gameObject);
		}
		else
		{
			saver.RewindToState(state);
		}
	}
	
	public TimeManager GetTimeManager()
	{
		return timeManager;
	}
}
