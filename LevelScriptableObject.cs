using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelScript;
using Jarrah;
using System.Text.RegularExpressions;
public class LevelScriptableObject : MonoBehaviour {
	public enum CodeSource { file, text }
	public CodeSource codeSource;
	[HideInInspector]
	public string input;
	[HideInInspector]
	public TextAsset file;
	Runtime runtime;
	new Camera camera;
	string textCode;
	// Start is called before the first frame update
	void Start ()
	{
		camera = Camera.main;
		textCode = input;
		if (codeSource == CodeSource.file) {
			name = file.name;
			textCode = file.text;
		}
		runtime = new Runtime ();
		runtime ["gameobject"] = gameObject;
		runtime ["transform"] = transform;
		runtime ["spawn"] = Methods.GetMethod ("Spawn", this);
		Node.Code code = Parser.Parse (Lexer.Lex (textCode));
		print (code);
		runtime.debugOut = Error;
		runtime.Go (code);
	}

	// Update is called once per frame
	void Update ()
	{
		runtime ["wasd"] = new Vector3 (Input.GetAxis ("Horizontal"), Input.GetAxis ("Vertical")) * Time.deltaTime * 60;
		runtime ["mouse"] = camera.ScreenToWorldPoint (Input.mousePosition);
		runtime.Call ("update");
	
	}
	public void Spawn (GameObject obj, object position = null, object rotation = null)
	{
		if (position == null && rotation == null)
			Instantiate (obj, transform.position, transform.rotation);
		else if (position is Vector3 vector && rotation == null)
			Instantiate (obj, vector, transform.rotation);
		else if (position is Vector3 pos && rotation is Vector3 rot)
			Instantiate (obj, pos, Quaternion.Euler (rot));
		else if (position is Vector3 pos2 && rotation is Quaternion quat)
			Instantiate (obj, pos2, quat);
	}
	void Error (LevelScript.Node.DebugInfo debugInfo, System.Exception error)
	{
		string coloredError = Regex.Replace (error.ToString (), @"[\w\.]*:\d+", delegate (Match match) {
			return "<color=red>" + match + "</color>";
		});

		string code = textCode;
		int startLine = code.GetNthIndex ('\n', debugInfo.line - 1) + 1;
		code = code.Insert (startLine, "<color=red>");
		int endLine = code.GetNthIndex ('\n', debugInfo.line);
		if (endLine > 0)
			code = code.Insert (endLine, "</color>");
		print ($"Line:{debugInfo.line}    :     {textCode.Substring(startLine, endLine)} \n" +
			"{coloredError}");
		//print(code);

	}
}