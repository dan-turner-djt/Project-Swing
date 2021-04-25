using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralInput : MonoBehaviour {

	Dictionary<string, InputAxis> axisDictionary = new Dictionary<string, InputAxis>();

	void Start () 
	{
		FillAxisDictionary ();
	}

	public void DoUpdate () 
	{
		foreach (var item in axisDictionary) 
		{
			item.Value.DoUpdate ();
		}
	}



	void FillAxisDictionary()
	{
		axisDictionary.Add("Horizontal" , new InputAxis ("Horizontal"));
		axisDictionary.Add("Vertical" , new InputAxis ("Vertical"));
		axisDictionary.Add("Jump" , new InputAxis ("Jump"));
		axisDictionary.Add("Grab" , new InputAxis ("Grab"));
		axisDictionary.Add("ChangeTailLength" , new InputAxis ("ChangeTailLength"));
		axisDictionary.Add("Submit" , new InputAxis ("Submit"));
		axisDictionary.Add("Cancel" , new InputAxis ("Cancel"));
		axisDictionary.Add("Start" , new InputAxis ("Start"));
	}



	public bool GetPressed (string name)
	{
		return axisDictionary[name].GetPressed();
	}

	public bool GetButtonDown (string name)
	{
		return axisDictionary[name].GetButtonDown();
	}

	public bool GetButtonUp (string name)
	{
		return axisDictionary[name].GetButtonUp();
	}

	public float GetRawInput (string name)
	{
		return axisDictionary [name].GetRawInput ();
	}
}
