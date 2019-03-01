using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

using static LevelScript.Token;
using static LevelScript.Phrasing;
using static LevelScript.Logging;
using static LevelScript.Extensions;
namespace LevelScript {
	public static class Lexer {
		public static List<dynamic> Lex (string input)
		{
			var tokens = Tokenize (input).ToList ();
			Print (tokens);
			tokens = Process (tokens).ToList ();
			Print (tokens);
			tokens = ExtractCodeBlocks (tokens.ToList ()).ToList (); // This also shunts the code blocks, but not the parent scope
//			Print (tokens);
			return tokens;
		}
		public static List<dynamic> ExtractCodeBlocks (List<dynamic> tokens) {
			var tokenListStack = new Stack<List<dynamic>> (); // This holds the code blocks, the top one is the current scope, when a '}' is encountered the block is finished and popped from the stack
			tokenListStack.Push (new List<dynamic> ()); // This is the global scope
			int curlies = 0;
			int codeBlockCount = 0;
			for (int i = 0; i < tokens.Count; i++) {
				if(tokens [i] is Token.Punctuation) {
					if (tokens [i] == Token.Punctuation.CurlyOpen) {
						curlies++;
						tokenListStack.Push (new List<dynamic> ()); // This scope
					} else if (tokens [i] == Token.Punctuation.CurlyClose) {
						curlies--;
						var codeBlock = tokenListStack.Pop (); // Pop off the stack, closes the scope
						CODEBLOCK codeToken = new CODEBLOCK (codeBlock);
						tokenListStack.Peek ().Add (codeToken); // Adds the scope back into the previous scope as a code token
						codeBlockCount++;
					} else {
						tokenListStack.Peek ().Add (tokens [i]);
					}
				} else {
					tokenListStack.Peek ().Add (tokens [i]);
				}
			}
			if (curlies != 0) {
				throw new Exception("Unmatched {}s");
			}
			return tokenListStack.Peek ();
		}
		public enum TokenType { Word, Number, Operator, Symbol, Regex, String, Space, Other, Pointer, None, Parenthesis, Bool, List, Bracket, Code, Function, Comma, Newline, Null, Unknown }

