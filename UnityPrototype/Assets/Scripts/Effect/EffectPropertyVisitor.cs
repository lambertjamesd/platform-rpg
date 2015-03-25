using System;

public abstract class EffectPropertyVisitor
{
	public EffectPropertyVisitor()
	{
	}

	public abstract void Visit<I>(EffectConstantProperty<I> constantProperty);
	public abstract void Visit(EffectChainProperty propertyChain);
	public abstract void Visit(EffectBinaryOpProperty propertyChain);
	public abstract void Visit(EffectUnaryOpProperty propertyChain);
	public abstract void Visit(EffectFunctionProperty propertyChain);
}