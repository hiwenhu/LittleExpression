using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public delegate bool ExpressionCaculate(GameObject target, List<Token> exprParams, Token token);

public enum StateType {
	FUNCTION,
	DIGIT,
	STRING,
	OPERATOR,
	RPN,
	STATEMENT,
}

public class Token {
	public StateType type;
	public string content;
	public string value;
	public List<Token> subToken = new List<Token> ();

	public int GetInt()
	{
		int result = 0;
		int.TryParse(value, out result);
		return result;
	}

	public float GetFloat()
	{
		float result = 0f;
		float.TryParse(value, out result);
		return result;
	}

	public void SetValue(int i)
	{
		value = i.ToString();
	}

	public void SetValue(float f)
	{
		value = f.ToString();
	}

	public void Print() {
		if (content != null)
			Debug.Log(content);
		if (subToken.Count > 0)
		{
			Debug.Log("=================");
			for (int i = 0 ; i < subToken.Count; ++i)
			{
				Debug.Log("-------------");
				subToken[i].Print();
			}
		}
	}
}

public class Expression : QFramework.QSingleton<Expression> {

	Stack<char> stk;

	string strpattern = "[A-Za-z]+";
	Regex strregex;

	string numpattern = "-?[0-9]+";
	Regex numregex;

	string wholestrpattern = "^[A-Za-z]+$";
	Regex wholestrregex;

	string wholenumpattern = "^-?[0-9]+$";
	Regex wholenumregex;

	string operpattern = "[\\+\\-*/]+";
	Regex operRegex;

	string rpnPattern = "^[\\(\\)0-9\\-\\+\\*\\/]*$";
	Regex rpnRegex;

	Dictionary<string, ExpressionCaculate> dictExpressions;

	GameObject target;

	Expression(){
		stk = new Stack<char> ();
		strregex = new Regex(strpattern);
		numregex = new Regex(numpattern);
		operRegex = new Regex(operpattern);
		wholestrregex = new Regex(wholestrpattern);
		wholenumregex = new Regex(wholenumpattern);
		rpnRegex = new Regex(rpnPattern);
		dictExpressions = new Dictionary<string, ExpressionCaculate>();
	}

	public void RegisterExpression(string s, ExpressionCaculate e)
	{
		if (!dictExpressions.ContainsKey(s))
			dictExpressions.Add(s, e);
	}

	public void Parse(string expr)
	{
		expr = expr.Trim();
		Match match = strregex.Match(expr,5);
	    string res = match.Value;
		int index = match.Index;
		if (Check(expr))
		{
			//RPN(expr);
			Token mainToken = new Token ();
			Analyse(expr, mainToken);
			mainToken.subToken[0].Print();
			Execute(mainToken.subToken[0]);

			Debug.Log("Final result is " + mainToken.subToken[0].value);
		}
	}



	public void Execute(Token token)
	{
		if (token.type == StateType.DIGIT || token.type == StateType.OPERATOR)
		{
			token.value = token.content;
			return;
		}

		if (token.type == StateType.STATEMENT)
		{
			string result = "";
			for (int i = 0; i < token.subToken.Count; ++i)
			{
				Execute(token.subToken[i]);
				result += token.subToken[i].value;
			}
			token.value = RPN(result).ToString();
		}
		else if (token.type == StateType.FUNCTION)
		{
			List<Token> result = new List<Token> ();
			for (int i = 0; i < token.subToken.Count; ++i)
			{
				Execute(token.subToken[i]);
				result.Add(token.subToken[i]);
			}

			ExpressionCaculate ec;
			if (dictExpressions.TryGetValue(token.content, out ec))
			{
				ec(target, result, token);
			}

		}
		else if (token.type == StateType.RPN)
		{
			token.value = RPN(token.content).ToString();
		}
	}

	public bool Check(string expr)
	{
		if (expr.Contains("(") || expr.Contains(")"))
		{
			if (!CheckParenthes(expr))
			{
				return false;
			}
		}

		return true;
	}

