using System;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine;
using static LevelScript.Node;
using static LevelScript.Logging;
using static LevelScript.Extensions;
using static LevelScript.Token;
using static Jarrah.Debug;
using System.Reflection;
namespace LevelScript {
	public class Runtime {
		Heap heap;
		public Runtime () {
			heap = new Heap ();
			heap.Enter ();
			heap ["print"] = GetType ().GetMethod ("Print");
			//			print (heap ["print"]);
			heap ["int"] = GetType ().GetMethod ("Int");
			heap ["float"] = GetType ().GetMethod ("Float");
			heap ["str"] = GetType ().GetMethod ("Str");
			heap ["destroy"] = GetType ().GetMethod ("Destroy");
			heap ["vector"] = GetType ().GetMethod ("Vector");
		}

		public void Go (Code code)
		{
			Run (code);
		}

		public dynamic Run (Code code, bool scopeMade = false)
		{
			heap.Enter ();
			foreach (Node node in code.code) {
				dynamic result = EvaluateNode (node);
				//print (result + "]]]");
				//print (node);
				if (result is ReturnValue) {
					heap.Exit ();
					return ((ReturnValue)result);
				}
			}
			heap.Exit ();
			return null;
		}
		public dynamic Run (Method method, dynamic [] parameters)
		{
			heap.Enter ();
			for (int p = 0; p < method.parameters.Length; p++)
				heap [method.parameters [p]] = parameters [p];
			foreach (Node node in method.code.code) {
				//				print (Parser.show (node));
				dynamic result = EvaluateNode (node);
				//print (result + ";");
				if (result is ReturnValue) {
					heap.Exit ();
					return ((ReturnValue)result).value;
				}
			}
			heap.Exit ();
			return null;
		}
		public class ReturnValue {
			public dynamic value;
			public ReturnValue(dynamic value)
			{
				this.value = value;
			}
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
				return Call (EvaluateNode(call.function), EvaluateNode (call.parameters));
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
			case Access access:
				return Access (access.obj, access.member);
			case Return _return:
//				print (EvaluateNode(_return._return));
				return new ReturnValue (EvaluateNode (_return._return));
			case If @if:
				dynamic evaluation = EvaluateNode (@if.condition);
				//				print (evaluation);
				if (evaluation) {
					return Run (@if.body);
				} else if (@if.@else != null)
					return EvaluateNode (@if.@else);
				return 9;
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
					heap [@for.variable] = var;
//					print (Parser.show (@for.body.code [0]));
					Run (@for.body);
				}
				heap.Exit ();
				return null;
			}
			throw new Exception ($"{node} has not been evaluated!");
		}
		async Task<dynamic> EvaluateNodeAsync (Node node, bool scopeMade = false)
		{
			if (!scopeMade) {
				heap.Enter ();
			}
			switch (node) {

			case Call call:
				return Call (EvaluateNode (call.function), EvaluateNode (call.parameters));
			case Code code:
				Run (code);
				return null;
			case If @if:
				dynamic evaluation = EvaluateNode (@if.condition);
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
				foreach (dynamic var in EvaluateNode (@for.list)) {
					heap [@for.variable] = var;
					Run (@for.body);
				}
				heap.Exit ();
				return null;
			}
			throw new Exception ($"{node} has not been evaluated!");
		}
		dynamic Operate (Node operandOne, Node operandTwo, Operators @operator)
		{
			//				print ($".{Parser.show(operandOne)} {@operator} {Parser.show (operandTwo)}");
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
				return Assign (one, two);
			// Assign
			case Operators.PlusAssign:
				return Assign (one, EvaluateNode (one) + two);
			//
			case Operators.Index: return one [two];
			case Operators.Access: return Access (one, two);
			default:
				throw new Exception ($"{Display (@operator)} is not implemented yet.");
			}

		}
		dynamic Assign (dynamic one, dynamic two)
		{
			switch (one) {
			case Index index:
				Node collection = index.list;
				int i = EvaluateNode (index.key);
				EvaluateNode (collection) [index] = two;
				return 2;
			case Word word:
				heap [word.word] = two;
				return two;
			case Access access:
				dynamic obj = EvaluateNode (access.obj);
				dynamic member = Access (access.obj, access.member, false);
				print (member);
				print (obj);
				print (two);
				member.SetValue (obj, two);
				return two;
			}
				throw new Exception ("could not assign");
		}
		dynamic Access (dynamic one, string two, bool GetValue = true)
		{
			one = EvaluateNode (one);
//			print ($"Finding member: '{two}' on '{one}'");
			if (one.GetType ().GetMethod (two) != null) {
				return new InstanceMethodInfo (one.GetType ().GetMethod (two), one);
			}
			if (one.GetType ().GetField (two) != null) {
				if(GetValue)
					return one.GetType ().GetField (two).GetValue (one);
				return one.GetType ().GetField (two);
			} else if (one.GetType ().GetProperty (two) != null) {
				if (GetValue)
					return one.GetType ().GetProperty (two).GetValue (one);
				return one.GetType ().GetProperty (two);
			} else {
				throw new Exception ($"Can't find member: '{two}' on '{one}'");
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
		public int Int (object obj)
		{
			switch (obj) {
			case int i:
				return i;
			case float f:
				return (int)f;
			case string s:
				int outInt;
				if (int.TryParse (s, out outInt))
					return outInt;
				break;
			}
			throw new Exception (obj.ToString() + " cannot be converted to an int");
		}
		public void Print(object obj) { print (">>>" + obj); }
		public float Float (object obj)
		{
			switch (obj) {
			case int i: return i;
			case float f: return f;
			case string s:
				float outFloat;
				if (float.TryParse (s, out outFloat))
					return outFloat;
				break;
			}
			throw new Exception (obj.ToString () + " cannot be converted to a float");
		}
		public string Str (object obj) { return obj.ToString (); }
		public void Destroy (UnityEngine.Object obj) { UnityEngine.Object.Destroy (obj); }
		public Vector3 Vector (float x, float y, float z = 0) { return new Vector3 (x, y, z); }
		dynamic Call (object function, dynamic [] parameters)
		{

			//dynamic func = EvaluateNode (function);
			switch (function) {
			case Method scriptedMethod:
				return Run (scriptedMethod, parameters);
			case MethodInfo cSharpMethod:
				return cSharpMethod.Invoke (this, parameters);
			case InstanceMethodInfo cSharpMethod:
				return cSharpMethod.methodInfo.Invoke (cSharpMethod.instance, parameters);
				
			}
			throw new Exception ($"Could not call '{function}'");
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
	  	public class InstanceMethodInfo {
			public MethodInfo methodInfo;
			public object instance;
			public InstanceMethodInfo (MethodInfo methodInfo, object instance)
			{
				this.methodInfo = methodInfo;
				this.instance = instance;
			}
		}
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
		List<dynamic> Range (int start, int end)
		{
			var rangeList = new List<dynamic> ();
			if (start < end) {
				for (int i = start; i < end; i++) {
					rangeList.Add (i);
				}
			} else {
				for (int i = start; i > end; i--) {
					rangeList.Add (i);
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