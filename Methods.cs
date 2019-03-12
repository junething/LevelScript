using System;
using System.Collections.Generic;
using System.Threading.Tasks;
//using UnityAsync;
using UnityEngine;
using static LevelScript.Node;
using static LevelScript.Logging;
using static Jarrah.Strings;
using static LevelScript.Token;
using static Jarrah.Debug;
using System.Reflection;

namespace LevelScript {
	public static class Methods
	{
		public static dynamic IndexAccess (Node collectionNode, Node keyNode, Runtime.Heap heap)
		{
			dynamic collection = collectionNode.Eval(heap);
			print (Parser.show (keyNode));
			if (keyNode is Operate op && op.@operator == Operators.Range) {
				int start = op.LHS.Eval(heap);
				int end = op.RHS.Eval (heap);
				if (collection is string str)
					return str.Substring (start, end);
				if (collection.GetType ().IsArray)
					return SubArray<dynamic> (collection, start, end);
				if (Library.Crucial.IsList (collection))
					return collection.GetRange (start, end);
				throw new Exception ("WHAT");
			}
			dynamic key = keyNode.Eval (heap);
			if (key is int && key < 0)
				key = Library.Crucial.GetLength (collection) + key;
			print (key);
			return collection [key];
		}
		public static T [] SubArray<T> (T [] data, int index, int length)
		{
			T [] result = new T [length];
			Array.Copy (data, index, result, 0, length);
			return result;
		}
		internal static dynamic Access (dynamic one, string two, bool GetValue = true)
		{
			// LevelScript class
			if (one is Class _class) {
				dynamic member = _class.heap [two];
				if (member is Method method)
					return new Method.Instanced (method, _class);
				return member;
			}
			// C# classes and structs
			// NON STATIC CLASSES
			if (one.GetType ().GetMethod (two) != null) {
				return new Runtime.InstanceMethodInfo (one.GetType ().GetMethod (two), one);
			}
			if (one.GetType ().GetField (two) != null) {
				if (GetValue)
					return one.GetType ().GetField (two).GetValue (one);
				return one.GetType ().GetField (two);
			}
			if (one.GetType ().GetProperty (two) != null) {
				if (GetValue)
					return one.GetType ().GetProperty (two).GetValue (one);
				return one.GetType ().GetProperty (two);
			}
			// STATIC CLASSES
			if (one.GetMethod (two) != null) {  // Static methods on static classes
				return new Runtime.InstanceMethodInfo (one.GetMethod (two), null);
			}
			if (one.GetField (two) != null) {
				if (GetValue)
					return one.GetField (two).GetValue (one);
				return one.GetField (two);
			}
			if (one.GetProperty (two) != null) {
				if (GetValue)
					return one.GetProperty (two).GetValue (one);
				return one.GetProperty (two);
			}
			throw new Exception ($"Can't find member: '{two}' on '{one}'");
		}
		internal static dynamic Set (dynamic one, string two, dynamic value, Runtime.Heap heap = null)
		{
			// LevelScript class
			if (heap == null) {
				if (one is Class _class) return _class.heap [two] = value;
				one = one is Node ? ((Node)one).Eval (heap) : one;
			}
			// C# classes and structs
			// NON STATIC CLASSES
			if (one.GetType ().GetField (two) != null) return one.GetType ().GetField (two).SetValue(one, value);
			else if (one.GetType ().GetProperty (two) != null) return one.GetType ().GetProperty (two).SetValue (one, value);
			// STATIC CLASSES
			else if (one.GetField (two) != null) one.GetField (two).SetValue (one, value);
			else if (one.GetProperty (two) != null) one.GetProperty (two).SetValue (one, value);
			else throw new Exception ($"Can't find member: '{two}' on '{one}'");
			return value;
		}
		internal static dynamic Operate (dynamic one, dynamic two, Operators @operator)
		{
			//				print ($"{one} {@operator} {two}");
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
			case Operators.Nor: return !one && !two;
			case Operators.Xor: return one ^ two;
			case Operators.Not: return !one;
			case Operators.Index: return one [two];
			case Operators.Access: return Access (one, two);
			default:
				throw new Exception ($"{Display (@operator)} is not implemented yet.");
			}
		}
		public static Runtime.InstanceMethodInfo GetMethod (string name, object instance)
		{
			MethodInfo method = instance.GetType ().GetMethod (name);
			if (method == null)
				throw new Exception ($"method {name} could not be found on {instance}");
			return new Runtime.InstanceMethodInfo  (method, instance);
		}
		public static Runtime.InstanceMethodInfo GetMethod<T> (string name)
		{
			return new Runtime.InstanceMethodInfo (typeof (T).GetMethod (name), null);
		}
		internal static List<dynamic> Range (int start, int end)
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
		public static dynamic Evaluate (object obj)
		{
			switch (obj) {
			case Runtime.ReturnValue returnValue:
				return (returnValue.value);
			default:
				return obj;
			}
		}
		public static async void WrapErrors (this Task task)
		{
			await task;
		}
		public static async void WrapErrors (this Task<dynamic> task)
		{
			await task;
		}
		public static async void WrapErrors<T> (this Task<T> task)
		{
			await task;
		}
		public static dynamic Call (object function, dynamic [] parameters, Runtime.Heap heap = null)
		{
			heap = new Runtime.Heap (heap);
			heap.Enter ();
			//function = Evaluate (function); I think this isnt needeed
			switch (function) {
			case ClassDefinition classInialize:
				Class _class = new Class (new Runtime.Heap (heap));
				classInialize.code.EvalOpen (_class.heap);
				if(_class.heap.globals.ContainsKey(classInialize.name) && _class.heap[classInialize.name] is Method init) {
					init.Run (null, _class.heap);
				}
				return _class;
			case Method scriptedMethod:
				return Run (scriptedMethod, parameters, heap);
			case Method.Instanced scriptedInstanceMethod:
				return Run (scriptedInstanceMethod.method, parameters, scriptedInstanceMethod.instance.heap);
			//case MethodInfo cSharpMethod: 
			//	throw new Exception ($"Should not be calling a MethodInfo, call InstanceMethodInfo instead'");
			//	return cSharpMethod.Invoke (this, parameters);
			case Runtime.InstanceMethodInfo cSharpMethod:
				return cSharpMethod.methodInfo.Invoke (cSharpMethod.instance, parameters);
			}
			throw new Exception ($"Could not call '{function}'");
		}
		public static dynamic[] Eval(this Node[] nodes, Runtime.Heap heap)
		{
			return Array.ConvertAll (nodes, x => x.Eval(heap));
		}
		public static dynamic EvaluateAnything (this object obj, Runtime.Heap heap)
		{
			switch (obj) {
			case Node node:
				return node.Eval(heap);
			default:
				return obj;
			}
		}
		internal static async Task<dynamic> CallAsync (object function, dynamic [] parameters, Runtime.Heap heap = null)
		{
			if (heap == null)
				heap = new Runtime.Heap ();
			function = Evaluate (function);
			switch (function) {
			case Method scriptedMethod:
				return await scriptedMethod.RunAsync(parameters, heap);
			case Runtime.InstanceMethodInfo cSharpMethod:
				ParameterInfo [] parameterInfo = cSharpMethod.methodInfo.GetParameters ();
				dynamic [] fixedParams;
				if (parameters.Length < parameterInfo.Length) {
					fixedParams = new dynamic [parameterInfo.Length];
					for (int p = 0; p < parameterInfo.Length; p++) {
						if (p < parameters.Length)
							fixedParams [p] = parameters [p];
						else if (parameterInfo [p].IsOptional)
							fixedParams [p] = parameterInfo [p].DefaultValue;
						else
							throw new Exception ("incorrecct params");
					}
				} else {
					fixedParams = parameters;
				}
				if (IsAsyncMethod (cSharpMethod.methodInfo)) {
					dynamic task = cSharpMethod.methodInfo.Invoke (cSharpMethod.instance, fixedParams);
					// Differentiates between Task and Task<T>
					if (cSharpMethod.methodInfo.ReturnType.GenericTypeArguments.Length == 1)
						return await task;
					else
						await task;
					return new Void ();

				} else
					return cSharpMethod.methodInfo.Invoke (cSharpMethod.instance, fixedParams);
			}
			throw new Exception ($"Could not call '{function}'");
		}
		private static bool IsAsyncMethod (MethodInfo method)
		{
			Type attType = typeof (System.Runtime.CompilerServices.AsyncStateMachineAttribute);
			var attrib = (System.Runtime.CompilerServices.AsyncStateMachineAttribute)method.GetCustomAttribute (attType);
			return (attrib != null);
		}
		public static dynamic Run (this Method method, dynamic [] parameters, Runtime.Heap heap)
		{
			heap.Enter ();
			for (int p = 0; p < method.parameters.Length; p++)
				heap [method.parameters [p]] = parameters [p];
			return method.code.Run(heap, true);
		}
		public static dynamic Run (this Code code, Runtime.Heap heap, bool scopeMade = false)
		{
			if (!scopeMade)
				heap.Enter ();
			foreach (Node node in code.code) {
				dynamic result = node.Eval(heap);
				if (result is Runtime.ReturnValue returnValue) {
					heap.Exit ();
					return returnValue;
				}
			}
			heap.Exit ();
			return null;
		}
		public async static Task<dynamic> RunAsync (this Method method, dynamic [] parameters, Runtime.Heap heap)
		{
			heap.Enter ();
			for (int p = 0; p < method.parameters.Length; p++)
				heap [method.parameters [p]] = parameters [p];
			return await method.code.RunAsync (heap, true);
		}
		public async static Task<dynamic> RunAsync (this Code code, Runtime.Heap heap, bool scopeMade = false)
		{
			if (!scopeMade)
				heap.Enter ();
			foreach (Node node in code.code) {
				dynamic result = await node.EvalAsync (heap);
				if (result is Runtime.ReturnValue returnValue) {
					heap.Exit ();
					return returnValue;
				}
			}
			heap.Exit ();
			return null;
		}
		public static async Task<dynamic> EvalAsync (this Node [] nodes, Runtime.Heap heap)
		{
			// I couldn't work out how to use aync/await in the lamada expression :(
			// return Array.ConvertAll (nodes, async x => Evaluate (await EvaluateNodeAsync (x)));
			// This will do for now
			dynamic [] values = new dynamic [nodes.Length];
			for (int n = 0; n < nodes.Length; n++) values [n] = (await nodes [n].EvalAsync(heap));
			return values;
		}
	}
	public class Method {
		public Code code;
		public string [] parameters;
		DebugInfo debug;
		public Method (Code code, string [] parameters, DebugInfo debug = null)
		{
			this.code = code;
			this.parameters = parameters;
			this.debug = debug;
		}
		public class Instanced {
			public Method method;
			public Class instance;
			public Instanced (Method method, Class instance)
			{
				this.method = method;
				this.instance = instance;
			}
		}
	}

	public class ClassDefinition {
		public string name;
		public Code code;
		
		public ClassDefinition(Code code, string name, DebugInfo debug = null) {
			this.code = code;
			this.name = name;
		}
	}
	public class Class {
		public Runtime.Heap heap;
		public Class (Runtime.Heap heap)
		{
			this.heap = heap;
		}
	}
	//public enum Void { start, }
	public class Void { }
}