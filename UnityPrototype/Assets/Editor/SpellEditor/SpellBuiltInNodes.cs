using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SpellBuiltInNodes
{
	public Dictionary<string, SpellNodeType> builtInTypes = new Dictionary<string, SpellNodeType>();

	public SpellBuiltInNodes()
	{
		BuildBinaryOperator("add", "+", "Adds two values together", "Any");
		BuildBinaryOperator("subtract", "-", "Subtracts b from a", "Any");
		BuildBinaryOperator("multiply", "*", "Multiplies a and b", "Any");
		BuildBinaryOperator("divide", "/", "Divides a by b", "Any");
		
		BuildBinaryOperator("less-than", "<", "Return true of a is less than b", "bool");
		BuildBinaryOperator("less-than-equal", "<=", "Return true of a is less than or equal to b", "bool");
		BuildBinaryOperator("greater-than", ">", "Return true of a is greater than b", "bool");
		BuildBinaryOperator("greater-than-equal", ">=", "Return true of a is greater than or equal to b", "bool");
		
		BuildBinaryOperator("equal", "==", "Return true of a is equal to b", "bool");
		BuildBinaryOperator("not-equal", "!=", "Return true of a is not equal to b", "bool");
		
		BuildUnaryOperator("negate", "-", "negates the input", "Any");
		BuildUnaryOperator("not", "!", "returns true if given false", "bool");
	}

	private void BuildBinaryOperator(string name, string operatorToken, string description, string returnType)
	{
		SpellNodeType result = new SpellNodeType(name, null, null);
		result.Description = description;
		result.Namespace = "math";

		SpellNodeConnectorType operandA = new SpellNodeConnectorType("operandA", "Any", false);
		operandA.Description = "the first operand";
		result.AddInput(operandA);
		
		SpellNodeConnectorType operandB = new SpellNodeConnectorType("operandB", "Any", false);
		operandA.Description = "the second operand";
		result.AddInput(operandB);

		SpellNodeConnectorType operatorResult = new SpellNodeConnectorType("result", returnType, true);
		operandA.Description = "the result of the operation";
		result.AddOutput(operatorResult);

		result.SetXmlGenerator(new SpellNodeBinaryOpXmlGenerator(operatorToken));

		builtInTypes[name] = result;
	}
	
	private void BuildUnaryOperator(string name, string operatorToken, string description, string returnType)
	{
		SpellNodeType result = new SpellNodeType(name, null, null);
		result.Description = description;
		result.Namespace = "math";
		
		SpellNodeConnectorType operandA = new SpellNodeConnectorType("operandA", "Any", false);
		operandA.Description = "the first operand";
		result.AddInput(operandA);
		
		SpellNodeConnectorType operatorResult = new SpellNodeConnectorType("result", returnType, true);
		operandA.Description = "the result of the operation";
		result.AddOutput(operatorResult);
		
		result.SetXmlGenerator(new SpellNodeUnaryOpXmlGenerator(operatorToken));
		
		builtInTypes[name] = result;
	}

	public Dictionary<string, SpellNodeType> BuiltInTypes
	{
		get
		{
			return builtInTypes;
		}
	}
}
