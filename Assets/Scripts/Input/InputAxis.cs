using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputAxis {

	string name;
	float currentInput;
	bool pressed;
	bool buttonDown;
	bool buttonUp;

	public InputAxis (string name)
	{
		this.name = name;

		currentInput = 0;
		pressed = false;
		buttonDown = false;
		buttonUp = false;
	}


	public void DoUpdate()
	{
		buttonDown = false;
		buttonUp = false;

		currentInput = Input.GetAxisRaw (name);

		if (currentInput != 0) 
		{
			if (!pressed) 
			{
				buttonDown = true;
			}


			pressed = true;
		} 
		else 
		{
			if (pressed) 
			{
				buttonUp = true;
			}

			pressed = false;
		}
	}


	public bool GetPressed ()
	{
		return pressed;
	}

	public bool GetButtonDown ()
	{
		return buttonDown;
	}

	public bool GetButtonUp ()
	{
		return buttonUp;
	}

	public float GetRawInput ()
	{
		return currentInput;
	}
}
