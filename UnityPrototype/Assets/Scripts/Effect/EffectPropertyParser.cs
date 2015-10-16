using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

public enum EffectTokenType
{
	None,
	OpOpenP,
	OpCloseP,
	OpOpenCB,
	OpCloseCB,
	OpComma,
	Number,
	Identifier,
	OpDot,
	Whitespace,
	String,
	OpAdd,
	OpMinus,
	OpMul,
	OpDiv,

	OpLessThan,
	OpLessEqual,
	OpGreaterThan,
	OpGreaterEqual,

	OpEqual,
	OpNotEqual,

	OpAnd,
	OpOr,

	OpNot,

	EOF,
	Error
}

public struct EffectToken
{
	public EffectToken(EffectTokenType tokenType, string value, int position)
	{
		this.tokenType = tokenType;
		this.value = value;
		this.position = position;
	}

	public EffectTokenType tokenType;
	public string value;
	public int position;

	public bool IsUnaryOperator()
	{
		return tokenType == EffectTokenType.OpMinus || tokenType == EffectTokenType.OpNot;
	}

	public bool IsBinaryOperator()
	{
		return tokenType >= EffectTokenType.OpAdd &&
			tokenType <= EffectTokenType.OpOr;
	}
}

public class EffectPropertyParseException : Exception
{
	public EffectPropertyParseException(string message) : base(message)
	{

	}

	public EffectPropertyParseException(EffectToken token) : base("Unexpected text '" + token.value + "' at " + token.position)
	{
		
	}
	
	
	public EffectPropertyParseException(EffectToken token, string message) : base(message + " '" + token.value + "' at " + token.position)
	{
		
	}
}

public class EffectPropertyTokenizer
{
	private delegate State State(char currentChar, ref EffectTokenType tokenType);

	private string source;
	private int currentTokenStart = 0;
	private int currentPosition = 0;
	private State state;

	private static State NumberInt(char currentChar, ref EffectTokenType tokenType)
	{
		if (char.IsDigit(currentChar))
		{
			return NumberInt;
		}
		else if (currentChar == '.')
		{
			return NumberDecimalPoint;
		}
		else if (currentChar == 'i')
		{
			return NumberIntEnd;
		}
		else
		{
			tokenType = EffectTokenType.Number;
			return DefaultState(currentChar);
		}
	}
	
	private static State NumberIntEnd(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.Number;
		return DefaultState(currentChar);
	}

	private static State NumberDecimalPoint(char currentChar, ref EffectTokenType tokenType)
	{
		if (char.IsDigit(currentChar))
		{
			return NumberFraction;
		}
		else
		{
			tokenType = EffectTokenType.Number;
			return DefaultState(currentChar);
		}
	}

	private static State NumberFraction(char currentChar, ref EffectTokenType tokenType)
	{
		if (char.IsDigit(currentChar))
		{
			return NumberFraction;
		}
		else
		{
			tokenType = EffectTokenType.Number;
			return DefaultState(currentChar);
		}
	}
	
	private static State WhitespaceState(char currentChar, ref EffectTokenType tokenType)
	{
		if (char.IsWhiteSpace(currentChar))
		{
			return WhitespaceState;
		}
		else
		{
			tokenType = EffectTokenType.Whitespace;
			return DefaultState(currentChar);
		}
	}
	
