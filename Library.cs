using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
	}
}