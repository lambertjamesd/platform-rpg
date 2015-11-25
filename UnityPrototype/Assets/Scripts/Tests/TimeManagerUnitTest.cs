using UnityEngine;
using System.Collections;

public class TimeManagerUnitTest : UnitTest {
	
	public class TestTimeTraveller : ITimeTravelable
	{
		private TimeManager timeManager;
		private object currentState;
		private object expectedState;

		public object CurrentState
		{
			get
			{
				return currentState;
			}

			set
			{
				currentState = value;
			}
		}

		public object ExpectedState
		{
			get
			{
				return expectedState;
			}

			set
			{
				expectedState = value;
			}
		}
		
		public TestTimeTraveller(TimeManager timeManager)
		{
			this.timeManager = timeManager;
		}
		
		public object GetCurrentState()
		{
			return currentState;
		}
		
		public void RewindToState(object state)
		{
			Assert(state == expectedState, "Rewind to state did not match expected value");
		}
		
		public TimeManager GetTimeManager()
		{
			return timeManager;
		}
	}

	protected override void Run()
	{
		TimeManager timeManager = GetComponent<TimeManager>();

		TestTimeTraveller alwaysStay = new TestTimeTraveller(timeManager);
		alwaysStay.CurrentState = alwaysStay.ExpectedState = "AlwaysStay";

		timeManager.AddTimeTraveler(alwaysStay);

		timeManager.TakeSnapshot();

		TestTimeTraveller willDie = new TestTimeTraveller(timeManager);
		willDie.CurrentState = willDie.ExpectedState = "WillDie";

		timeManager.AddTimeTraveler(willDie);

		TestTimeTraveller willShift = new TestTimeTraveller(timeManager);
		willShift.CurrentState = willShift.ExpectedState = "WillShift";

		timeManager.AddTimeTraveler(willShift);

		timeManager.TakeSnapshot();

		willDie.CurrentState = null;
		willDie.ExpectedState = null;

		timeManager.TakeSnapshot();

		timeManager.TakeSnapshot();

		timeManager.CleanUpSnapshots(1, timeManager.SnapshotCount - 1);

		timeManager.Rewind();

		Assert(timeManager.IsSaved(alwaysStay), "Always stay stayed");
		Assert(!timeManager.IsSaved(willDie), "Will die is gone");
		Assert(timeManager.IsSaved(willShift), "Will shift is still here");
	}
}