		public static IEnumerable<dynamic> Tokenize (string code)
		{
			TokenType type = TokenType.None;
			code = code + ' '; //Important
			var tokens = new List<dynamic> ();
			bool inString = false;
			bool inComment = false;
			bool inRegex = false;
			bool escape = false;
			int closeParenthesisToAddNextNewLine = 0;
			string token = "";
			foreach (char ch in code) {
				(bool yes, Token.Punctuation symbol) potentialSymbol = IsSymbol (ch.ToString ());

				if (escape) {
					if (escapeChars.ContainsKey (ch))
						token += escapeChars [ch];
					else
						throw new Exception ($"'\\{ch.ToString ()}' is not a valid escape character, if you meant to type '\\', type two: '\\\\'");
					escape = false;
				} else if (ch == '\n') {
					if (inComment)
						inComment = false;
					else {
						while (closeParenthesisToAddNextNewLine > 0) {
							SubmitToken (Punctuation.ParenthesisClose);
							closeParenthesisToAddNextNewLine--;
						}
						SubmitToken (Punctuation.Newline);
					}
				} else if (inComment) {
					continue;
				} if (ch == '"') {
					if (!inString) {
						SubmitToken ();
						inString = true;
					} else {
						type = TokenType.String;
						SubmitToken ();
						inString = false;
					}
				} else if (inString) {
					if (ch == '\\')
						escape = true;
					else
						token += ch;
				} else if (ch == '#') {
					SubmitToken ();
					inComment = true;
				} else if (ch == '\\') {
					if (!inRegex) {
						SubmitToken ();
						inRegex = true;
					} else {
						type = TokenType.Regex;
						SubmitToken ();
						inRegex = false;
					}
				} else if (char.IsDigit (ch) || (type == TokenType.Number && ch == '.')) {
					if (type != TokenType.Word) {
						if (type != TokenType.Number)
							SubmitToken ();
						type = TokenType.Number;
					}
					token += ch;
				} else if (char.IsWhiteSpace(ch))
					SubmitToken ();
				else if (potentialSymbol.yes) {
					SubmitToken (potentialSymbol.symbol);
				} else if (operatorPhraseInfo.ContainsKey (ch.ToString ())) {
					string op;
					if (type == TokenType.Operator)
						op = token + ch.ToString ();
					else op = ch.ToString ();
					//			  		print($"'{op}'");
					OperatorPhraseInfo operatorInfo = operatorPhraseInfo [op];
					if (operatorInfo.MoreCharsAllowed) {
						//						print ("yes");
						if (type != TokenType.Operator)
							SubmitToken ();
						token += ch.ToString ();
					} else {
						//	print ("no");
						if (type == TokenType.Operator) {
							token = op;
							SubmitToken ();
						} else {
							SubmitToken (operatorPhraseInfo [ch.ToString ()].op);
						}

					}
					type = TokenType.Operator;
				} else {
					if (type == TokenType.Operator || type == TokenType.Number)
						SubmitToken ();
					type = TokenType.Word;
					token += ch;
				}
			}
			while (closeParenthesisToAddNextNewLine > 0) {
				SubmitToken (Punctuation.ParenthesisClose);
				closeParenthesisToAddNextNewLine--;
			}
			SubmitToken (); // submit last token
			void SubmitToken (dynamic otherToken = null)
			{
//				Debug.Log (token);
				if (type == TokenType.String)
					tokens.Add (token);
				else if (token == " " || token == "") { } else if (type == TokenType.Number)
					tokens.Add (PhraseNumber (token));
				else if (type == TokenType.String)
					tokens.Add (token);
				else if (type == TokenType.String)
					tokens.Add (token);
				else if (type == TokenType.Regex)
					tokens.Add (new System.Text.RegularExpressions.Regex(token));
				else if (type == TokenType.Operator)
					tokens.Add (operatorPhraseInfo [token].op);
				else {
					switch (token) {
					case "true": tokens.Add (true); break;
					case "false": tokens.Add (false); break;
					case "def": tokens.Add (Operators.Define); break;
					case "end": tokens.Add (Punctuation.CurlyClose); break;
					case "if": tokens.Add (Operators.If); break;
					case "else": tokens.Add (Operators.Else); break;
					case "elif": tokens.Add (Operators.Elif); break;
					case "for": tokens.Add (Operators.For); break;
					case "while": tokens.Add (Operators.While); break;
					//case "return": tokens.Add (Operators.Return); break;
					//case "wait": tokens.Add (Operators.Wait); break;
					default:
						(bool isKeyword, Keywords keyword) = IsKeyword (token);
						if (isKeyword) {
							closeParenthesisToAddNextNewLine++;
							tokens.Add (keyword);
							tokens.Add (Punctuation.ParenthesisOpen);
						} else 
							tokens.Add (new WORD (token));
						break;
					}

				}
				type = TokenType.None;
				token = "";
				if (otherToken != null)
					tokens.Add (otherToken);
			}
			#region LocalPhrasingAndCheckFunctions
			(bool, Punctuation) IsSymbol (string str)
			{
				switch (str) {
				case "\n": return (true, Token.Punctuation.Newline);
				case "(": return (true, Token.Punctuation.ParenthesisOpen);
				case ")": return (true, Token.Punctuation.ParenthesisClose);
				case "{": return (true, Token.Punctuation.CurlyOpen);
				case "}": return (true, Token.Punctuation.CurlyClose);
				case "[": return (true, Token.Punctuation.SquareOpen);
				case "]": return (true, Token.Punctuation.SquareClose);
				case ",": return (true, Token.Punctuation.Comma);
				case ";": return (true, Token.Punctuation.EndStatement);
				case ":": return (true, Token.Punctuation.CurlyOpen);
				default: return (false, Token.Punctuation.None);
				}
			}
			(bool, Keywords) IsKeyword (string oper)
			{
				switch (oper) {
				case "return": return (true, Keywords.Return);
				case "await": return (true, Keywords.Wait);
				case "start": return (true, Keywords.Start);
				default: return (false, Keywords.None);
				}
			}
			#endregion
			return tokens;
		}

