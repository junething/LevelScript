using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace LevelScript {
	public class Token {
		public enum Operators {
			None = 0,
			//	 +      -       *        /       %        ^      - 
			Plus, Minus, Multiply, Divide, Modulus, Power, Negate,      // Math
			//     &    |   !|   !    |!&
			And, Or, Nor, Not, Xor,                                     // Logic
			//     ==          !=            <           >           <=                    >=
			Equals, NotEquals, GreaterThan, LesserThan, LesserThanOrEqualTo, GreaterThanOrEqualTo,           // Checks
			//      .      [       (
			Access, Index, Invoke,                                      // Um
			//       =        +=
			Assign, PlusAssign, MultiplyAssign, DivideAssign, MinusAssign,                                              // Assign
			//     ..
			Range,                                             // Other
			Define, If, For, While, Else, Elif, Return, Wait									// I dont like these here :( HACK
	
		};
		public enum Keywords { None, Define, Class, If, Else, Elif, For, While, Return, Wait, Start};
		public enum Punctuation {
			ParenthesisOpen, ParenthesisClose,  // (    )
			CurlyOpen, CurlyClose,              // {    }
			SquareOpen, SquareClose,            // [    ]
			Comma, Colon,                       // ,    :
			Newline, EndStatement, Terminate,   // ↲    §
			None = 0
		};

		public enum Symbol {
			Terminate,               //    §
		};
	}
	public class OPERATOR : Token {
		public readonly Operators _;
		public OPERATOR (Operators @operator)
		{
			_ = @operator;
		}
	}
	public class PUNCTUATION : Token {
		public readonly Punctuation _;
		public PUNCTUATION (Punctuation @punctuation)
		{
			_ = @punctuation;
		}
	}
	public class WORD : Token {
    	public string str { get; }
		public WORD (string word)
		{
			str = word;
		}
		//public static implicit operator string (Word word)
		//{
		//	return word.value;
		//}
	}
	public class VALUE : Token {
		public string str { get; }
		public VALUE (dynamic word)
		{
			str = word;
		}
		//public static implicit operator string (Word word)
		//{
		//	return word.value;
		//}
	}
	public class CODEBLOCK : Token {
		public List<dynamic> code { get; set; }
		public CODEBLOCK (List<dynamic> tokens)
		{
			code = tokens;
		}
	}
	public class BARRIER : Token {
	}
	/*public class Func {
		readonly string name;
		public List<dynamic> code;
		public string [] parameters;
		public Func (string n, List<dynamic> c, string [] p = null)
		{
			name = n;
			code = c;
			parameters = p;
		}
	}*/

}