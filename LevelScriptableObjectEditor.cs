using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor (typeof (LevelScriptableObject))]
public class LevelScriptableObjectEditor : Editor {
	SerializedProperty file;
	void OnEnable ()
	{
		file = serializedObject.FindProperty ("file");
	}
	public override void OnInspectorGUI ()
	{
		DrawDefaultInspector ();
		serializedObject.Update ();
		LevelScriptableObject myScript = (LevelScriptableObject)target;
		if (myScript.codeSource == LevelScriptableObject.CodeSource.text)
			myScript.input = GUILayout.TextArea (myScript.input);
		else
			EditorGUILayout.PropertyField (file);

		serializedObject.ApplyModifiedProperties ();
	}
}