	public bool CheckParenthes(string expr)
	{
		string left = "([{";
		string right = ")]}";
		stk.Clear();
		for (int i = 0; i < expr.Length; ++i)
		{
			if (left.Contains(expr[i].ToString()))
			{
				stk.Push(expr[i]);
			}
			else
			{
				int index = right.IndexOf(expr[i]);
				if (index == -1) continue;

				if (stk.Count == 0 || stk.Peek() != left[index])
				{
					Debug.Log("The expr contains valid right Parenthes");
					return false;
				}
				else
				{
					stk.Pop();
				}
			}
		}

		if (stk.Count > 0)
		{
			Debug.Log("The expr contains valid left Parenthes");
			return false;
		}
		Debug.Log("Test Pass!!!!");
		stk.Clear();
		return true;
	}

	string stroperator = "+-*/";

	bool IsOperator(char c)
	{		
		return stroperator.Contains(c.ToString());
	}

	bool IsOperator(string s)
	{
		return stroperator.Contains(s);
	}

	bool GreaterPriority(char c, Stack<string> s)
	{
		string oper = s.Peek();
		if (!IsOperator(oper))
			return true;
		return "+-".Contains(oper) && "*/".Contains(c.ToString());
	}

	bool IsExpression(string s)
	{
		return dictExpressions.ContainsKey(s);
	}

	char[] paramSplits = {','};

	int FindMatchPathesisIndex(string expr, int matchedIndex, char match)
	{
		stk.Clear();
		int whoIndex = 0;
		for (int i = 0; i < expr.Length; ++i)
		{
			if (expr[i] == expr[matchedIndex])
			{
				if (matchedIndex == i)
					whoIndex = stk.Count;
				stk.Push(expr[i]);
			}
			else if (expr[i] == match)
			{
				if (stk.Count == 0)
				{
					return -1;
				}
				stk.Pop();
				if (whoIndex == stk.Count)
				{
					return i;
				}
			}
		}
		return -1;
	}

	bool CheckIsParameters(string[] paramters)
	{
		if (paramters.Length <= 0)
			return false;

		for (int i = 0; i < paramters.Length; ++i)
		{
			if (!Check(paramters[i]))
				return false;
		}
		return true;
	}

	void Analyse(string expr, Token token)
	{
		if (expr.Length <= 0)
			return;

		if (expr[0] >= 'A' && expr[0] <= 'z')
		{
			Match match = strregex.Match(expr);
			if (IsExpression(match.Value))
			{
				int nextIndex = match.Index + match.Value.Length;
				if( nextIndex < expr.Length)
				{
					Token subToken = new Token();
					subToken.type = StateType.FUNCTION;
					subToken.content = match.Value;
					token.subToken.Add(subToken);

					if (expr[nextIndex] == '(')
					{
						int matchindex = FindMatchPathesisIndex(expr,nextIndex,')');
						if (matchindex == -1 || matchindex <= nextIndex)
						{
							Debug.Log("cant find match ) for " + match.Value);
						}

						string parameterstr = expr.Substring(nextIndex + 1, matchindex - nextIndex - 1);
						string []paramters = parameterstr.Split(paramSplits);
						if (CheckIsParameters(paramters))
						{
							for (int p = 0; p < paramters.Length; ++p)
							{
//								Token subSubToken = new Token();
//								subSubToken.type = StateType.STATEMENT;
//								subToken.subToken.Add(subSubToken);
//								Analyse(paramters[p],subSubToken);
								Analyse(paramters[p],subToken);
							}
						}
						else
						{
							Analyse(parameterstr,subToken);
						}

						if (matchindex + 1 < expr.Length)
						{
							if (!IsOperator(expr[matchindex + 1]))
							{
								Debug.Log(" after " + match.Value + "unexpected " + expr[matchindex + 1]);
							}
							
							string substring = expr.Substring(matchindex + 1);
							Analyse(substring, token);
						}

					}
					else if (IsOperator(expr[nextIndex]))
					{
						string nextstr = expr.Substring(nextIndex);
						Analyse(nextstr, token);
					}
					else 
					{
						Debug.Log("Wrong after " + match.Value + " unexpected " + expr[nextIndex].ToString());
					}
				}
			}
		}
		else if (expr[0] >= '0' && expr[0] <= '9')
		{
			Match match = rpnRegex.Match(expr);
			if (match.Value.Length > 0)
			{
				Token newStateToken = new Token();
				newStateToken.type = StateType.RPN;
				newStateToken.content = expr;
				token.subToken.Add(newStateToken);
			}
			else
			{
				match = numregex.Match(expr);
				if (match.Value.Length > 0)
				{
					Token subToken = new Token();
					subToken.type = StateType.STATEMENT;
					token.subToken.Add(subToken);

					Token subSubToken = new Token();
					subSubToken.type = StateType.DIGIT;
					subSubToken.content = match.Value;
					subToken.subToken.Add(subSubToken);
					int nextindex = match.Index + match.Value.Length;
					if (nextindex < expr.Length)
					{
						if (!IsOperator(expr[nextindex]))
						{
							Debug.LogError("error operator" + expr[nextindex]);		
						}
						else
						{
							string nextstr = expr.Substring(nextindex);
							Analyse(nextstr, subToken);
						}
					}
				}
			}
				
		}
		else if (IsOperator(expr[0]))
		{
			Token subToken = new Token();
			subToken.type = StateType.OPERATOR;
			subToken.content = expr[0].ToString();
			token.subToken.Add(subToken);
			if (1 < expr.Length)
			{
				string nextstr = expr.Substring(1);
				Analyse(nextstr, token);
			}
			else{
				Debug.LogError("after operator there no expression" + expr[0].ToString());
			}
		}
	}



