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
	public class Runtime {
		Dictionary<string, dynamic> instanceMembers;
		public int maxLoopingOrRecursion = 1000;
		Heap heap;
		public string input = null;
		public Runtime (Dictionary<string, dynamic> parentInstanceMembers = null)
		{
			instanceMembers = parentInstanceMembers ?? new Dictionary<string, dynamic> ();
			this ["math"] = typeof (Library.Math);
			this ["input"] = GetMethod ("GetUserInput", this);
			this [""] += typeof (Library.Crucial);
			Debug.Log (this["test"]);
			//AddAllMembers  ());
			heap = new Heap (this);
			heap.Enter ();
		}
		public Runtime (string input, Dictionary<string, dynamic> parentInstanceMembers = null)
		{
			instanceMembers = parentInstanceMembers ?? new Dictionary<string, dynamic> ();
			AddAllMembers (typeof (Library.Crucial));
			heap = new Heap (this);
			heap.Enter ();
			Go (Parser.Parse (Lexer.Lex (input)));
		}
		public void Input (string text)
		{
			input = text;
		}
		public async Task<string> GetUserInput (string prompt = "")
		{
			Call ("echo", new dynamic [] { prompt });
			while (input == null) {
				//	print ("K" + input + "K");
				await Task.Yield ();
			}
			print ("-----" + input + "-----");
			string text = input;
			input = null;
			return text;
		}
		public void AddAllMembers (Type type, bool enforceSnakeCase = true)
		{
			MemberInfo [] members = type.GetMembers ();
			foreach (MemberInfo member in members) {
				switch (member) {
				case MethodInfo method:
					this [method.Name.Snake (enforceSnakeCase)] = new InstanceMethodInfo (method, null);
//					Debug.Log (method.Name.Snake (enforceSnakeCase));
					break;
				case FieldInfo field:
					this [field.Name.Snake (enforceSnakeCase)] = field.GetValue (null);
					break;
				case PropertyInfo property:
					this [property.Name.Snake (enforceSnakeCase)] = property.GetValue (null);
					break;
				}
			}
		}
		public static InstanceMethodInfo GetMethod (string name, object instance)
		{
			MethodInfo method = instance.GetType ().GetMethod (name);
			if (method == null)
				throw new Exception ($"method {name} could not be found on {instance}");
			return new InstanceMethodInfo (method, instance);
		}
		public static InstanceMethodInfo GetMethod<T> (string name)
		{
			return new InstanceMethodInfo (typeof (T).GetMethod (name), null);
		}
		/*public static InstanceMethodInfo GetStaticMethod (string name, Type type)
		{
			return new InstanceMethodInfo (typeof(Type).GetMethod (name), null);
		}*/
		public async Task seconds (float seconds)
		{
			await Task.Delay (TimeSpan.FromSeconds (seconds));
		}
		public async Task frames (int frames)
		{
			await Task.Delay (TimeSpan.FromSeconds (1));
		}
		public async void Go (Code code)
		{
			await RunAsync (code, exit: false);
		}
		public async void Go (string code)
		{
			await RunAsync (Parser.Parse(Lexer.Lex(code)), exit: false);
		}
		public async Task<dynamic> GoWait (string code)
		{
			return await RunAsync (Parser.Parse (Lexer.Lex (code)), exit: false);
		}
		public async Task<dynamic> RunAsync (Code code, bool scopeMade = false, bool exit = true)
		{
			if (!scopeMade)
				heap.Enter ();
			foreach (Node node in code.code) {
				if (node.awaitable) {
					dynamic result = await EvaluateNodeAsync (node);
					if (result is ReturnValue returnValue) {
						heap.Exit ();
						return returnValue;
					}
				} else {
					dynamic result = EvaluateNode (node);
					if (result is ReturnValue returnValue) {
						heap.Exit ();
						return returnValue;
					}
				}
			}
			if (exit)
				heap.Exit ();
			return null;
		}
		public dynamic Run (Method method, dynamic [] parameters)
		{
			heap.Enter ();
			for (int p = 0; p < method.parameters.Length; p++)
				heap [method.parameters [p]] = parameters [p];
			return Run (method.code, true);
		}
		public async Task<dynamic> RunAsync (Method method, dynamic [] parameters)
		{
			heap.Enter ();
			for (int p = 0; p < method.parameters.Length; p++)
				heap [method.parameters [p]] = parameters [p];
			return await RunAsync (method.code, true);
		}
		public dynamic Run (Code code, bool scopeMade = false)
		{
			if (!scopeMade)
				heap.Enter ();
			foreach (Node node in code.code) {
				dynamic result = EvaluateNode (node);
				if (result is ReturnValue returnValue) {
					heap.Exit ();
					return returnValue;
				}
			}
			heap.Exit ();
			return null;
		}
		public class ReturnValue {
			public dynamic value;
			public ReturnValue (dynamic value)
			{
				this.value = value;
			}
		}
		public dynamic Evaluate (object obj)
		{
			switch (obj) {
			case ReturnValue returnValue:
				return (returnValue.value);
			default:
				return obj;
			}
		}
		public dynamic EvaluateAnything (object obj)
		{
			switch (obj) {
			case Node node:
				return EvaluateNode (node);
			default:
				return obj;
			}
		}
		public async Task<dynamic> EvaluateAnythingAsync (object obj)
		{
			switch (obj) {
			case Node node:
				return await EvaluateNodeAsync (node);
			default:
				return obj;
			}
		}
		public dynamic EvaluateNode (Node node, bool scopeMade = false)
		{
			//print (Parser.show(node));
			if (!scopeMade) {
				heap.Enter ();
			}
			switch (node) {
			case Definition definition:
				//				print (": " + definition.name);
				heap [definition.name] = new Method (definition.code, definition.parameters, definition.debug);
				return null;
			case Operator operation:
				return HandleOperation (operation.OperandOne, operation.OperandTwo, operation.@operator);

			case List list:
				return EvaluateNode (list.items);
			case Const @const:
				return @const.value;
			case Word word:
				return heap [word.word];
			case Index index:
				return IndexAccess (index.list, index.key);
			case Access access:
				return Access (access.obj, access.member);
			case Return _return:
				return new ReturnValue (EvaluateNode (_return._return));
			/*case Code code:
				Run (code);
				return null;*/
			case Call call:
				return Evaluate (Call (EvaluateNode (call.function), EvaluateNode (call.parameters)));
			case If @if:
				dynamic evaluation = EvaluateNode (@if.condition);
				if (evaluation) {
					return Run (@if.body);
				} else if (@if.@else != null)
					return EvaluateNode (@if.@else);
				return null;
			case While @while:

				print (Parser.show (@while.condition));
				int loops = 0;
				while (EvaluateNode (@while.condition)) {
					loops++;
					Run (@while.body);
					if (loops > maxLoopingOrRecursion)
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
			case Start start:
				if (start.asyncMethod is Call) {
					EvaluateNodeAsync (start.asyncMethod).WrapErrors ();
				}
				return null;
			case Wait wait:
				throw new Exception ($"Cannot wait in a function that has not been called with wait");
			}
			throw new Exception ($"{node} has not been evaluated!");
		}
		async Task<dynamic> EvaluateNodeAsync (Node node, bool scopeMade = false)
		{
			//print ("a: " + Parser.show(node));
			if (!scopeMade) {
				heap.Enter ();
			}
			switch (node) {
			case Operator operation:
				print (operation.OperandOne.GetType ());
								print (Parser.show (operation));
				dynamic result1 = await HandleOperationAsync (operation.OperandOne, operation.OperandTwo, operation.@operator);
				//print ("lll   " + result1);
				return result1;
			case Code code:
				return await RunAsync (code);
			case Call call:
				return Evaluate (Call (await EvaluateNodeAsync (call.function), await EvaluateNodeAsync (call.parameters)));
			case If @if:
				dynamic evaluation = EvaluateNode (@if.condition);
				if (evaluation) {
					return await RunAsync (@if.body);
				} else if (@if.@else != null) {
					return await EvaluateNodeAsync (@if.@else);
				}
				return 9;
			case While @while:
				print (Parser.show (@while.condition));
				int loops = 0;
				while (EvaluateNode (@while.condition)) {
					loops++;
					dynamic result = await RunAsync (@while.body);
					if (result is ReturnValue) return result;
					if (loops > maxLoopingOrRecursion)
						throw new Exception ("Too much looping");
				}
				return null;
			case Return _return:
				return new ReturnValue (EvaluateNodeAsync (_return._return));
			case For @for:
				heap.Enter ();
				foreach (dynamic var in EvaluateNode (@for.list)) {
					heap [@for.variable] = var;
					dynamic result = await RunAsync (@for.body);
					if (result is ReturnValue) return result;
				}
				heap.Exit ();
				return null;
			case Wait wait:
				if (wait.obj is Call asyncCall) {
					dynamic result = await CallAsync (await EvaluateNodeAsync (asyncCall.function), await EvaluateNodeAsync (asyncCall.parameters));
					return Evaluate (result);
				} else if (wait.obj is Until) {
					loops = 0;
					Node condition = ((Until)wait.obj).condition;
					while (EvaluateNode (condition)) {
						loops++;
						await Task.Yield ();
						if (loops > maxLoopingOrRecursion)
							throw new Exception ("Too much looping");
					}
				} else {
					object obj = EvaluateNode (wait.obj);
					if (obj is float || obj is int) {
						double seconds = Convert.ToDouble (obj);
						await Task.Delay (TimeSpan.FromSeconds (seconds));
					}
				}
				return null;
			case Start start:
				if (start.asyncMethod is Call) {
					if (start.asyncMethod is Call startCall) {
						StartAsync (await EvaluateNodeAsync (startCall.function), await EvaluateNodeAsync (startCall.parameters));
						return null;
					} else {
						throw new Exception ($"Cannot start {start.asyncMethod}");
					}
					//EvaluateNodeAsync (start.asyncMethod).WrapErrors ();
				}
				return null;
			case Until until:
				throw new Exception ("`until` can only be used in conjunction with `wait`");
			}
			return EvaluateNode (node);
			throw new Exception ($"{node} has not been evaluated!");
		}
		dynamic HandleOperation (Node operandOne, Node operandTwo, Operators @operator)
		{
			dynamic one = (Lexer.operators [@operator].Type != Lexer.OperatorType.Assign) ? EvaluateNode (operandOne) : operandOne;
			dynamic two = EvaluateNode (operandTwo);
			return Operate (one, two, @operator);
		}
		async Task<dynamic> HandleOperationAsync (Node operandOne, Node operandTwo, Operators @operator)
		{
			dynamic one = (Lexer.operators [@operator].Type != Lexer.OperatorType.Assign) ? await EvaluateNodeAsync (operandOne) : operandOne;
			dynamic two = await EvaluateNodeAsync (operandTwo);
			return Operate (one, two, @operator);
		}
		dynamic Operate (dynamic one, dynamic two, Operators @operator)
		{
				print ($"{one} {@operator} {two}");
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
			case Operators.Assign: return Assign (one, two);
			case Operators.PlusAssign: return Assign (one, EvaluateNode (one) + two);
			case Operators.MinusAssign: return Assign (one, EvaluateNode (one) - two);
			case Operators.MultiplyAssign: return Assign (one, EvaluateNode (one) * two);
			case Operators.DivideAssign: return Assign (one, EvaluateNode (one) / two);
			//
			case Operators.Index: return one [two];
			case Operators.Access: return Access (one, two);
			default:
				throw new Exception ($"{Display (@operator)} is not implemented yet.");
			}
		}
		dynamic Assign (dynamic one, dynamic two)
		{
			two = Evaluate (two);
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
				//				print (member);
				//				print (obj);
				//				print (two);
				member.SetValue (obj, two);
				return two;
			}
			throw new Exception ("could not assign");
		}
		dynamic IndexAccess (Node collectionNode, Node keyNode)
		{
			dynamic collection = EvaluateNode (collectionNode);
			print (Parser.show (keyNode));
			if (keyNode is Operator op && op.@operator == Operators.Range) {
				int start = EvaluateNode (op.OperandOne);
				int end = EvaluateNode (op.OperandTwo);
				if (collection is string str)
					return str.Substring (start, end);
				if (collection.GetType ().IsArray)
					return SubArray<dynamic> (collection, start, end);
				if (Library.Crucial.IsList (collection))
					return collection.GetRange (start, end);
				throw new Exception ("WHAT");
			}
			dynamic key = EvaluateNode (keyNode);
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
		dynamic Access (dynamic one, string two, bool GetValue = true)
		{
			one = EvaluateNode (one);

			// NON STATIC CLASSES
			if (one.GetType ().GetMethod (two) != null) {
				return new InstanceMethodInfo (one.GetType ().GetMethod (two), one);
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
				return new InstanceMethodInfo (one.GetMethod (two), null);
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
		public List<dynamic> EvaluateNode (List<Node> node)
		{
			return node.ConvertAll (x => Evaluate (EvaluateNode (x)));
		}
		public dynamic EvaluateNode (Node [] nodes)
		{
			return Array.ConvertAll (nodes, x => EvaluateAnything (EvaluateNode (x)));
		}
		public async Task<dynamic> EvaluateNodeAsync (Node [] nodes)
		{
			// I couldn't work out how to use aync/await in the lamada expression :(
			// return Array.ConvertAll (nodes, async x => Evaluate (await EvaluateNodeAsync (x)));
			// This will do for now
			dynamic [] values = new dynamic [nodes.Length];
			for (int n = 0; n < nodes.Length; n++) values [n] = (await EvaluateNodeAsync (nodes [n]));
			return values;
		}

		dynamic Call (object function, dynamic [] parameters)
		{
			//function = Evaluate (function); I think this isnt needeed
			switch (function) {
			case Method scriptedMethod:
				return Run (scriptedMethod, parameters);
			case MethodInfo cSharpMethod:
				throw new Exception ($"Should not be calling a MethodInfo, call InstanceMethodInfo instead'");
				return cSharpMethod.Invoke (this, parameters);
			case InstanceMethodInfo cSharpMethod:
				return cSharpMethod.methodInfo.Invoke (cSharpMethod.instance, parameters);
			}
			throw new Exception ($"Could not call '{function}'");
		}
		public dynamic Call (string function, dynamic [] parameters = null)
		{
			return Call (heap [function], parameters);
		}
//		public dynamic Call (string function, params dynamic[] parameters)
//		{
//			return Run (heap [function], parameters);
//		}
		async void StartAsync (object function, dynamic[] parameters)
		{
			await CallAsync (function, parameters);
		}
		async Task<dynamic> CallAsync (object function, dynamic [] parameters)
		{
			function = Evaluate (function);
			switch (function) {
			case Method scriptedMethod:
				return await RunAsync (scriptedMethod, parameters);
			case MethodInfo cSharpMethod:
				return cSharpMethod.Invoke (this, parameters);
			case InstanceMethodInfo cSharpMethod:
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
					Type type = cSharpMethod.methodInfo.ReturnType;
					dynamic task = cSharpMethod.methodInfo.Invoke (cSharpMethod.instance, fixedParams);
					dynamic result = await task;
					return result;
					
				} else
					return cSharpMethod.methodInfo.Invoke (cSharpMethod.instance, fixedParams);
			}
			throw new Exception ($"Could not call '{function}'");
		}

		private static bool IsAsyncMethod (MethodInfo method)
		{

			Type attType = typeof (System.Runtime.CompilerServices.AsyncStateMachineAttribute);

			// Obtain the custom attribute for the method. 
			// The value returned contains the StateMachineType property. 
			// Null is returned if the attribute isn't present for the method. 
			var attrib = (System.Runtime.CompilerServices.AsyncStateMachineAttribute)method.GetCustomAttribute (attType);

			return (attrib != null);
		}
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
			get {
				if (key == "") {
					return new AddLibary ();
				}
				else
				return instanceMembers [key];
				}
			set {
				if(key == "") {
					AddAllMembers (value);
				} else
					instanceMembers [key] = value;
			}
		}
		public class AddLibary {
			public static object operator+ (AddLibary lib, object obj)
			{
				return obj;
			}
		}
		/*public class Range {
			public int start;
			public int end;
			public Range (int start, int end)
			{
				this.start = start;
				this.end = end;
			}
		}*/
		public class Heap {
			List<Dictionary<string, dynamic>> scopes;
			Runtime runtime;
			public Heap (Runtime runtime)
			{
				this.runtime = runtime;
				scopes = new List<Dictionary<string, dynamic>> ();
			}
			public dynamic this [string key] {
				get { return Get (key); }
				set { Set (key, value); }
			}
			public dynamic this [bool setInBottomScope, string key] {
				//get { return Get (key); }
				set { Set (key, value, setInBottomScope); }
			}
			dynamic Get (string key)
			{
				foreach (var scope in scopes) {
					if (scope.ContainsKey (key))
						return scope [key];
				}
				if (runtime.instanceMembers.ContainsKey (key))
					return runtime.instanceMembers [key];
				throw new Exception ($"[404]: Value at '{key}' could not be found.");
			}
			void Set (string key, dynamic value, bool setInBottomScope = false)
			{
				if (setInBottomScope) {
					scopes [0] [key] = value;
					return;
				}

				//				print ($"Set {key} to {value}");
				foreach (var scope in scopes) {
					if (scope.ContainsKey (key)) {
						scope [key] = value;
						return;
					}
				}
				scopes [scopes.Count - 1].Add (key, value);
				return;

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
			public Method (Code code, string [] parameters, DebugInfo debug = null)
			{
				this.code = code;
				this.parameters = parameters;
				this.debug = debug;
			}

		}
	}
}