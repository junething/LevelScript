using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using static LevelScript.Methods;
namespace LevelScript {
	public abstract class Node {
		public DebugInfo debug;
		public bool awaitable;
		public virtual dynamic Eval (Runtime.Heap heap)
		{
			throw new System.NotImplementedException ();
		}
		public async virtual Task<dynamic> EvalAsync (Runtime.Heap heap)
		{
			return Eval (heap);
		}
		//public virtual dynamic Eval (Runtime.Heap heap)
		//{
		//	throw new System.NotImplementedException ();
		//}
		public class Operate : Node {
			public Operators @operator;
			public Node LHS;  // Left Hand Side
			public Node RHS;  // RIght Hand Side
			public Operate (Operators op, Node one, Node two, DebugInfo debug = null)
			{
				//	Parser.log ($":{one} {op.ToString ()} {two}");
				awaitable = true;
				@operator = op;
				LHS = one;
				RHS = two;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
//				Debug.Log (@operator);
				dynamic one = (Lexer.operators [@operator].Type != Lexer.OperatorType.Assign) ? LHS.Eval (heap) : LHS;
				dynamic two = RHS.Eval (heap);
				return Methods.Operate (one, two, @operator);
			}
			public async override Task<dynamic> EvalAsync (Runtime.Heap heap)
			{
				dynamic one = (Lexer.operators [@operator].Type != Lexer.OperatorType.Assign) ? await LHS.EvalAsync (heap) : LHS;
				dynamic two = await RHS.EvalAsync (heap);
				return Methods.Operate (one, two, @operator);
			}
			public override string ToString ()
			{
				return ($"({LHS} {Logging.Display (@operator)} {RHS})");
			}
		}
		public class Until : Node {
			public Node condition;
			public Until (Node condition)
			{
				this.condition = condition;
			}

		}
		public class Unary : Node {
			public Operators _operator;
			public Node node;
			public Unary (Operators _operator, Node node, DebugInfo debugInfo = null)
			{
				this.node = node;
				this.debug = debugInfo;
				this._operator = _operator;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				dynamic operand = node.Eval (heap);
				switch (_operator) {
				case Operators.Negate:
					return -operand;
				case Operators.Not:
					return !operand;
				default:
					throw new Exception ($"`{_operator}` is not an unary operator");
				}
			}
		}
		public class Assign : Node {
			public Node LHS;
			public Node RHS;
			public Assign (Node left, Node right, DebugInfo debugInfo = null)
			{
				awaitable = true;
				LHS = left;
				RHS = right;
				debug = debugInfo;
			}
			public override async Task<dynamic> EvalAsync (Runtime.Heap heap)
			{
				dynamic right = await RHS.EvalAsync (heap);
//				Debug.Log ($"{LHS} = {right}");
				switch (LHS) {
				case Index index:
					Node collection = index.list;
					int i = index.key.Eval (heap);
					collection.Eval (heap) [index] = right;
					return 2;
				case Word word:
					heap [word.word] = right;
					return right;
				case Access access:
					//dynamic obj = access.obj.Eval (heap);
					//dynamic member = Methods.Access (access.obj, access.member, false);
					//member.SetValue (obj, right);
					return Methods.Set (access.obj, access.member, right);
				}
				throw new System.Exception ("could not assign");
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				dynamic right = this.RHS.Eval (heap);
//				Debug.Log ($"{Parser.show (LHS)} = {right}");
				switch (LHS) {
				case Index index:
					Node collection = index.list;
					int i = index.key.Eval (heap);
					collection.Eval (heap) [index] = right;
					return 2;
				case Word word:
					heap [word.word] = right;
					return right;
				case Access access:
					dynamic obj = access.obj.Eval (heap);
//					Debug.Log (access.obj.ToString ());
					dynamic member = Methods.Access (access.obj.Eval(heap), access.member, false);
					member.SetValue (obj, right);
					return right;
				}
				throw new System.Exception ("could not assign");
			}
			public override string ToString ()
			{
				return $"{LHS} = {RHS}";
			}
		}
		public class Const : Node {
			public readonly dynamic value;
			public Const (dynamic v)
			{
				value = v;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				return value;
			}
			public override string ToString ()
			{
				if (value is List<dynamic>)
					return "[ " + string.Join (", ", ((List<dynamic>)value).Select (t => t.ToString())) + " ]";
				if (value is string)
					return $"\"{value}\"";
				return value.ToString ();
			}
		}
		public class If : Node {

