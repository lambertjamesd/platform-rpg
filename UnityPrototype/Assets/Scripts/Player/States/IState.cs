using UnityEngine;
using System.Collections;

public interface IState {
	void BeginState(StateMachine stateMachine);
	void Update(StateMachine stateMachine, float timestep);
	void EndState(StateMachine stateMachine);
}
