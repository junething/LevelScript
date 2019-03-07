using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
//using System.Text.RegularExpressions;
using System.Reflection;
namespace Jarrah {
	public static class Debug {
		public static void print (object obj, Color color)
		{
			UnityEngine.Debug.Log ($"<color=#{ColorUtility.ToHtmlStringRGB (color)}>{obj.ToString()}</color>");
		}
		public static void print (object obj)
		{
			UnityEngine.Debug.Log (obj);
		}

		[MenuItem ("Tools/Clear Console %&#c")] // CMD + SHIFT + C
		public static void ClearConsole ()
		{
			var assembly = Assembly.GetAssembly (typeof (SceneView));
			var type = assembly.GetType ("UnityEditor.LogEntries");
			var method = type.GetMethod ("Clear");
			method.Invoke (new object (), null);
		}
	}
	public static class Strings {
		public static string Snake (this string str, bool makeSnake)
		{
			if (!makeSnake)
				return str;
			var snake = new System.Text.StringBuilder ();
			snake.Append (char.ToLower(str [0]));
			for (int c = 1; c < str.Length; c++) {
				if (char.IsUpper (str [c]))
					snake.Append ('_');
				snake.Append (str [c]);
			}
			return snake.ToString ();
		}
		public static string Spaced (this string str)
		{
			return Regex.Replace (
			    Regex.Replace (
				  str,
				  @"(\P{Ll})(\P{Ll}\p{Ll})",
				  "$1 $2"
			    ),
			    @"(\p{Ll})(\P{Ll})",
			    "$1 $2"
			);
		}
		public static int GetNthIndex (this string s, char t, int n)
		{
			int count = 0;
			for (int i = 0; i < s.Length; i++) {
				if (s [i] == t) {
					count++;
					if (count == n) {
						return i;
					}
				}
			}
			return -1;
		}
	}
}
