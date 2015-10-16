using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpellAttributeBuilder : EffectPropertyVisitor {

	SpellXmlLoader xmlLoader;
	Stack<SpellNodeConnector> currentConnector = new Stack<SpellNodeConnector>();
	List<int> idList = null;
	int idIndex = 0;
	bool skipCreation = false;

	public SpellAttributeBuilder(SpellXmlLoader xmlLoader, List<int> idList, SpellNodeConnector startingConnector)
	{
		this.xmlLoader = xmlLoader;
		this.idList = idList;
		currentConnector.Push(startingConnector);
	}

	public int GetAttributeID()
	{
		if (idList == null)
		{
			return -1;
		}
		else
		{
			return idList[idIndex];
		}
	}

	public void AdvanceID()
	{
		++idIndex;
	}

	private SpellNodeConnector CurrentConnector
	{
		get
		{
			return currentConnector.Peek();
		}
	}
	
	public override void Visit<I>(EffectConstantProperty<I> constantProperty)
	{
		if (skipCreation)
		{
			return;
		}

		switch (typeof(I).Name)
		{
		case "Single":
		case "Int32":
		case "String":
			CurrentConnector.DefaultValue = constantProperty.GetValue().ToString();

			if (CurrentConnector.Type.Name == "Prefab")
			{
				int index = -1;

				if (CurrentConnector.DefaultValue != null &&int.TryParse(CurrentConnector.DefaultValue, out index))
				{
					CurrentConnector.Parent.ParentEditor.CurretlyOpenSpell.AddReference(index);
				}
			}
			break;
		case "Vector3":
		{
			break;
		}
		case "Boolean":
			CurrentConnector.DefaultValue = constantProperty.GetValue().ToString().ToLower();
			break;
		case "Object":
			// do nothing, this is a null value
			break;
		default:
			xmlLoader.XmlReaderError("Unrecognized constant type " + typeof(I).Name);
			break;
		}
	}

	public override void Visit(EffectChainProperty propertyChain)
	{
		if (skipCreation)
		{
			return;
		}

		SpellNode targetNode = xmlLoader.GetNode(propertyChain.NodeID);

		if (targetNode == null)
		{
			xmlLoader.XmlReaderError("Could not find node named " + propertyChain.NodeID);
		}
		else
		{
			string eventName = xmlLoader.GetEventName(propertyChain.NodeID);
			SpellNodeConnector sourceConnector = null;

			if (eventName == null)
			{
				sourceConnector = targetNode.GetOutputConnector(propertyChain.PropertyName);
			}
			else
			{
				sourceConnector = targetNode.GetEventOutputConnector(eventName, propertyChain.PropertyName);
			}

			if (sourceConnector == null)
			{
				xmlLoader.XmlReaderError("Could not find property named " + propertyChain.ToString());
			}
			else
			{
				xmlLoader.SpellEditor.AddConnection(sourceConnector, CurrentConnector);
			}
		}
	}

	public override void Visit(EffectBinaryOpProperty binaryOp)
	{
		if (skipCreation)
		{
			AdvanceID();
			binaryOp.OperandA.Accept(this);
			binaryOp.OperandB.Accept(this);
			return;
		}

		string nodeName = "undefined";
		
		switch (binaryOp.OperatorName)
		{
		case "+":
			nodeName = "add";
			break;
		case "-":
			nodeName = "subtract";
			break;
		case "*":
			nodeName = "multiply";
			break;
		case "/":
			nodeName = "divide";
			break;
			
		case "<":
			nodeName = "less-than";
			break;
		case "<=":
			nodeName = "less-than-equal";
			break;
		case ">":
			nodeName = "greater-than";
			break;
		case ">=":
			nodeName = "greater-than-equal";
			break;
			
		case "==":
			nodeName = "equal";
			break;
		case "!=":
			nodeName = "not-equal";
			break;
		case "&&":
			nodeName = "and";
			break;
		case "||":
			nodeName = "or";
			break;
		}
		
		SpellNodeType operatorType = xmlLoader.GetBuiltInType(nodeName);
		
		if (operatorType == null)
		{
			xmlLoader.XmlReaderError("Undefined operator " + binaryOp.OperatorName);
		}
		else
		{
			if (operatorType.InputCount != 2)
			{
				xmlLoader.XmlReaderError("input count mismatch binary operator: " + binaryOp.OperatorName);
			}
			else
			{
				bool alreadyExists = xmlLoader.HasExistingAttribute(GetAttributeID());
				SpellNode operatorNode = xmlLoader.AddAttributeNode(operatorType, GetAttributeID());
				xmlLoader.SpellEditor.AddConnection(operatorNode.GetOutputConnector(0), CurrentConnector);
				AdvanceID();

				if (alreadyExists)
				{
					skipCreation = true;
					binaryOp.OperandA.Accept(this);
					binaryOp.OperandB.Accept(this);
					skipCreation = false;
				}
				else
				{
					currentConnector.Push(operatorNode.GetInputConnector(0));
					binaryOp.OperandA.Accept(this);
					currentConnector.Pop();
					currentConnector.Push(operatorNode.GetInputConnector(1));
					binaryOp.OperandB.Accept(this);
					currentConnector.Pop();
				}
			}
		}
	}

	public override void Visit(EffectUnaryOpProperty unaryOp)
	{
		if (skipCreation)
		{
			AdvanceID();
			unaryOp.Operand.Accept(this);
			return;
		}

		string nodeName = "undefined";

		switch (unaryOp.OperatorName)
		{
		case "-":
			nodeName = "negate";
			break;
		case "!":
			nodeName = "not";
			break;
		}

		SpellNodeType operatorType = xmlLoader.GetBuiltInType(nodeName);
		
		if (operatorType == null)
		{
			xmlLoader.XmlReaderError("Undefined operator " + unaryOp.OperatorName);
		}
		else
		{
			if (operatorType.InputCount != 1)
			{
				xmlLoader.XmlReaderError("input count mismatch unary operator: " + unaryOp.OperatorName);
			}
			else
			{
				bool alreadyExists = xmlLoader.HasExistingAttribute(GetAttributeID());

				SpellNode operatorNode = xmlLoader.AddAttributeNode(operatorType, GetAttributeID());
				xmlLoader.SpellEditor.AddConnection(operatorNode.GetOutputConnector(0), CurrentConnector);
				AdvanceID();

				if (alreadyExists)
				{
					skipCreation = true;
					unaryOp.Operand.Accept(this);
					skipCreation = false;
				}
				else
				{
					currentConnector.Push(operatorNode.GetInputConnector(0));
					unaryOp.Operand.Accept(this);
					currentConnector.Pop();
				}
			}
		}
	}

	public override void Visit(EffectFunctionProperty function)
	{
		if (skipCreation)
		{
			AdvanceID();
			for (int i = 0; i < function.ParameterCount; ++i)
			{
				function.GetParameter(i).Accept(this);
			}
			return;
		}

		SpellNodeType functionType = xmlLoader.GetFunctionType(function.Name);

		if (functionType == null)
		{
			xmlLoader.XmlReaderError("Undefined function " + function.Name);
		}
		else
		{
			if (functionType.InputCount != function.ParameterCount)
			{
				xmlLoader.XmlReaderError("parameter count mismatch function: " + function.Name);
			}
			else
			{
				bool alreadyExists = xmlLoader.HasExistingAttribute(GetAttributeID());
				SpellNode functionNode = xmlLoader.AddAttributeNode(functionType, GetAttributeID());
				xmlLoader.SpellEditor.AddConnection(functionNode.GetOutputConnector(0), CurrentConnector);
				AdvanceID();

				if (alreadyExists)
				{
					skipCreation = true;
					for (int i = 0; i < function.ParameterCount; ++i)
					{
						function.GetParameter(i).Accept(this);
					}
					skipCreation = false;
				}
				else
				{
					for (int i = 0; i < function.ParameterCount; ++i)
					{
						currentConnector.Push(functionNode.GetInputConnector(i));
						function.GetParameter(i).Accept(this);
						currentConnector.Pop();
					}
				}
			}
		}
	}
}