			public readonly Node condition;
			public readonly Code body;
			public Node @else;
			public If (Node condition, Code body, Node @else = null, DebugInfo debugInfo = null)
			{
				awaitable = true;
				this.condition = condition;
				this.body = body;
				this.@else = @else;
				debug = debugInfo;
			}
			public If (Node condition, Code body, DebugInfo debugInfo = null)
			{
				awaitable = true;
				this.condition = condition;
				this.body = body;
				debug = debugInfo;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				dynamic evaluation = condition.Eval (heap);
				if (evaluation) {
					return body.Eval (heap);
				} else if (@else != null) {
					return @else.Eval (heap);
				}
				return null;
			}
			public async override Task<dynamic> EvalAsync (Runtime.Heap heap)
			{
				dynamic evaluation = await condition.EvalAsync (heap);
				if (evaluation) {
					return await body.EvalAsync (heap);
				} else if (@else != null) {
					return await @else.EvalAsync (heap);
				}
				return null;
			}
			public override string ToString ()
			{
				return $"if {(condition)} then {{ {(body)} }} else {{ {(@else)} }}";
			}
		}
		public class Return : Node {
			public Node _return;
			public Return (Node _return, DebugInfo debug = null)
			{
				this._return = _return;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				return new Runtime.ReturnValue (_return.Eval (heap));
			}
			public async override Task<dynamic> EvalAsync (Runtime.Heap heap)
			{
				return new Runtime.ReturnValue (await _return.EvalAsync (heap));
			}
		}
		public class Wait : Node {
			public Node obj;
			public Wait (Node await, DebugInfo debugInfo = null)
			{
				awaitable = true;
				this.obj = await;
				debug = debugInfo;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				throw new Exception ("Cannot wait in non-async method, try calling with `await` or `start`");
			}
			public async override Task<dynamic> EvalAsync (Runtime.Heap heap)
			{
				int loops = 0;
				if (obj is Call asyncCall) {
					//					Debug.Log (Parser.show (asyncCall));
					dynamic result = await asyncCall.CallAsync (heap);
					return Evaluate (result);
				} else if (obj is Until) {
					loops = 0;
					Node condition = ((Until)obj).condition;
					while (condition.Eval (null)) {
						loops++;
						await Task.Yield ();
						if (loops > 500)
							throw new Exception ("Too much looping");
					}
				} else {
					//object obj = this.obj.Eval(heap);
					//if (obj is float || obj is int) {
					//	double seconds = Convert.ToDouble (obj);
					//	await Task.Delay (TimeSpan.FromSeconds (seconds));
					//}
				}
				return null;
			}
		}
		public class Start : Node {
			public Node asyncMethod;
			public Start (Node asyncMethod, DebugInfo debugInfo = null)
			{
				awaitable = true;
				this.asyncMethod = asyncMethod;
				debug = debugInfo;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				if (asyncMethod is Call call) {
					call.CallAsync (heap).WrapErrors ();
				} else {
					throw new Exception ($"Cannot start {asyncMethod}");
				}
				return new Void ();
			}
		}
		public class While : Node {
			public readonly Node condition;
			public readonly Code body;
			public While (Node condition, Code body, DebugInfo debug = null)
			{
				awaitable = true;
				this.condition = condition;
				this.body = body;
				this.debug = debug;
			}
			public override async Task<dynamic> EvalAsync (Runtime.Heap heap)
			{
				int loops = 0;
				while (condition.Eval (heap)) {
					loops++;
					dynamic result = await body.EvalAsync (heap);
					if (result is Runtime.ReturnValue) return result;
					if (loops > 500)
						throw new Exception ("Too much looping");
				}
				return null;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				//print (Parser.show (condition));
				int loops = 0;
				while (condition.Eval (heap)) {
					loops++;
					body.Run (heap);
					if (loops > 500)
						throw new Exception ("Too much looping");
				}
				return null;
			}
		}

