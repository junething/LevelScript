using System;
using System.Collections.Generic;
using System.Threading.Tasks;
//using UnityAsync;
using UnityEngine;
using static LevelScript.Node;
using static Jarrah.Strings;
using System.Reflection;
using static LevelScript.Methods;
namespace LevelScript {
	public class Runtime {
		public int maxLoopingOrRecursion = 1000;
		Heap heap;
		public string input;
		public Action<DebugInfo, Exception> debugOut;
		public Runtime ()
		{
			//globals = parentInstanceMembers ?? new Dictionary<string, dynamic> ();
			heap = new Heap (this);
			this ["math"] = typeof (Library.Math);
			this ["input"] = GetMethod ("GetUserInput", this);
			this [""] += typeof (Library.Crucial);
			Debug.Log (this["test"]);
			//AddAllMembers  ());
		}
		public Runtime (string input, Dictionary<string, dynamic> parentInstanceMembers = null)
		{
			//globals = parentInstanceMembers ?? new Dictionary<string, dynamic> ();
			AddAllMembers (typeof (Library.Crucial));
			heap = new Heap (this);
			Go (Parser.Parse (Lexer.Lex (input)));
		}
		public async Task<string> GetUserInput (string prompt = "")
		{
			Call ("echo", new dynamic [] { prompt });
			while (input == null) {
				await Task.Yield ();
			}
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
		public async void Go (Code code) => await RunAsync (code, exit: false);
		public async void Go (string code) => await RunAsync (Parser.Parse (Lexer.Lex (code)));
		public async Task<dynamic> GoWait (string code) => await RunAsync (Parser.Parse (Lexer.Lex (code)), exit: false);
		public async Task<dynamic> RunAsync (Code code, bool scopeMade = false, bool exit = true)
		{
			Node.DebugInfo debug;
			//if (!scopeMade)
			//	heap.Enter ();
			foreach (Node node in code.code) {
				debug = node.debug;
				try {
					if (node.awaitable) {
						dynamic result = await node.EvalAsync (heap);
						if (result is ReturnValue returnValue) {
							heap.Exit ();
							return returnValue;
						}
					} else {
						dynamic result = node.Eval (heap);
						if (result is ReturnValue returnValue) {
							heap.Exit ();
							return returnValue;
						}
					}
				} catch (Exception e) {
					debugOut?.Invoke (debug, e);
					return null;
					
				}
			}
//			if (exit)
//				heap.Exit ();
			return null;
		}

		public async Task<dynamic> RunAsync (Method method, dynamic [] parameters)
		{
			heap.Enter ();
			for (int p = 0; p < method.parameters.Length; p++)
				heap [method.parameters [p]] = parameters [p];
			return await RunAsync (method.code, true);
		}

		public class ReturnValue {
			public dynamic value;
			public ReturnValue (dynamic value)
			{
				this.value = value;
			}
		}
		dynamic Assign (dynamic one, dynamic two)
		{
			two = Evaluate (two);
			switch (one) {
			case Index index:
				Node collection = index.list;
				int i = index.key.Eval(heap);
				collection.Eval(heap) [index] = two;
				return 2;
			case Word word:
				heap [word.word] = two;
				return two;
			case Access access:
				dynamic obj = access.obj.Eval(heap);
				dynamic member = Access (access.obj, access.member, false);
				member.SetValue (obj, two);
				return two;
			}
			throw new Exception ("could not assign");
		}

		//		public dynamic Call (string function, params dynamic[] parameters)
		//		{
		//			return Run (heap [function], parameters);
		//		}
		private async void StartAsync (object function, dynamic[] parameters)
		{
			await CallAsync (function, parameters);
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
			get => key == "" ? (dynamic)new Nothing () : heap.globals [key];
			set {
				if (key == "") {
					AddAllMembers (value);
				} else {
					if (heap.globals.ContainsKey (key))
						heap.globals [key] = value;
					else
						heap.globals.Add (key, value);
				}
			}
		}
		public dynamic Call (string function, dynamic [] parameters = null)
		{
			return Methods.Call (heap [function], parameters);
		}
		public class Nothing {
			public static object operator+ (Nothing nothing, object other) { return other; }
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
			public Dictionary<string, dynamic> globals;
			List<Dictionary<string, dynamic>> scopes;
			public Heap (Runtime runtime) // Is this one even nessisary???
			{
				globals = runtime?.heap != null ? runtime.heap.globals : new Dictionary<string, dynamic> ();
				scopes = new List<Dictionary<string, dynamic>> ();
			}
			public Heap (Heap oldHeap)
			{
				globals = oldHeap != null ? oldHeap.globals : new Dictionary<string, dynamic> ();
				scopes = new List<Dictionary<string, dynamic>> ();
			}
			public Heap () // Will be used in static method calls? If I implemnt them??
			{
				globals = new Dictionary<string, dynamic> ();
				scopes = new List<Dictionary<string, dynamic>> ();
			}
			public dynamic this [string key] {
				get => Get (key);
				set => Set (key, value);
			}
			dynamic Get (string key)
			{
				foreach (var scope in scopes) {
					if (scope.ContainsKey (key))
						return scope [key];
				}
				if (globals != null && globals.ContainsKey (key))
					return globals [key];
				throw new Exception ($"[404]: Value at '{key}' could not be found.");
			}
			void Set (string key, dynamic value)
			{
				if (globals.ContainsKey (key)) {
					globals [key] = value;
					return;
				}
				if (scopes.Count == 0) {
					globals.Add (key, value);
					return;
				}
				foreach (var scope in scopes) {
					if (scope.ContainsKey (key)) {
						scope [key] = value;
						return;
					}
				}
				scopes [scopes.Count - 1].Add (key, value);
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

	}
}