	private static State OpClosePState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpCloseP;
		return DefaultState(currentChar);
	}
	
	private static State OpOpenPState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpOpenP;
		return DefaultState(currentChar);
	}
	
	private static State OpCloseCBState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpCloseCB;
		return DefaultState(currentChar);
	}
	
	private static State OpOpenCBState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpOpenCB;
		return DefaultState(currentChar);
	}
	
	private static State OpCommaState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpComma;
		return DefaultState(currentChar);
	}
	
	private static State OpDotState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpDot;
		return DefaultState(currentChar);
	}
	
	private static State OpAddState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpAdd;
		return DefaultState(currentChar);
	}
	
	private static State OpMinusState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpMinus;
		return DefaultState(currentChar);
	}
	
	private static State OpMulState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpMul;
		return DefaultState(currentChar);
	}

	private static State OpDivState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpDiv;
		return DefaultState(currentChar);
	}
	
	private static State OpLessThan(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '=')
		{
			return OpLessEqual;
		}
		else
		{
			tokenType = EffectTokenType.OpLessThan;
			return DefaultState(currentChar);
		}
	}
	
	private static State OpLessEqual(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpLessEqual;
		return DefaultState(currentChar);
	}
	
	
	private static State OpGreaterThan(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '=')
		{
			return OpGreaterEqual;
		}
		else
		{
			tokenType = EffectTokenType.OpGreaterThan;
			return DefaultState(currentChar);
		}
	}
	
	private static State OpGreaterEqual(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpGreaterEqual;
		return DefaultState(currentChar);
	}

	private static State OpEqual(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '=')
		{
			return OpEqualFinish;
		}
		else
		{
			tokenType = EffectTokenType.Error;
			return ErrorState;
		}
	}
	
	private static State OpEqualFinish(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpEqual;
		return DefaultState(currentChar);
	}
	
	private static State OpNotEqual(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpNotEqual;
		return DefaultState(currentChar);
	}
	
	private static State OpNot(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '=')
		{
			return OpNotEqual;
		}
		else
		{
			tokenType = EffectTokenType.OpNot;
			return DefaultState(currentChar);
		}
	}

	private static State OpAnd(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '&')
		{
			return OpAndAnd;
		}
		else
		{
			tokenType = EffectTokenType.Error;
			return ErrorState;
		}
	}
	
	private static State OpAndAnd(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpAnd;
		return DefaultState(currentChar);
	}
	
	private static State OpOr(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '&')
		{
			return OpOrOr;
		}
		else
		{
			tokenType = EffectTokenType.Error;
			return ErrorState;
		}
	}
	
	private static State OpOrOr(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.OpOr;
		return DefaultState(currentChar);
	}

	private static State StringState(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '\"')
		{
			return StringEndState;
		}
		else if (currentChar == '\\')
		{
			return StringEscapeState;
		}
		else if (currentChar == '\0')
		{
			tokenType = EffectTokenType.Error;
			return ErrorState;
		}
		else
		{
			return StringState;
		}
	}

	private static State StringEndState(char currentChar, ref EffectTokenType tokenType)
	{
		tokenType = EffectTokenType.String;
		return DefaultState(currentChar);
	}

	private static State StringEscapeState(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '\0')
		{
			tokenType = EffectTokenType.Error;
			return ErrorState;
		}
		else
		{
			return StringState;
		}
	}
	
	private static State IdentifierState(char currentChar, ref EffectTokenType tokenType)
	{
		if (char.IsLetterOrDigit(currentChar))
		{
			return IdentifierState;
		}
		else
		{
			tokenType = EffectTokenType.Identifier;
			return DefaultState(currentChar);
		}
	}

	private static State StartState(char currentChar, ref EffectTokenType tokenType)
	{
		return DefaultState(currentChar);
	}

	private static State ErrorState(char currentChar, ref EffectTokenType tokenType)
	{
		if (currentChar == '\0')
		{
			tokenType = EffectTokenType.Error;
			return EOFState;
		}
		else
		{
			return EOFState;
		}
	}

	private static State EOFState(char currentChar, ref EffectTokenType tokenType)
	{
		return EOFState;
	}

	private static State DefaultState(char currentChar)
	{
		if (char.IsDigit(currentChar))
		{
			return NumberInt;
		}
		else if (char.IsLetter(currentChar))
		{
			return IdentifierState;
		}
		else if (char.IsWhiteSpace(currentChar))
		{
			return WhitespaceState;
		}
		else if (currentChar == '(')
		{
			return OpOpenPState;
		}
		else if (currentChar == ')')
		{
			return OpClosePState;
		}
		else if (currentChar == '{')
		{
			return OpOpenCBState;
		}
		else if (currentChar == '}')
		{
			return OpCloseCBState;
		}
		else if (currentChar == '"')
		{
			return StringState;
		}
		else if (currentChar == '.')
		{
			return OpDotState;
		}
		else if (currentChar == '+')
		{
			return OpAddState;
		}
		else if (currentChar == '-')
		{
			return OpMinusState;
		}
		else if (currentChar == '*')
		{
			return OpMulState;
		}
		else if (currentChar == '/')
		{
			return OpDivState;
		}
		else if (currentChar == '<')
		{
			return OpLessThan;
		}
		else if (currentChar == '>')
		{
			return OpGreaterThan;
		}
		else if (currentChar == '=')
		{
			return OpEqual;
		}
		else if (currentChar == '!')
		{
			return OpNot;
		}
		else if (currentChar == '&')
		{
			return OpAnd;
		}
		else if (currentChar == '|')
		{
			return OpOr;
		}
		else if (currentChar == ',')
		{
			return OpCommaState;
		}
		else if (currentChar == '\0')
		{
			return EOFState;
		}
		else
		{
			return ErrorState;
		}
	}
	
	public EffectPropertyTokenizer(string source)
	{
		this.source = source;
	}

	public List<EffectToken> Tokenize()
	{
		List<EffectToken> result = new List<EffectToken>();

		if (source != null && source.Length != 0)
		{
			State currentState = StartState;

			while (currentState != null && currentPosition <= source.Length)
			{
				char currentChar;

				if (currentPosition == source.Length)
				{
					currentChar = '\0';
				}
				else
				{
					currentChar = source[currentPosition];
				}

				EffectTokenType tokenType = EffectTokenType.None;
				currentState = currentState(currentChar, ref tokenType);

				if (tokenType != EffectTokenType.None)
				{
					if (tokenType != EffectTokenType.Whitespace)
					{
						string tokenValue = source.Substring(currentTokenStart, currentPosition - currentTokenStart);
						result.Add(new EffectToken(tokenType, tokenValue, currentTokenStart));
					}
					else if (tokenType == EffectTokenType.Error)
					{
						throw new EffectPropertyParseException("Unexpected character '" + currentChar + "' at col " + currentTokenStart);
					}

					currentTokenStart = currentPosition;
				}

				++currentPosition;
			}
		}

		result.Add(new EffectToken(EffectTokenType.EOF, "", source.Length));
		return result;
	}
}