		 public static IEnumerable<dynamic> Process(List<dynamic> tokens)
		 {
			bool IsObject (dynamic obj)
			{
				if (obj is Punctuation && (obj == Punctuation.ParenthesisClose || obj == Punctuation.SquareClose))
					return true;
				if (obj is WORD || obj is Keywords)
					return true;
				return false;
			}
			bool IsIndexable (dynamic obj)
			{
				if (IsObject (obj) || obj is string) return true;
				return false;
			}
			bool IsOneOf (object obj, params object[] matches)
			{
				foreach (object match in matches) {
					if (match is Type type && obj.GetType() == type)
						return true;
			  		else if (obj.GetType () == match.GetType() && obj == match)
							return true;
				}
				return false;
			}
			for (int i=0; i < tokens.Count - 1; i++) {
				switch (tokens [i]) {
				case Token.Punctuation s:
					if (s == Token.Punctuation.SquareOpen && i > 0 && IsIndexable (tokens [i-1])) {
						tokens [i] = Operators.Index;
						tokens.Insert (++i, Token.Punctuation.ParenthesisOpen);
						for (int j = i; j < tokens.Count; j++) {
							if (tokens [j] is Token.Punctuation && tokens [j] == Token.Punctuation.SquareClose) {
								tokens [j] = Token.Punctuation.ParenthesisClose;
								break;
							}
						}
					}
					if (s == Token.Punctuation.ParenthesisOpen && i > 0 && IsObject (tokens [i - 1])) {
						tokens [i] = Token.Punctuation.SquareOpen;
						if (!(i > 1 && tokens [i - 2] is Operators && tokens [i - 2] == Operators.Define)) {
//							Debug.Log (8888);
							tokens.Insert (i, Operators.Invoke);

						}
						

						int parenthesis = 1;
						for (int j = i; j < tokens.Count; j++) {
							if (tokens [j] is Token.Punctuation && tokens [j] == Token.Punctuation.ParenthesisOpen) {
								parenthesis++;
							} else if (tokens [j] is Token.Punctuation && tokens [j] == Token.Punctuation.ParenthesisClose) {
								parenthesis--;
								if (parenthesis == 0) {
									tokens [j] = Token.Punctuation.SquareClose;
									i++;
									break;
								}
							}
						}
					}
					break;
				case Keywords keywords:

					break;
				case Operators op:
					if (op == Operators.If || op == Operators.While || op == Operators.Elif) {
						if (op == Operators.Elif) {
							tokens.Insert (i - 1, Token.Punctuation.CurlyClose);
							i++;
						}
						tokens.Insert (++i, Token.Punctuation.ParenthesisOpen);
						for (int j = ++i; j < tokens.Count; j++) {
							if (tokens [j] is Token.Punctuation) {
								if (tokens [j] == Token.Punctuation.CurlyOpen) {
									tokens.Insert (j, Token.Punctuation.ParenthesisClose);
									break;
								}

							}
						}
					} else if (op == Operators.For) {
						tokens[i+2]= Token.Punctuation.ParenthesisOpen;
						for (int j = ++i; j < tokens.Count; j++) {
							 if (tokens [j] is Token.Punctuation) {
								if (tokens [j] == Token.Punctuation.CurlyOpen) {
									tokens.Insert (j, Token.Punctuation.ParenthesisClose);
									i++;
									break;
								}
							}
						}
					} else if (op == Operators.Else) {

					}
					
					break;
				}
			}
			 return tokens;
		 }
		public  static bool CompareOperators(Operators op1, Operators op2)
		{
			 return operators[op1].RightAssociative ? operators [op1].Precedence < operators [op2].Precedence : operators [op1].Precedence <= operators [op2].Precedence;
		 }

