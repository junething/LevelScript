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
using static Jarrah.Debug;
namespace LevelScript {
	public class Parser {
		public static Code Parse (List<dynamic> tokens, TextMesh textMesh = null)
		{
			var tree = new Stack<dynamic> ();
			var stack = new Stack<dynamic> ();
			int line = 0;
			foreach (var token in tokens) {
//				print($"{Display(token)} : { token }");
				switch (token) {
				case Token.Punctuation s:
					switch (s) {
					case Token.Punctuation.Newline:
						line++;
						while (stack.Any () && !Compare(stack.Peek(), Token.Punctuation.Newline)) {
								var tok = stack.Pop ();
								//if (token.Type == TokenType.Parenthesis)
								//	throw new Exception("Mismatched parentheses");
								if (tok is Token.Operators) {
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
							if (stack.Peek () is Token.Operators) {
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
							if (stack.Peek () is Token.Operators) {
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
						tree.Push (new List (list));

						break;
					case Token.Punctuation.ParenthesisClose:
						while (!Compare (stack.Peek (), Token.Punctuation.ParenthesisOpen)) {
							if (stack.Peek () is Token.Operators) {
								HandleOperatorOrKeyword ();
							} else {
								tree.Push (stack.Pop ());
							}
						}
						stack.Pop ();
						//if (stack.Any () && Compare (stack.Peek (), Token.Operators.Invoke))
						//	tree.Push( stack.Pop ());
						//	if (stack.Count > 0 && stack.Peek ().type == TokenType.Function)
						//		yield return stack.Pop ();
						break;
					}
					break;
				 case Token.Operators o:
//					print (token);
					 if (Lexer.operators [o].Unary == true) {

					 } else {
						while (stack.Any () && stack.Peek () is Token.Operators && Lexer.CompareOperators (o, stack.Peek ())) {
							HandleOperatorOrKeyword ();
						}						
					 }
					 stack.Push(o);
					 break;
				case CODEBLOCK c:
					tree.Push (new Code(Parse(c.code)));
					break;
				case WORD w:
					tree.Push (new Word (w.str));
					break;
				default:
					tree.Push (new Const(token));
					break;
				}
			 }
			
			while (stack.Any())
			{
				var tok = stack.Pop();
				if (tok is Token.Punctuation && tok == Token.Punctuation.ParenthesisOpen)
					throw new Exception("Mismatched parentheses: (");
				if (tok is Token.Punctuation && tok == Token.Punctuation.ParenthesisClose)
					throw new Exception ("Mismatched parentheses: )");
				if (tok is Token.Operators) {
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
			return new Code(code.ToArray ());
			//<summary> If an operator is not provided will take it from the stack </summary>
			void HandleOperatorOrKeyword (dynamic token = null)
			{

				if (token == null) {
					token = stack.Pop ();
				}
				switch (token) {
				case Token.Operators.Define: {
						//					print (show (tree.Pop ()));
						//					print (show (tree.Pop ()));
						//					print (show (tree.Pop ()));
						Code body = tree.Pop ().code [0];
						string [] parameters = Array.ConvertAll ((dynamic [])tree.Pop ().items.ToArray (), item => (string)item.word);
						string name = tree.Pop ().word;
						var node = new Definition (name, parameters, body, new DebugInfo (line));
						tree.Push (node);
					}
					break;
				case Token.Operators.Invoke: {
						//					print ("INVOKE");
						//Node [] parameters = Array.ConvertAll ((dynamic [])tree.Pop ().items.ToArray (), item => (Node)item);
						//print (show (tree.Pop ()));
						Node parameters = tree.Pop ();
						//					print (tree.Peek());
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
									tree.Push (new Return (parameterArray [0]));
									break;
								case Token.Keywords.Wait:
									tree.Push (new Wait (parameterArray [0]));
									break;
								case Token.Keywords.Start:
									tree.Push (new Start (parameterArray [0]));
									break;
								}
								break;
							default:
								tree.Push (new Call (function, parameterArray));
								break;
							}
						} else
							tree.Push (new Call (function, parameterArray));
					}
				break;
				case Token.Operators.If: {
						Code body = tree.Pop ().code [0];   /// HACK: Nani The Fuck????
						Node condition = tree.Pop ();
						var node = new If (condition, body);
						tree.Push (node);
					}
				break;
				case Token.Operators.Elif: {
						Code body = tree.Pop ().code [0];   /// HACK: Nani The Fuck????
						Node condition = tree.Pop ();
						var node = new If (condition, body);
						if (tree.Peek () is Token.Punctuation && tree.Peek () == Token.Punctuation.Newline)
							tree.Pop ();
						((If)tree.Peek ()).@else = node;
					}
				break;
				case Token.Operators.Else: {
						Code body = tree.Pop ().code [0];   /// HACK: Nani The Fuck????
						if (tree.Peek () is Token.Punctuation && tree.Peek () == Token.Punctuation.Newline)
							tree.Pop ();
						if(tree.Peek() is If @if) {
							if (@if.@else == null)
								@if.@else = body;
							else if (@if.@else is If elif)
								elif.@else = body;
							else
								throw new Exception ("Unexpexed else statement");
						}
					
					}
					break;
				case Token.Operators.For: {
						//					print (show (tree.Peek()));
						Code body = tree.Pop ().code [0];   // HACK: This is concerning 
						Node list = tree.Pop ();
						Word variable = tree.Pop ();
						var node = new For (variable.word, list, body);
						tree.Push (node);
					}
				break;
				case Token.Operators.While: {
						Code body = tree.Pop ().code [0];   // HACK: This is concerning 
						Node condition = tree.Pop ();
						var node = new While (condition, body);
						tree.Push (node);
					}
					break;
				case Token.Operators.Index: {
						Node index = tree.Pop ();
						Node collection = tree.Pop ();
						tree.Push (new Index (collection, index));
					}
					break;
				case Token.Operators.Access: {
						string member = tree.Pop ().word;
						Node obj = tree.Pop ();
						tree.Push (new Access (obj, member));
					}
					break;
				default:
					var two = tree.Pop ();
					var one = tree.Pop ();
//							print (show (one));
//					print (show (two));
					tree.Push (new Operator (token, one, two, new DebugInfo (line)));
					break;
				
			}
				
			}
		}
		public static void log(dynamic thing)
		{
			print (thing);
		}
		public static string show (Operator node)
		{
			return ($"({show(node.OperandOne)} {Display(node.@operator)} {show(node.OperandTwo)})");
		}
		public static string show (Access node)
		{
			return ($"({show (node.obj)}.{node.member})");
		}
		public static string show (Const node)
		{
			if (node.value is List<dynamic>)
				return "[ " + string.Join (", ", ((List<dynamic>)node.value).Select (t => show(t))) + " ]";
			else
				return node.value.ToString ();
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
		public static string show (Definition node)
		{
			return $"def {node.name } ()";
		}
		public static string show (If node)
		{
			return $"if {show(node.condition)} then {show(node.body)} else {show(node.@else)})";
		}
		public static string show (Node node)
		{
			switch (node) {
			case Operator @operator:
				return show (@operator);
			case Const con:
				return show (con);
			case Word w:
				return w.word;
			case Definition d:
				return show (d);
			case Call c:
				return show (c);
			case Access a:
				return show (a);
			case If i:
				return show (i);
			case Code c:
				return show (c);
			case Return r:
				return "return " + show(r._return);
			case Wait w:
				return "(wait " + show (w.obj) + ")";
			default:
				return node.ToString () + "*";
		}
		}
	}

}