public class EffectPropertyParser {

	private string source;
	private List<EffectToken> tokenList;
	private List<string> idList;
	private int currentIndex = 0;

	public EffectPropertyParser(string source, List<string> idList)
	{
		EffectPropertyTokenizer tokenizer = new EffectPropertyTokenizer(source);
		tokenList = tokenizer.Tokenize();
		this.idList = idList;
	}

	private string UnescapeString(string input)
	{
		StringWriter result = new StringWriter();

		// skip enclosing parenthesis
		for (int i = 1; i < input.Length - 1; ++i)
		{
			if (input[i] == '\\')
			{
				++i;

				switch (input[i])
				{
				case '\r':
					result.Write('\r');
					break;
				case '\n':
					result.Write('\n');
					break;
				case '\t':
					result.Write('\t');
					break;
				default:
					result.Write(input[i]);
					break;
				}
			}
			else
			{
				result.Write(input[i]);
			}
		}

		return result.ToString();
	}

	private EffectToken Advance()
	{
		EffectToken result = CurrentToken;
		++currentIndex;
		return result;
	}

	private EffectToken Require(EffectTokenType tokenType)
	{
		EffectToken currentToken = CurrentToken;

		if (currentToken.tokenType == tokenType)
		{
			Advance();
			return currentToken;
		}
		else
		{
			throw new EffectPropertyParseException(CurrentToken);
		}
	}

	private EffectToken Peek(int offset)
	{
		if (currentIndex + offset < tokenList.Count)
		{
			return tokenList[currentIndex + offset];
		}
		else
		{
			return tokenList[tokenList.Count - 1];
		}
	}

	private bool Optional(EffectTokenType tokenType)
	{
		if (CurrentToken.tokenType == tokenType)
		{
			Advance();
			return true;
		}
		else
		{
			return false;
		}

	}

	private EffectToken CurrentToken
	{
		get
		{
			return tokenList[currentIndex];
		}
	}

	private float ParseNumber()
	{
		return float.Parse(Require(EffectTokenType.Number).value);
	}

	private int ParseInt()
	{
		string stringInt = Require(EffectTokenType.Number).value;
		return int.Parse(stringInt.Substring(0, stringInt.Length - 1));
	}

	private Vector3 ParseVector()
	{
		Require(EffectTokenType.OpOpenCB);

		float x = 0;
		float y = 0;
		float z = 0;

		x = ParseNumber();

		if (Optional(EffectTokenType.OpComma))
		{
			y = ParseNumber();

			if (Optional(EffectTokenType.OpComma))
			{
				z = ParseNumber();
			}
		}

		Require(EffectTokenType.OpCloseCB);

		return new Vector3(x, y, z);
	}

	private EffectProperty ParseFunction()
	{
		EffectToken functionName = Require(EffectTokenType.Identifier);

		List<EffectProperty> parameters = new List<EffectProperty>();

		Require(EffectTokenType.OpOpenP);

		while (!Optional(EffectTokenType.OpCloseP))
		{
			parameters.Add(ParseBinaryOperator());

			if (!Optional(EffectTokenType.OpComma))
			{
				if (Peek(0).tokenType != EffectTokenType.OpCloseP)
				{
					throw new EffectPropertyParseException(CurrentToken);
				}
			}
		}

		EffectFunctionProperty.PropertyFunction function = EffectFactory.GetInstance().GetFunction(functionName.value);

		if (function == null)
		{
			throw new EffectPropertyParseException(functionName, "Function not defined");
		}
		else
		{
			return new EffectFunctionProperty(functionName.value, function, parameters);
		}
	}

