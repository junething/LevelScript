using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
namespace LevelScript {
	namespace Library {
		public static class Math {
			public const float pi = Mathf.PI;
			public static float atan2 (float y, float x)
			{
				return Mathf.Atan2 (y, x);
			}
			public static float atan (float f)
			{
				return Mathf.Atan (f);
			}
		}
		public static class Crucial {
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
		}
	}
}