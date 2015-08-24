using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AnimatorStateSaver {

	private Animator animator;
	private List<AnimationParameter> parameters;

	public AnimatorStateSaver(Animator target)
	{
		animator = target;

		parameters = new List<AnimationParameter>();

		foreach (AnimatorControllerParameter param in target.parameters)
		{
			if (param.type == AnimatorControllerParameterType.Bool)
			{
				parameters.Add(new BoolAnimationParameter(param.nameHash));
			} 
			else if (param.type == AnimatorControllerParameterType.Float)
			{
				parameters.Add(new FloatAnimationParameter(param.nameHash));
			}
			else if (param.type == AnimatorControllerParameterType.Int)
			{
				parameters.Add(new IntAnimationParameter(param.nameHash));
			}
		}
	}

	private abstract class AnimationParameter
	{
		protected int hashId;

		protected AnimationParameter(int hashId)
		{
			this.hashId = hashId;
		}

		public abstract void Set(Animator target, object value);
		public abstract object Get(Animator target);
	}

	private class BoolAnimationParameter : AnimationParameter
	{
		public BoolAnimationParameter(int hashId) : base(hashId)
		{

		}

		public override void Set(Animator target, object value)
		{
			target.SetBool(hashId, (bool)value);
		}
		
		public override object Get(Animator target)
		{
			return target.GetBool(hashId);
		}
	}
	
	private class FloatAnimationParameter : AnimationParameter
	{
		public FloatAnimationParameter(int hashId) : base(hashId)
		{

		}
		
		public override void Set(Animator target, object value)
		{
			target.SetFloat(hashId, (float)value);
		}
		
		public override object Get(Animator target)
		{
			return target.GetFloat(hashId);
		}
	}
	
	private class IntAnimationParameter : AnimationParameter
	{
		public IntAnimationParameter(int hashId) : base(hashId)
		{

		}
		
		public override void Set(Animator target, object value)
		{
			target.SetInteger(hashId, (int)value);
		}
		
		public override object Get(Animator target)
		{
			return target.GetInteger(hashId);
		}
	}

	private class AnimatorState
	{
		public AnimatorState(AnimatorStateInfo[] layerStates, object[] parameterValues)
		{
			this.layerStates = layerStates;
			this.parameterValues = parameterValues;
		}
		                    

		public AnimatorStateInfo[] layerStates;
		public object[] parameterValues;
	}

	public object GetCurrentState()
	{
		AnimatorStateInfo[] layerStates = new AnimatorStateInfo[animator.layerCount];

		for (int i = 0; i < animator.layerCount; ++i)
		{
			layerStates[i] = animator.GetNextAnimatorStateInfo(i);
		}

		object[] parameterValues = new object[parameters.Count];

		for (int i = 0; i < parameterValues.Length; ++i)
		{
			parameterValues[i] = parameters[i].Get(animator);
		}

		return new AnimatorState(layerStates, parameterValues);
	}

	public void RewindToState(object state)
	{
		AnimatorState animState = (AnimatorState)state;

		for (int i = 0; i < animState.layerStates.Length; ++i)
		{
			animator.CrossFade(animState.layerStates[i].fullPathHash, 0.0f, i, animState.layerStates[i].normalizedTime);
		}

		for (int i = 0; i < animState.parameterValues.Length; ++i)
		{
			parameters[i].Set(animator, animState.parameterValues[i]);
		}
	}
}
