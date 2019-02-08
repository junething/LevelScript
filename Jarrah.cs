using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
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
		public static async void WrapErrors (this Task task)
		{
			await task;
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
}