	float RPN(string expr)
	{
		float result = 0;
		Stack<string> stack = new Stack<string>();;
		List<string> var = new List<string> ();
		stack.Push("#");


		for (int i = 0 ; i < expr.Length; )
		{
			if (expr[i] >= 'A' && expr[i] <= 'z')
			{
				Match match = strregex.Match(expr, i);
				if (match.Index < i)
				{
					Debug.Log("match str error " + i.ToString());
					return 0;
				}

				if (IsExpression(match.Value))
				{
					
				}

				var.Add(match.Value);
				i += match.Value.Length;
			}
			else if (expr[i] >= '0' && expr[i] <= '9')
			{
				Match match = numregex.Match(expr, i);
				var.Add(match.Value);
				i += match.Value.Length;
			}
			else if (IsOperator(expr[i]))
			{
				Match match = operRegex.Match(expr, i);
				if (match.Value.Length > 1)
				{
					Debug.Log("match Operator error " + i.ToString());
					return 0;
				}

				if (GreaterPriority(expr[i], stack))
				{
					stack.Push(expr[i].ToString());
				}
				else
				{
					var.Add(stack.Pop());
					stack.Push(expr[i].ToString());
				}

				++i;
			}
			else if (expr[i] == '(')
			{
				stack.Push(expr[i].ToString());
				++i;
			}
			else if (expr[i] == ')')
			{
				while(stack.Peek() != "#")
				{
					if (stack.Peek() == "(")
					{
						stack.Pop();
						break;
					}
					var.Add(stack.Pop());
				}
				++i;
			}
		}

		while(stack.Peek() != "#")
		{
			var.Add(stack.Pop());
		}

		string strresult = "";
		foreach(string s in var)
			strresult += s;
		Debug.Log("result" + strresult);

		Stack<float> calcStack = new Stack<float>();
		float floatResult = 0f;
		for (int i = 0; i < var.Count; ++i)
		{
			if (!IsOperator(var[i]))
			{
				float.TryParse(var[i],out floatResult);
				calcStack.Push(floatResult);
			}
			else
			{
				float right = calcStack.Pop();
				float left = calcStack.Pop();
				switch(var[i])
				{
				case "+":
					calcStack.Push(left + right);
					break;
				case "-":
					calcStack.Push(left - right);
					break;
				case "*":
					calcStack.Push(left * right);
					break;
				case "/":
					calcStack.Push(left / right);
					break;
				}
			}
//			if (IsExpression(var[i]))
//			{
//				ExpressionCaculate ec;
//				if (dictExpressions.TryGetValue(var[i], out ec))
//				{
//					if (ec != null)
//					{
//						
//						//ec(target, );
//					}
//				}
//			}
		}
		result = calcStack.Pop();
		Debug.Log("RPN result " + result.ToString());
		return result;
	}

}
