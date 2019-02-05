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
using static LevelScript.Lexer;
using static LevelScript.Token;
using System.Reflection;
namespace LevelScript {
	public class Runtime : MonoBehaviour {
		Heap heap;
		//List<Func<int, int>> callBacks;
		public void Go (Code code)
		{
//			print (code.code.Length);
			heap = new Heap ();
			//callBacks = new List<Func> ();
			//	callBacks.Add ((Func<int>)CallBack);
			heap.Enter ();
			this ["test"] = GetType ().GetMethod ("CallBack");
			Run (code);
		}
		public void CallBack (int num)
		{
			print ("It WORKS!" + num);
		}
		public void Run (Code code, bool scopeMade = false)
		{
			heap.Enter ();
			foreach (Node node in code.code) {
//				print (Parser.show (node));
				EvaluateNode (node);
			}
			heap.Exit ();
		}
		public void Run (Method method, dynamic[] parameters)
		{
			heap.Enter ();
			for (int p = 0; p < method.parameters.Length; p++)
				heap [method.parameters [p]] = parameters [p];
			foreach (Node node in method.code.code) {
//				print (Parser.show (node));
				EvaluateNode (node);
			}
			heap.Exit ();
		}
		public dynamic EvaluateNode (Node node, bool scopeMade = false)
		{
			if (!scopeMade) {
				heap.Enter ();
			}
//			print (Parser.show (node));
			switch (node) {
			case Definition definition:
//				print (": " + definition.name);
				heap [definition.name] = new Method (definition.code, definition.parameters, definition.debug);
				return null;
			case Operator operation:
				return Operate (operation.OperandOne, operation.OperandTwo, operation.@operator);
			case Call call:
//				print (Parser.show (call));
//				print (((Word)call.function).word);
				return Call (call.function, EvaluateNode (call.parameters));
			case List list:
				return EvaluateNode (list.items);
			case Const @const:
				return /*(@const.value is Const) ? EvaluateNode(@const.value) :*/ @const.value;
			case Word word:
				return heap [word.word];
			case Code code:
				Run (code);
				return null;
			case Index index:
				return EvaluateNode(index.list) [EvaluateNode(index.key)];
			case If @if:
				dynamic evaluation = EvaluateNode (@if.condition);
				//				print (evaluation);
				if (evaluation) {
					Run (@if.body);
				} else if (@if.@else != null)
					EvaluateNode (@if.@else);
				return null;
			case While @while:

				print (Parser.show (@while.condition));
				int loops = 0;
				while (EvaluateNode (@while.condition)) {
					loops++;
					Run (@while.body);
					if (loops > 100)
						throw new Exception ("Too much looping");
				}
				return null;
			case For @for:
				heap.Enter ();
				foreach (dynamic var in EvaluateNode(@for.list)) {
//					print ($"{@for.variable} = {Parser.show(var)}");
					heap [@for.variable] = EvaluateNode(var);
//					print (Parser.show (@for.body.code [0]));
					Run (@for.body);
				}
				heap.Exit ();
				return null;
			}
			throw new Exception ($"{node} has not been evaluated!");
			dynamic Operate (Node operandOne, Node operandTwo, Operators @operator)
			{
				dynamic one;
				if (Lexer.operators [@operator].Type != Lexer.OperatorType.Assign)
					one = EvaluateNode (operandOne);
				else
					one = operandOne;
				dynamic two = EvaluateNode (operandTwo);
				switch (@operator) {
				// Math
				case Operators.Plus: return one + two;
				case Operators.Minus: return one - two;
				case Operators.Multiply: return one * two;
				case Operators.Divide: return one / two;
				case Operators.Power: return Mathf.Pow (one, two);
				case Operators.Modulus: return one % two;
				case Operators.Range: return Range (one, two);
				// Check
				case Operators.Equals: return one == two;
				case Operators.LesserThan: return one < two;
				case Operators.GreaterThan: return one > two;
				case Operators.LesserThanOrEqualTo: return one <= two;
				case Operators.GreaterThanOrEqualTo: return one >= two;
				// Logic
				case Operators.And: return one && two;
				case Operators.Or: return one || two;
				//case Operators.Xor: return one ^ two;
				case Operators.Not: return !one;
				// Assign
				case Operators.Assign:
					if (one is Index) {
						Node collection = ((Index)one).list;
						int index = EvaluateNode (((Index)one).key);
//						print ($"{collection} [ {index} ]");
						EvaluateNode (collection) [index] = two;
					} else
						heap [one.word] = two;
				return two;
				// Assign
				case Operators.PlusAssign: heap [one.word] = EvaluateNode (one) + two; return Token.Punctuation.None;
				//
				case Operators.Index: return one [two];
				default:
					throw new Exception ($"{Display (@operator)} is not implemented yet.");
				}

			}
		}
		public List<dynamic> EvaluateNode (List<Node> node)
		{
			return node.ConvertAll (x => EvaluateNode(x));
		}
		public dynamic EvaluateCode (Code code)
		{
			dynamic [] nodes = Array.ConvertAll (code.code, x => EvaluateNode (x));
			foreach(dynamic node in nodes) {
//				print (node);
			}
			return nodes[0];
		}
		public dynamic EvaluateNode (Node [] nodes)
		{
			return Array.ConvertAll (nodes, x => EvaluateNode (x));
		}
		dynamic Call (Node function, dynamic [] parameters)
		{

			//dynamic func = EvaluateNode (function);
			if (function is Word) {
				string name = ((Word)function).word;
//				print ($"Calling : {name}, parameter[0] = {parameters [0]}");
				switch (name) {
				case "print":
					print (" >>>> " + parameters [0]);
					break;
				default:
					dynamic method = heap [name];
					switch (method) {
					case Method scriptedMethod:
						Run (scriptedMethod, parameters);
						break;
					case MethodInfo cSharpMethod:
						cSharpMethod.Invoke (this, parameters);
						break;
					}

					break;
				}
			}
			return null;
		}

