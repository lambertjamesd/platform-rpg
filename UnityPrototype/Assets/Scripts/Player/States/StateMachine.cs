using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StateMachine {

	public delegate IState StateSource(string stateName);

	private IState currentState;
	private StateSource stateSource;

	private string nextStateName;
	private IState nextState;

	private Dictionary<string, System.Object> parameters;

	public StateMachine(StateSource stateSource)
	{
		this.stateSource = stateSource;
		currentState = stateSource("Start");
		currentState.BeginState(this);
	}

	private IState GetNextState()
	{
		IState result = null;

		if (nextState != null)
		{
			result = nextState;
		}
		else if (nextStateName != null)
		{
			result = stateSource(nextStateName);
		}

		nextState = null;
		nextStateName = null;

		return result;
	}

	private void SetCurrentState(IState newState)
	{
		currentState.EndState(this);
		currentState = newState;
		currentState.BeginState(this);
	}

	public void Update(float timestep)
	{
		currentState.Update(this, timestep);

		IState nextState = GetNextState();

		if (nextState != null)
		{
			SetCurrentState(nextState);
		}
	}

	public void SetNextState(IState value)
	{
		nextState = value;
		nextStateName = null;
	}

	public void SetNextState(string name)
	{
		SetNextState(name, null);
	}

	public void SetNextState(string name, Dictionary<string, System.Object> parameters)
	{
		nextState = null;
		nextStateName = name;
		this.parameters = parameters;
	}

	public System.Object GetParameter(string key)
	{
		if (parameters != null && parameters.ContainsKey(key))
		{
			return parameters[key];
		}
		else
		{
			return null;
		}
	}

	public T GetParameter<T>(string key, T defaultValue)
	{
		System.Object result = GetParameter(key);

		if (result != null && result is T)
		{
			return (T)result;
		}
		else
		{
			return defaultValue;
		}
	}

	private class StateMachineState
	{
		public IState currentState;
		public object currentStateSavedState;
		
		public string nextStateName;
		public IState nextState;
		
		public Dictionary<string, System.Object> parameters;

		public StateMachineState(IState currentState, string nextStateName, IState nextState, Dictionary<string, object> parameters)
		{
			this.currentState = currentState;
			this.nextStateName = nextStateName;
			this.nextState = nextState;
			this.parameters = parameters;

			currentStateSavedState = currentState.GetCurrentState();
		}
	}
	
	public object GetCurrentState()
	{
		return new StateMachineState(currentState, nextStateName, nextState, parameters);
	}
	
	public void RewindToState(object state)
	{
		StateMachineState stateMachineState = (StateMachineState)state;

		currentState = stateMachineState.currentState;
		nextStateName = stateMachineState.nextStateName;
		nextState = stateMachineState.nextState;
		parameters = stateMachineState.parameters;

		currentState.RewindToState(stateMachineState.currentStateSavedState);
	}
}