		//private bool CompareOperators(string op1, string op2) => CompareOperators(operators[op1], operators[op2]);

		public static IDictionary<Operators, OperatorInfo> operators = new Dictionary<Operators, OperatorInfo> {
			// Arithmetic
			[Operators.Plus] = new OperatorInfo { Text = "+", Precedence = 3 },
			[Operators.Minus] = new OperatorInfo { Text = "-", Precedence = 3 },
			[Operators.Multiply] = new OperatorInfo { Text = "*", Precedence = 4 },
			[Operators.Divide] = new OperatorInfo { Text = "/", Precedence = 5 },
			[Operators.Modulus] = new OperatorInfo { Text = "%", Precedence = 4 },
			[Operators.Power] = new OperatorInfo { Text = "^", Precedence = 6, RightAssociative = true },
			[Operators.Negate] = new OperatorInfo { Text = "_", Precedence = 7, Unary = true },
			// Assignment
			[Operators.Assign] = new OperatorInfo { Text = "=", Precedence = 0, Type = OperatorType.Assign},
			[Operators.PlusAssign] = new OperatorInfo { Text = "+=", Precedence = 0, Type = OperatorType.Assign},
			[Operators.MinusAssign] = new OperatorInfo { Text = "-=", Precedence = 0, Type = OperatorType.Assign },
			[Operators.MultiplyAssign] = new OperatorInfo { Text = "*=", Precedence = 0 , Type = OperatorType.Assign },
			[Operators.DivideAssign] = new OperatorInfo { Text = "/=", Precedence = 0 , Type = OperatorType.Assign },
			//[Operators.PowerEquals] = new OperatorInfo { Text = "^=", Precedence = 0 , Type = OperatorType.Assign },
			// Logic
			[Operators.Equals] = new OperatorInfo { Text = "==", Precedence = 0 },
			[Operators.GreaterThan] = new OperatorInfo { Text = ">", Precedence = 2 },
			[Operators.LesserThan] = new OperatorInfo { Text = "<", Precedence = 2 },
			[Operators.GreaterThanOrEqualTo] = new OperatorInfo { Text = ">=", Precedence = 1 },
			[Operators.LesserThanOrEqualTo] = new OperatorInfo { Text = "<=", Precedence = 1 },
			[Operators.Not] = new OperatorInfo { Text = "!", Precedence = 7, Unary = true },
			// Special
			[Operators.Access] = new OperatorInfo { Text = ".", Precedence = 8, RightAssociative = false },
			[Operators.Range] = new OperatorInfo { Text = "..", Precedence = 6 },
			//["->"] = new Operator { Name = "..", Precedence = 6 },
			[Operators.Index] = new OperatorInfo { Text = "#", Precedence = 1, RightAssociative = true },
			[Operators.Invoke] = new OperatorInfo { Text = "@", Precedence = 7, RightAssociative = false },
			[Operators.Define] = new OperatorInfo { Text = "def", Precedence = 7, RightAssociative = false },
			[Operators.If] = new OperatorInfo { Text = "if", Precedence = 7, RightAssociative = false },
			[Operators.Elif] = new OperatorInfo { Text = "elif", Precedence = 7, RightAssociative = false },
			[Operators.Else] = new OperatorInfo { Text = "else", Precedence = 7, RightAssociative = false },
			[Operators.For] = new OperatorInfo { Text = "for", Precedence = 7, RightAssociative = false },
			[Operators.While] = new OperatorInfo { Text = "for", Precedence = 7, RightAssociative = false },
			[Operators.Return] = new OperatorInfo { Text = "return", Precedence = 7, RightAssociative = false },
			[Operators.Wait] = new OperatorInfo { Text = "wait", Precedence = 7, RightAssociative = false },
			//[Operators.While] = new OperatorInfo { Text = "while", Precedence = 7, RightAssociative = false },
		};
		public static IDictionary<string, OperatorPhraseInfo> operatorPhraseInfo = new Dictionary<string, OperatorPhraseInfo> {
			// Arithmetic
			["+"] = new OperatorPhraseInfo { op = Operators.Plus, MoreCharsAllowed = true },
			["-"] = new OperatorPhraseInfo { op = Operators.Minus, MoreCharsAllowed = true },
			["*"] = new OperatorPhraseInfo { op = Operators.Multiply, MoreCharsAllowed = true },
			["/"] = new OperatorPhraseInfo { op = Operators.Divide, MoreCharsAllowed = true },
			["%"] = new OperatorPhraseInfo { op = Operators.Modulus, MoreCharsAllowed = false },

			["+="] = new OperatorPhraseInfo { op = Operators.PlusAssign, MoreCharsAllowed = false },
			["-="] = new OperatorPhraseInfo { op = Operators.MinusAssign, MoreCharsAllowed = false },
			["*="] = new OperatorPhraseInfo { op = Operators.MultiplyAssign, MoreCharsAllowed = false },
			["/="] = new OperatorPhraseInfo { op = Operators.DivideAssign, MoreCharsAllowed = false },

			["="] = new OperatorPhraseInfo { op = Operators.Assign, MoreCharsAllowed = true },
			["=="] = new OperatorPhraseInfo { op = Operators.Equals, MoreCharsAllowed = false },

			["<"] = new OperatorPhraseInfo { op = Operators.LesserThan, MoreCharsAllowed = true },
			[">"] = new OperatorPhraseInfo { op = Operators.GreaterThan, MoreCharsAllowed = true },

			["<="] = new OperatorPhraseInfo { op = Operators.LesserThanOrEqualTo, MoreCharsAllowed = false },
			[">="] = new OperatorPhraseInfo { op = Operators.GreaterThanOrEqualTo, MoreCharsAllowed = false },

			["."] = new OperatorPhraseInfo { op = Operators.Access, MoreCharsAllowed = true },
			[".."] = new OperatorPhraseInfo { op = Operators.Range, MoreCharsAllowed = false },
			["@"] = new OperatorPhraseInfo { op = Operators.Invoke, MoreCharsAllowed = false },

		};
		public static IDictionary<Token.Punctuation, string> symbols = new Dictionary<Token.Punctuation, string> {
			[Token.Punctuation.ParenthesisOpen] = "(",
			[Token.Punctuation.ParenthesisClose] = ")",
			[Token.Punctuation.Newline] = "↲\n",

			[Token.Punctuation.Terminate] = "§",
			[Token.Punctuation.CurlyOpen] = "{",
			[Token.Punctuation.CurlyClose] = "}",
			[Token.Punctuation.Comma] = ",",
			[Token.Punctuation.EndStatement] = ";",
			[Token.Punctuation.SquareOpen] = "[",
			[Token.Punctuation.SquareClose] = "]",

		};
		public static IDictionary<char, string> escapeChars = new Dictionary<char, string> {
			['n'] = "\n",
			['t'] = "\t",
			['b'] = "\b",
			['\\'] = "\\",
			['"'] = "\"",
			['\''] = "\'",
		};
		public class OperatorInfo {
			public string Text { get; set; }
			public int Precedence { get; set; }
			public bool RightAssociative { get; set; }
			public bool Unary { get; set; }
			public OperatorType Type { get; set; }
		}
		public class OperatorPhraseInfo {
			public Operators op { get; set; }
			public bool MoreCharsAllowed { get; set; }
		}
		public enum OperatorType { NotSet, Assign, Check, Logic, Math }


	}
}