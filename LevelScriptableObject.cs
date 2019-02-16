using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LevelScript;
public class LevelScriptableObject : MonoBehaviour {
	public enum CodeSource { file, text }
	public CodeSource codeSource;
	[HideInInspector]
	public string input;
	[HideInInspector]
	public TextAsset file;
	Runtime runtime;
	new Camera camera;
	// Start is called before the first frame update
	void Start ()
	{
		camera = Camera.main;
		string textCode = input;
		if (codeSource == CodeSource.file) {
			name = file.name;
			textCode = file.text;
		}
		runtime = new Runtime ();
		runtime ["game_object"] = gameObject;
		runtime ["transform"] = transform;
		runtime ["spawn"] = Runtime.GetMethod ("Spawn", this);
		runtime ["rotation"] = Runtime.GetMethod<LevelScriptableObject> ("Rotation");
		Node.Code code = Parser.Parse (Lexer.Lex (textCode));
		print (Parser.show (code));
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
	public static Quaternion Rotation (float x, float y, float z) { return Quaternion.Euler (new Vector3 (x, y, z)); }
}