	private EffectProperty ParseIdentifier()
	{
		EffectToken objectName = Require(EffectTokenType.Identifier);

		if (objectName.value == "true")
		{
			return new EffectConstantProperty<bool>(true);
		}
		else if (objectName.value == "false")
		{
			return new EffectConstantProperty<bool>(false);
		}
		else if (objectName.value == "null")
		{
			return new EffectConstantProperty<object>(null);
		}

		StringWriter propertyName = new StringWriter();
		bool hasStarted = false;

		while (Optional(EffectTokenType.OpDot))
		{
			if (hasStarted)
			{
				propertyName.Write(".");
			}
			else
			{
				hasStarted = true;
			}

			propertyName.Write(Require(EffectTokenType.Identifier).value);
		}

		int index = idList.LastIndexOf(objectName.value);

		if (index == -1)
		{
			throw new EffectPropertyParseException("Could not find object with id '" + objectName.value + "'");
		}

		return new EffectChainProperty(idList.Count - index - 1, objectName.value, propertyName.ToString());
	}

	private EffectProperty ParseParenthesis()
	{
		Require(EffectTokenType.OpOpenP);
		EffectProperty result = ParseBinaryOperator();
		Require(EffectTokenType.OpCloseP);
		return result;
	}

	private EffectProperty ParseNumber(bool negate)
	{
		EffectToken currentToken = CurrentToken;

		if (currentToken.value[currentToken.value.Length - 1] == 'i')
		{
			return new EffectConstantProperty<int>(ParseInt() * (negate ? -1 : 1));
		}
		else
		{
			return new EffectConstantProperty<float>(ParseNumber() * (negate ? -1.0f : 1.0f));
		}
	}

	private EffectProperty ParseValue()
	{
		EffectToken currentToken = CurrentToken;

		if (currentToken.tokenType == EffectTokenType.Number)
		{
			return ParseNumber(false);
		}
		else if (currentToken.tokenType == EffectTokenType.String)
		{
			Advance();
			return new EffectConstantProperty<string>(UnescapeString(currentToken.value));
		}
		else if (currentToken.tokenType == EffectTokenType.OpOpenCB)
		{
			return new EffectConstantProperty<Vector3>(ParseVector());
		}
		else if (currentToken.tokenType == EffectTokenType.Identifier)
		{
			if (Peek(1).tokenType == EffectTokenType.OpOpenP)
			{
				return ParseFunction();
			}
			else
			{
				return ParseIdentifier();
			}
		}
		else if (currentToken.tokenType == EffectTokenType.OpOpenP)
		{
			return ParseParenthesis();
		}
		else
		{
			throw new EffectPropertyParseException(currentToken);
		}
	}

	private EffectProperty ParseUnaryOperator()
	{
		if (Peek(0).IsUnaryOperator())
		{
			EffectToken unaryOperator = Advance();

			if (unaryOperator.tokenType == EffectTokenType.OpMinus && Peek(0).tokenType == EffectTokenType.Number)
			{
				return ParseNumber(true);
			}
			else
			{
				return EffectUnaryOpProperty.CreateOperator(ParseUnaryOperator(), unaryOperator.value);
			}
		}
		else
		{
			return ParseValue();
		}
	}

	private EffectProperty ParseBinaryOperator(EffectOperatorPrecedence minPrecedence = EffectOperatorPrecedence.MinPrecedence)
	{
		EffectProperty operandA = ParseUnaryOperator();

		while (Peek(0).IsBinaryOperator())
		{
			EffectOperatorPrecedence precedence = EffectBinaryOpProperty.GetPrecedence(Peek(0).value);

			if (precedence >= minPrecedence)
			{
				EffectToken operatorToken = Advance();
				operandA = EffectBinaryOpProperty.CreateOperator(operandA, ParseBinaryOperator(precedence + 1), operatorToken.value);
			}
			else
			{
				break;
			}
		}

		return operandA;
	}

	public EffectProperty Parse()
	{
		return ParseBinaryOperator();
	}

	public static void RunTest<I>(string source, I expectedResult)
	{
		EffectPropertyParser parser = new EffectPropertyParser(source, new List<string>());
		I actualResult = parser.Parse().GetValue<I>(null);

		if (!EqualityComparer<I>.Default.Equals(actualResult, expectedResult))
		{
			Debug.LogError("Failed test \'" + source + "\'");
		}
	}

	public static void RunParserTests()
	{
		RunTest<float>("1+2", 3.0f);
		RunTest<float>("3-2-1", 0.0f);
		RunTest<float>("3*2+1", 7.0f);
		RunTest<float>("1+2*3", 7.0f);
		RunTest<float>("3+2+1", 6.0f);
		
		RunTest<float>("3-(2-1)", 2.0f);
		RunTest<float>("(3-2)-1", 0.0f);
		RunTest<float>("3*(2+1)", 9.0f);
		
		RunTest<Vector3>("{0,1,0}+{2,0,0}+{0,0,3}", new Vector3(2f,1f,3f));
	}

	static EffectPropertyParser()
	{
		RunParserTests();
	}
}
