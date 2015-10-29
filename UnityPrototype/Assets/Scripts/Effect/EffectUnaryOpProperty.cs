using System;
using UnityEngine;

public abstract class EffectUnaryOpProperty : EffectProperty
{
	protected EffectProperty operand;
	protected string operatorName;

	protected EffectUnaryOpProperty (EffectProperty operand, string operatorName)
	{
		this.operand = operand;
		this.operatorName = operatorName;
	}

	public static EffectUnaryOpProperty CreateOperator(EffectProperty operand, string operatorString)
	{
		switch (operatorString)
		{
		case "-":
			return new EffectNegateOpProperty(operand);
		case "!":
			return new EffectBooleanNotProperty(operand);
		}

		return null;
	}

	public string OperatorName
	{
		get
		{
			return operatorName;
		}
	}

	public EffectProperty Operand
	{
		get
		{
			return operand;
		}
	}
	
	public override void Accept (EffectPropertyVisitor visitor)
	{
		visitor.Visit(this);
	}
}

public class EffectNegateOpProperty : EffectUnaryOpProperty
{
	public EffectNegateOpProperty(EffectProperty operand) : base (operand, "-")
	{

	}

	public override object GetObjectValue(EffectPropertyChain chain)
	{
		object value = operand.GetObjectValue(chain);
		
		if (value is float)
		{
			return -(float)value;
		}
		else if (value is Vector3)
		{
			return -(Vector3)value;
		}
		
		return null;
	}
	
	public override void Accept (EffectPropertyVisitor visitor)
	{
		visitor.Visit(this);
	}
}

public class EffectBooleanNotProperty : EffectUnaryOpProperty
{
	public EffectBooleanNotProperty(EffectProperty operand) : base (operand, "!")
	{
		
	}
	
	public override object GetObjectValue(EffectPropertyChain chain)
	{
		return !EffectAndProperty.AsBoolean(operand.GetObjectValue(chain));
	}
	
	public override void Accept (EffectPropertyVisitor visitor)
	{
		visitor.Visit(this);
	}
}