		public class For : Node {
			public readonly string variable;
			public readonly Node list;
			public readonly Code body;
			public For (string variable, Node list, Code body, DebugInfo debugInfo = null)
			{
				awaitable = true;
				this.variable = variable;
				this.list = list;
				this.body = body;
				debug = debugInfo;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				heap.Enter ();
				foreach (dynamic var in list.Eval (heap)) {
					heap [variable] = var;
					body.Eval (heap);
				}
				heap.Exit ();
				return null;
			}
			public override async Task<dynamic> EvalAsync (Runtime.Heap heap)
			{
				heap.Enter ();
				foreach (dynamic var in list.Eval (heap)) {
					heap [variable] = var;
					dynamic result = await body.EvalAsync (heap);
					if (result is Runtime.ReturnValue) return result;
				}
				heap.Exit ();
				return null;
			}
			public override string ToString ()
			{
				return $"for ({variable} in { (list)}) do {{\n {(body)} \n}}\n";
			}
		}
		public class Access : Node {
			public readonly string member;
			public readonly Node obj;
			public Access (Node obj, string member, DebugInfo debugInfo = null)
			{
				this.obj = obj;
				this.member = member;
				debug = debugInfo;
			}
			public override dynamic Eval (Runtime.Heap heap) => Methods.Access (obj.Eval (heap), member);
			public override string ToString () => ($"({obj}.{member})");
		}
		public class DefineMethod : Node {
			public readonly string name;
			public readonly string [] parameters;
			public readonly Code code;
			public readonly DebugInfo debug;
			public DefineMethod (string name, string [] parameters, Code code, DebugInfo debug = null)
			{
				this.name = name;
				this.parameters = parameters;
				this.code = code;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				heap [name] = new Method (code, parameters, debug);
				return heap [name];
			}
			public override string ToString ()
			{
			return $"def {name } ({ string.Join (", ", parameters) }) {{\n {code} \n}}";
			}
		}
		public class DefineClass : Node {
			public readonly string name;
			public readonly Code code;
			public readonly DebugInfo debug;
			public DefineClass (string name, Code code, DebugInfo debug = null)
			{
				this.name = name;
				this.code = code;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				Debug.Log (name);
				heap [name] = new ClassDefinition (code, name, debug);
				return heap [name];
			}
		}
		public class Word : Node {
			public readonly string word;
			public Word (dynamic word, DebugInfo debug = null)
			{
				this.word = word;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				return heap [word];
			}
			public override string ToString () => word;
		}
		public class Code : Node {
			public readonly Node [] code;
			public Code (Node [] code, DebugInfo debug = null)
			{
				awaitable = true;
				this.code = code;
				debug = debug;
			}
			public Code (Node code, DebugInfo debug)
			{
				awaitable = true;
				this.code = new Node [1];
				this.code [0] = code;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				heap.Enter ();
				foreach (Node node in code) {
					dynamic result = node.Eval (heap);
					if (result is Runtime.ReturnValue returnValue) {
						heap.Exit ();
						return returnValue;
					}
				}
				heap.Exit ();
				return new Void ();
			}
			public void EvalOpen (Runtime.Heap heap)
			{
				foreach (Node node in code) {
					node.Eval (heap);
				}
			}
			public async override Task<dynamic> EvalAsync (Runtime.Heap heap)
			{
				heap.Enter ();
				foreach (Node node in code) {
					if (node.awaitable) {
						dynamic result = await node.EvalAsync (heap);
						if (result is Runtime.ReturnValue returnValue) {
							heap.Exit ();
							return returnValue;
						}
					} else {
						dynamic result = node.Eval (heap);
						if (result is Runtime.ReturnValue returnValue) {
							heap.Exit ();
							return returnValue;
						}
					}
				}
				heap.Exit ();
				return new Void ();
			}
			public override string ToString () => string.Join ("\n ",(object[])code/*Array.ConvertAll<System.Object, string> (code, ToString)*/);
		}
		public class Call : Node {
			public Node function;
			public Node [] parameters;
			public Call (Node function, Node [] parameters, DebugInfo debug = null)
			{
				awaitable = true;
				this.function = function;
				this.parameters = parameters;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				return Evaluate (Call (function.Eval (heap), parameters.Eval (heap), heap));
			}
			public async override Task<dynamic> EvalAsync (Runtime.Heap heap) // only awaits geting the method and on the parameters
			{
				return Evaluate (Call (await function.EvalAsync (heap), await parameters.EvalAsync (heap), heap));
			}
			public async Task<dynamic> CallAsync (Runtime.Heap heap) // also awaits the actaul method call
			{
				return Evaluate (await Methods.CallAsync (await function.EvalAsync (heap), await parameters.EvalAsync (heap), heap));
			}
			public override string ToString ()
			{
				return $"{ (function) } @ ( { string.Join (", ", (object[])parameters) } )";
			}

		}
		public class LsList : List<dynamic> {
			public override string ToString ()
			{
				return base.ToString ();
			}
			public LsList ()
			{

			}
		}
		public class ListConstructor : Node {
			public List<Node> items;
			public ListConstructor (List<Node> items, DebugInfo debug = null)
			{
				this.items = items;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				return items.ConvertAll (x => Evaluate (x.Eval (heap)));
			}
			public override string ToString ()
			{
				return "[ " + string.Join (", ", items.Select (t => (t))) + " ]";
			}
	}
		public class New : Node {
			public Call thing;
			public New (Call thing)
			{
				this.thing = thing;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{

				Class _class = new Class (new Runtime.Heap (heap));
				ClassDefinition classDefinition = thing.function.Eval (heap);
				classDefinition.code.EvalOpen (_class.heap);
				if (_class.heap.globals.ContainsKey (classDefinition.name) && _class.heap [classDefinition.name] is Method init) {
					init.Run (thing.parameters, _class.heap);
				}
				return _class;
			}
			public override string ToString ()
			{
				return $"new {thing}";
			}
		}
		public class Index : Node {
			public Node list;
			public Node key;
			public Index (Node list, Node key, DebugInfo debug = null)
			{
				this.list = list;
				this.key = key;
				this.debug = debug;
			}
			public override dynamic Eval (Runtime.Heap heap)
			{
				dynamic collection = list.Eval (heap);
				//print (Parser.show (keyNode));
				if (this.key is Operate op && op.@operator == Operators.Range) {
					int start = op.LHS.Eval (heap);
					int end = op.RHS.Eval (heap);
					if (collection is string str)
						return str.Substring (start, end - start);
					if (collection.GetType ().IsArray)
						return SubArray<dynamic> (collection, start, end);
					if (Library.Crucial.IsList (collection))
						return collection.GetRange (start, end - start);
					throw new Exception ("WHAT");
				}
				dynamic key = this.key.Eval (heap);
				if (key is int && key < 0)
					key = Library.Crucial.GetLength (collection) + key;
				return collection [key];
			}
			public override string ToString ()
			{
				return $"{list} [{key}]";
			}
		}
		public class DebugInfo {
			public int line;
			public DebugInfo (int line, string script = "")
			{
				this.line = line;
			}
		}

	}
}
