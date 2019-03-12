using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
namespace LevelScript {
	namespace Library {
		public static class Math {
			public const float Pi = Mathf.PI;

			// Trig
			public static float Atan2 (float y, float x) => Mathf.Atan2 (y, x);
			public static float Atan (float f) => Mathf.Atan (f);
			public static float Cos (float f) => Mathf.Cos (f);
			public static float Sin (float f) => Mathf.Sin (f);

			public static float Round (float f) => Mathf.Round (f);
			public static float Floor (float f) => Mathf.Floor (f);
			public static float Floor (float f, float min, float max) => Mathf.Clamp (f, min, max);
			public static float Abs (float f) => Mathf.Abs (f);
			public static float Log (float f) => Mathf.Log (f);
			public static float Deg (float f) => f * Mathf.Deg2Rad;
			public static float Rad (float f) => f * Mathf.Rad2Deg;
		}
		public static class Unity {
			
			public static async Task<int> Frames (int frames)
			{
				for (int f = 0; f < frames; f++)
					await Task.Yield ();
				return 0;
			}
		}
		public static class Crucial {
			public static int test = 7;
			public static int Int (object obj)
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
				throw new Exception (obj.ToString () + " cannot be converted to an int");
			}
			public static void Print (object obj)
			{
				if (IsList (obj)) {
					Print (string.Join (", ", (List<dynamic>)obj));
				} else {
					Debug.Log (">>>" + obj);
				}
			}
			public static bool IsList (object o)
			{
				if (o == null) return false;
				return o is System.Collections.IList &&
					 o.GetType ().IsGenericType &&
					 o.GetType ().GetGenericTypeDefinition ().IsAssignableFrom (typeof (List<>));
			}
			public static float Float (object obj)
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
			public static int GetLength (dynamic collection)
			{
				if (collection is System.Collections.Generic.List<Type> list) return list.Count; // TODO check if this works
				return collection.Length;
			}

			public static string Str (object obj) { return obj.ToString (); }
			public static void Destroy (UnityEngine.Object obj) { UnityEngine.Object.Destroy (obj); }
			public static Vector3 Vector (float x, float y, float z = 0) { return new Vector3 (x, y, z); }

			public static async Task Seconds (float seconds)
			{
				await Task.Delay (TimeSpan.FromSeconds (Convert.ToDouble (seconds)));
			}
			public static async Task<int> Miliseconds (float miliseconds)
			{
				await Task.Delay (TimeSpan.FromMilliseconds (Convert.ToDouble (miliseconds)));
				return 0;
			}
		}
	}
}