using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralInput : MonoBehaviour {

	public enum AxesNames
	{
		Horizontal, Vertical, Jump, Grab, ChangeTailLength, Submit, Cancel, Start, DebugMode, CameraX, CameraY, MouseX, MouseY, 
		FirstPerson, DebugMoveVertical, DebugPlace, DpadHorizontal
    }

	Dictionary<int, InputAxis> axisDictionary = new Dictionary<int, InputAxis>();
	bool controllerConnected = false;

	void Start () 
	{
		foreach (AxesNames axisName in System.Enum.GetValues(typeof(AxesNames)))
		{
			axisDictionary.Add((int)axisName, new InputAxis(axisName.ToString()));
		}
	}

	public void DoUpdate () 
	{
		string[] controllerNames = Input.GetJoystickNames();
		controllerConnected = controllerNames.Length > 0 && controllerNames[0] != "";

		foreach (var item in axisDictionary) 
		{
			item.Value.DoUpdate ();
		}
	}

	public bool GetPressed (AxesNames name)
	{
		return axisDictionary[(int)name].GetPressed();
	}

	public bool GetButtonDown (AxesNames name)
	{
		return axisDictionary[(int)name].GetButtonDown();
	}

	public bool GetButtonUp (AxesNames name)
	{
		return axisDictionary[(int)name].GetButtonUp();
	}

	public float GetRawInput (AxesNames name)
	{
		return axisDictionary [(int)name].GetRawInput ();
	}

	public float GetSmoothedInput(AxesNames name)
	{
		return axisDictionary[(int)name].GetSmoothedInput();
	}
	public bool GetControllerConnected()
    {
		return controllerConnected;
    }
}