		/*void AssignVar (string variable, dynamic value)
		{
			print ($"Set {variable} to {value}");
			foreach (var scope in TheHEAP) {
				if (scope.ContainsKey (variable)) {
					scope [variable] = value;
					return;
				}
			}
			TheHEAP [TheHEAP.Count - 1].Add (variable, value);
		}
		dynamic GetVar (Word word)
		{
			foreach (var scope in TheHEAP) {
				if (scope.ContainsKey (word.word))
					return scope [word.word];
			}
			throw new Exception ($"[404]: Varaible '{word.word}' could not be found.");
		}*/
		public dynamic this [string key] {
			get { return heap[key]; }
			set { heap [key] = value; }
		}
		public class Heap {
			List<Dictionary<string, dynamic>> scopes;
			public Heap ()
			{
				scopes = new List<Dictionary<string, dynamic>> ();
			}


			public dynamic this [string key] {
				get { return Get (key); }
				set { Set (key, value, true); }
			}
			dynamic Get (string key)
			{
				foreach (var scope in scopes) {
					if (scope.ContainsKey (key))
						return scope [key];
				}
				throw new Exception ($"[404]: Value at '{key}' could not be found.");
			}
			void Set (string key, dynamic value, bool create = true)
			{
//				print ($"Set {key} to {value}");
				foreach (var scope in scopes) {
					if (scope.ContainsKey (key)) {
						scope [key] = value;
						return;
					}
				}
				if (create) {
					scopes [scopes.Count - 1].Add (key, value);
					return;
				}
				throw new Exception ($"[404]: Value at '{key}' could not be set.");
			}
			public void Add (string key, dynamic value)
			{
				scopes [scopes.Count - 1].Add (key, value);
			}
			public void Enter ()
			{
				scopes.Add (new Dictionary<string, dynamic> ());
			}
			public void Exit ()
			{
				scopes.RemoveAt (scopes.Count - 1);
			}
		}
		List<Node> Range (int start, int end)
		{
			var rangeList = new List<Node> ();
			if (start < end) {
				for (int i = start; i < end; i++) {
					rangeList.Add (new Const (i));
				}
			} else {
				for (int i = start; i > end; i--) {
					rangeList.Add (new Const (i));
				}
			}
			return rangeList;
		}
		public class Method {
			public Code code;
			public string [] parameters;
			DebugInfo debug;
			public Method (Code code, string[] parameters, DebugInfo debug = null)
			{
				this.code = code;
				this.parameters = parameters;
				this.debug = debug;
			}

		}
	}
}