using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static LevelScript.Token;
namespace LevelScript
{
	public static class Phrasing
	{
		public static dynamic PhraseNumber(string str) // Prefers int, otherwise uses float
		{
			int @int;
			float @float;
			if (int.TryParse(str, out @int))
			{
				return @int;
			}
			else if (float.TryParse(str, out @float))
			{
				return @float;
			}
			else
			{
				throw new Exception($"{str} is not a valid number!");
			}
		}

	}
	public static class Extensions
	{
		public static string Display (this string str)
		{
			return "'" + str + "'";
		}
		public static string Display(this Token.Operators op)
		{
			return op.ToString();
		}
		public static string Display(this Token.Punctuation symbol)
		{
			return symbol.ToString();
		}
		public static bool Compare (dynamic one, Token.Punctuation sym)
		{
			return (one is Token.Punctuation && one == sym);
		}
		public static bool Compare (dynamic one, Operators op)
		{
			return (one is Operators && one == op);
		}
		/*public static void Shove (this Stack<dynamic> stack, object thing)
		{
			Console.WriteLine ("works");
			if (thing is bool && (bool)thing == true)
				throw new Exception ("Caught yah bastard");
			stack.Push (thing);
		}*/
	}
	class Logging {
		//public static void print (object obj)
	//	{
	//		Debug.Log (obj);
	//	}
		public static void Print (List<dynamic> tokens)
		{
			Debug.Log (string.Join (" ", tokens.Select (t => Display (t))));
		}
		public static string Display (List<dynamic> tokens, int highlight)
		{
			tokens [highlight] = $"<color=red>{Display(tokens [highlight])}</color>";
			return string.Join (" ", tokens.Select (t => Display (t))).ToString();
		}
		public static void Print (string str, List<dynamic> tokens)
		{
			Debug.Log (str + " " + string.Join (" ", tokens.Select (t => Display (t))));
		}
		public static string Display (string str)
		{
			return str.Display ();
		}
		public static string Display (CODEBLOCK code)
		{
			return "{ " + string.Join (" ", code.code.Select (t => Display (t))) + " }";
		}
		public static string Display (dynamic thing)
		{
			return thing.ToString ();
		}
		public static string Display (Token.Operators op)
		{
			return Lexer.operators [op].Text;
		}
		public static string Display (WORD word)
		{
			return word.str;
		}
		public static string Display (Token.Punctuation symbol)
		{
			try {
				return Lexer.symbols [symbol];
			} catch (KeyNotFoundException) {
				Debug.Log ($"{symbol} not found");
				return "";
			}
		}
	}
}
