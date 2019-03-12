using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using static LevelScript.Phrasing;
using static LevelScript.Node;
using static LevelScript.Logging;
using static LevelScript.Extensions;
using static UnityEngine.MonoBehaviour;
//using Jarrah;
namespace LevelScript {
	public class Out<T> {
		public T value;
	}
	public class Parser {
		public static Code Parse (List<dynamic> tokens, TextMesh textMesh = null, Out<int> debug = null)
		{
			var tree = new Stack<dynamic> ();
			var stack = new Stack<dynamic> ();
			int line = 0;
			foreach (var token in tokens) {
				switch (token) {
				case Token.Punctuation s:
					switch (s) {
					case Token.Punctuation.Newline:
						line++;
						while (stack.Any () && !Compare(stack.Peek(), Token.Punctuation.Newline)) {
								var tok = stack.Pop ();
								//if (token.Type == TokenType.Parenthesis)
								//	throw new Exception("Mismatched parentheses");
								if (tok is Operators) {
									HandleOperatorOrKeyword (tok);
								}
							//tree.Push(tok);
						}
						tree.Push (token);
						break;
					case Token.Punctuation.Comma:
						//while (!Compare (stack.Peek (), Token.Punctuation.ParenthesisOpen))
						//	tree.Push( stack.Pop ());
						while (!Compare (stack.Peek (), Token.Punctuation.ParenthesisOpen) && !Compare (stack.Peek (), Token.Punctuation.Comma)) {
							if (stack.Peek () is Operators) {
								HandleOperatorOrKeyword ();
							} else {
								tree.Push (stack.Pop ());
							}
						}
						break;
					case Token.Punctuation.Terminate:
						tree.Push (token);
						break;
					case Token.Punctuation.ParenthesisOpen:
						stack.Push (token);
						break;
					case Token.Punctuation.SquareOpen:
						stack.Push (Token.Punctuation.ParenthesisOpen);
						tree.Push (token);
						break;
					case Token.Punctuation.SquareClose:
						while (!Compare (stack.Peek (), Token.Punctuation.ParenthesisOpen)) {
							if (stack.Peek () is Operators) {
								HandleOperatorOrKeyword ();
							} else {
								tree.Push (stack.Pop ());
							}
						}
						stack.Pop ();
						var list = new List<Node> ();
						while (!Compare (tree.Peek (), Token.Punctuation.SquareOpen)) {
//							print (tree.Peek ());
							list.Add (tree.Pop ());
						}
						tree.Pop ();
						list.Reverse ();
						tree.Push (new List (list, new DebugInfo(line)));

						break;
					case Token.Punctuation.ParenthesisClose:
						while (!Compare (stack.Peek (), Token.Punctuation.ParenthesisOpen)) {
							if (stack.Peek () is Operators) {
								HandleOperatorOrKeyword ();
							} else {
								tree.Push (stack.Pop ());
							}
						}
						stack.Pop ();
						//if (stack.Any () && Compare (stack.Peek (), Operators.Invoke))
						//	tree.Push( stack.Pop ());
						//	if (stack.Count > 0 && stack.Peek ().type == TokenType.Function)
						//		yield return stack.Pop ();
						break;
					}
					break;
				 case Operators o:
//					print (token);
					 if (Lexer.operators [o].Unary == true) {
						while (stack.Any () && stack.Peek () is Operators && Lexer.CompareOperators (o, stack.Peek ())) {
							HandleOperatorOrKeyword ();
						}
					} else {
						while (stack.Any () && stack.Peek () is Operators && Lexer.CompareOperators (o, stack.Peek ())) {
							HandleOperatorOrKeyword ();
						}						
					 }
					 stack.Push(o);
					 break;
				case CODEBLOCK c:
					tree.Push (new Code(Parse(c.code), new DebugInfo(line)));
					break;
				case WORD w:
					tree.Push (new Word (w.str, new DebugInfo(line)));
					break;
				default:
					tree.Push (new Const(token));
					break;
				}
				if(debug != null)
					debug.value++;
			}
			
			while (stack.Any())
			{
				var tok = stack.Pop();
				if (tok is Token.Punctuation && tok == Token.Punctuation.ParenthesisOpen)
					throw new Exception("Mismatched parentheses: (");
				if (tok is Token.Punctuation && tok == Token.Punctuation.ParenthesisClose)
					throw new Exception ("Mismatched parentheses: )");
				if (tok is Operators) {
					HandleOperatorOrKeyword (tok);
				}
				//tree.Push(tok);
			}
			var code = new List<Node> (tree.Count);
			while (tree.Any ()) {
				var node = tree.Pop ();
				if (node is Node) {
					code.Add ((Node)node);
				}
			}
			code.Reverse ();
//			print (show (code), Color.red);
			return new Code(code.ToArray (), new DebugInfo(line));
			//<summary> If an operator is not provided will take it from the stack </summary>
			void HandleOperatorOrKeyword (dynamic token = null)
			{
				if (token == null) {
					token = stack.Pop ();
				}
				switch (token) {
				case Operators.Define: {
						Code body = tree.Pop ().code [0];
						string [] parameters = Array.ConvertAll ((dynamic [])tree.Pop ().items.ToArray (), item => (string)item.word);
						string name = tree.Pop ().word;
						var node = new DefineMethod (name, parameters, body, new DebugInfo (line));
						tree.Push (node);
					}
					break;
				case Operators.Class: {
						Code body = tree.Pop ().code [0];
						//string [] parameters = Array.ConvertAll ((dynamic [])tree.Pop ().items.ToArray (), item => (string)item.word);
						string name = tree.Pop ().word;
						var node = new DefineClass (name, body, new DebugInfo (line));
						tree.Push (node);
					}
					break;
				case Operators.Invoke: {
						Node parameters = tree.Pop ();
						Node function = tree.Pop ();
						Node [] parameterArray;
						if (parameters is List)
							parameterArray = ((List)parameters).items.ToArray ();
						else
							parameterArray = new Node [] { parameters };
						if (function is Const) {
							switch (((Const)function).value) {
							case Token.Keywords keyword:
								switch(keyword) {
								case Token.Keywords.Return:
									tree.Push (new Return (parameterArray [0], new DebugInfo (line)));
									break;
								case Token.Keywords.Wait:
									tree.Push (new Wait (parameterArray [0], new DebugInfo (line)));
									break;
								case Token.Keywords.Start:
									tree.Push (new Start (parameterArray [0], new DebugInfo (line)));
									break;
								}
								break;
							default:
								tree.Push (new Call (function, parameterArray, new DebugInfo (line)));
								break;
							}
						} else
							tree.Push (new Call (function, parameterArray, new DebugInfo (line)));
					}
				break;
				case Operators.If: {
						Code body = tree.Pop ().code [0];   /// HACK: Nani The Fuck????
						Node condition = tree.Pop ();
						var node = new If (condition, body, new DebugInfo (line));
						tree.Push (node);
					}
				break;
				case Operators.Elif: {
						Code body = tree.Pop ().code [0];   /// HACK: Nani The Fuck????
						Node condition = tree.Pop ();
						var node = new If (condition, body, new DebugInfo (line));
						if (tree.Peek () is Token.Punctuation && tree.Peek () == Token.Punctuation.Newline)
							tree.Pop ();
						if (tree.Peek () is If parentStatement) {
							while (parentStatement.@else is If _elseif) {
								parentStatement = _elseif;
							}
							parentStatement.@else = node;
						}
					}
				break;
				case Operators.Else: {
						Code body = tree.Pop ().code [0];   /// HACK: Nani The Fuck????
						if (tree.Peek () is Token.Punctuation && tree.Peek () == Token.Punctuation.Newline)
							tree.Pop ();
						
						if(tree.Peek() is If parentStatement) {
							while(parentStatement.@else is If _elseif) {
								parentStatement = _elseif;
							}
							parentStatement.@else = body;
						}
					
					}
					break;
				case Operators.For: {
						//					print (show (tree.Peek()));
						Code body = Ensure<Code>( tree.Pop ()).code [0];   // HACK: This is concerning 
						Node list = tree.Pop ();
						Word variable = tree.Pop ();
						var node = new For (variable.word, list, body, new DebugInfo (line));
						tree.Push (node);
					}
				break;
				case Operators.While: {
						Code body = tree.Pop ().code [0];   // HACK: This is concerning 
						Node condition = tree.Pop ();
						var node = new While (condition, body, new DebugInfo (line));
						tree.Push (node);
					}
					break;
				case Operators.Index: {
						Node index = tree.Pop ();
						Node collection = tree.Pop ();
						tree.Push (new Index (collection, index, new DebugInfo (line)));
					}
					break;
				case Operators.Access: {
						string member = tree.Pop ().word;
						Node obj = tree.Pop ();
						tree.Push (new Access (obj, member, new DebugInfo (line)));
					}
					break;
				case Operators.PlusAssign:
				case Operators.MinusAssign:
				case Operators.MultiplyAssign:
				case Operators.DivideAssign:
				case Operators.Assign: {
						var right = tree.Pop ();
						var left = tree.Pop ();
						if(token != Operators.Assign)
							right = new Operate (ToNormalOperator (token), left, right);
						tree.Push (new Assign (left, right, new DebugInfo(line)));
					}
					break;
				default:
					if (Lexer.operators [token].Unary) {
						tree.Push (new Unary (token, tree.Pop (), new DebugInfo (line)));
					} else {
						var two = tree.Pop ();
						var one = tree.Pop ();
						tree.Push (new Operate (token, one, two, new DebugInfo (line)));
					}
					break;
				}
			}
		}
		public static Operators ToNormalOperator (Operators op)
		{
			switch (op) {
			case Operators.PlusAssign: return Operators.Plus;
			case Operators.MinusAssign: return Operators.Minus;
			case Operators.MultiplyAssign: return Operators.Multiply;
			case Operators.DivideAssign: return Operators.Divide;
			case Operators.Assign: return Operators.None;
			default:
				throw new Exception ();
			}
		}
		public static void log(dynamic thing)
		{
			print (thing);
		}
		public static string show (Operate node)
		{
			return ($"({show(node.LHS)} {Display(node.@operator)} {show(node.RHS)})");
		}
		public static string show (Access node)
		{
			return ($"({show (node.obj)}.{node.member})");
		}
		public static string show (Const node)
		{
			if (node.value is List<dynamic>)
				return "[ " + string.Join (", ", ((List<dynamic>)node.value).Select (t => show(t))) + " ]";
			else if(node.value is string)
				return $"\"{node.value}\"";
			else
				return node.value.ToString ();
		}
		public static string show (List<dynamic> list)
		{
		return "[ " + string.Join (", ", (list).Select (t => show (t))) + " ]";

		}
		public static string show (Call node)
		{

			return $"{ show(node.function) } @ ( { string.Join(", ", Array.ConvertAll(node.parameters, show)) } )";
		}
		public static string show (Code node)
		{
			return  string.Join ("\n ", Array.ConvertAll (node.code, show));
		}
		public static string show (List<Node> nodes)
		{
			return string.Join ("\n", nodes.Select (show));
		}
		public static string show (DefineMethod node)
		{
			return $"def {node.name } ({ string.Join(", ", node.parameters) }) {{\n {show(node.code)} \n}}";
		}
		public static string show (Assign node)
		{
			return $"{show(node.LHS)} = {show (node.RHS)}";
		}
		public static string show (If node)
		{
			return $"if {show(node.condition)} then {{ {show(node.body)} }} else {{ {show(node.@else)} }}";
		}
		public static string show (For node)
		{
			return $"for ({node.variable} in { show (node.list)}) do {{\n {show (node.body)} \n}}\n";
		}
		public static string show (Node node)
		{
			switch (node) {
			case Operate @operator:
				return show (@operator);
			case Const con:
				return show (con);
			case Word w:
				return w.word;
			case DefineMethod d:
				return show (d);
			case Call c:
				return show (c);
			case Access a:
				return show (a);
			case If i:
				return show (i);
			case Code c:
				return show (c);
			case For f:
				return show (f);
			case Assign a:
				return show (a);
			case Return r:
				return "return " + show(r._return);
			case Wait w:
				return "(wait " + show (w.obj) + ")";
			default:
				return node?.ToString () + "*";
		}
		}
		public static T Ensure<T> (Node node)
		{
			if (node is T type)
				return type;
			throw new Exception ($"Unexpected token `{show(node)}:node`, expecting {typeof(T)}");
		}
	}

}