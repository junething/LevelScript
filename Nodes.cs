using System.Collections.Generic;
namespace LevelScript
{
	public class Node {
		public bool awaitable;
		public class Operator : Node {
			public Token.Operators @operator;
			public Node OperandOne;
			public Node OperandTwo;
			public DebugInfo debug;
			public Operator (Token.Operators op, Node one, Node two, DebugInfo debug)
			{
				//	Parser.log ($":{one} {op.ToString ()} {two}");
				awaitable = true;
				@operator = op;
				OperandOne = one;
				OperandTwo = two;
				this.debug = debug;
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
			public Token.Operators @operator;
			public Node OperandOne;
			public Node OperandTwo;
		}
		public class Const : Node {
			public readonly dynamic value;
			public Const (dynamic v)
			{
				value = v;
			}
		}
		public class If : Node {

			public readonly Node condition;
			public readonly Code body;
			public Node @else;
			public If (Node condition, Code body, Node @else = null)
			{
				awaitable = true;
				this.condition = condition;
				this.body = body;
				this.@else = @else;
			}
		}
		public class Return : Node {
			public Node _return;
			public Return (Node _return)
			{
				this._return = _return;
			}
		}
		public class Wait : Node {
			public Node obj;
			public Wait (Node await)
			{
				awaitable = true;
				this.obj = await;
			}
		}
		public class Start : Node {
			public Node asyncMethod;
			public Start (Node asyncMethod)
			{
				awaitable = true;
				this.asyncMethod = asyncMethod;
			}
		}
		public class While : Node {
			public readonly Node condition;
			public readonly Code body;
			public While (Node condition, Code body)
			{
				awaitable = true;
				this.condition = condition;
				this.body = body;
			}
		}

		public class For : Node {
			public readonly string variable;
			public readonly Node list;
			public readonly Code body;
			public For (string variable, Node list, Code body)
			{
				awaitable = true;
				this.variable = variable;
				this.list = list;
				this.body = body;
			}
		}
		public class Access : Node {
			public readonly string member;
			public readonly Node obj;
			public Access (Node obj, string member)
			{
				this.obj = obj;
				this.member = member;
			}
		}
		public class Definition : Node {
			public readonly string name;
			public readonly string [] parameters;
			public readonly Code code;
			public readonly DebugInfo debug;
			public Definition (string name, string [] parameters, Code code, DebugInfo debug = null)
			{
				this.name = name;
				this.parameters = parameters;
				this.code = code;
				this.debug = debug;
			}
		}
		public class Word : Node {
			public readonly string word;
			public Word (dynamic word)
			{
				this.word = word;
			}
		}
		public class Code : Node {
			public readonly Node [] code;
			public Code (Node [] code)
			{
				awaitable = true;
				this.code = code;
			}
			public Code (Node code)
			{
				awaitable = true;
				this.code = new Node [1];
				this.code [0] = code;
			}
		}
		public class Call : Node {
			public Node function;
			public Node [] parameters;
			public Call (Node function, Node [] parameters)
			{
				awaitable = true;
				this.function = function;
				this.parameters = parameters;
			}
		}
		public class List : Node {
			public List<Node> items;
			public List (List<Node> items)
			{
				this.items = items;
			}
		}
		public class Index : Node {
			public Node list;
			public Node key;
			public Index (Node list, Node key)
			{
				this.list = list;
				this.key = key;
			}
		}
		public class DebugInfo {
			int line;
			public DebugInfo (int line, string script = "")
			{
				this.line = line;
			}
		}

	}
}
