using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour {

	private ActionSceneController sc;
	private GeneralInput input;
	private Transform camera;
	public Transform directionPivot;

	public bool canUpdatePlayerDirection = true;

	private Vector3 rawInput;
	private float inputMag;
	private Vector3 move;

	public float skidAngle;

	public bool jumpAxis;
	public bool glideAxis;
	public bool grabAxis;

	void Start ()
	{
		sc = GameObject.Find("SceneController").GetComponent<ActionSceneController>();
		input = sc.GetComponent<GeneralInput>();

		camera = (Camera.main).transform;
	}


	public void DoUpdate()
	{
		// Get the axis and jump input.

		float h = input.GetRawInput (GeneralInput.AxesNames.Horizontal);
		float v = input.GetRawInput(GeneralInput.AxesNames.Vertical);
		Vector3 moveInp = new Vector3(h, 0, v);
		Vector3 normInp = moveInp.normalized;
		inputMag = (new Vector3(moveInp.x * normInp.x, 0, moveInp.z * normInp.z)).magnitude;

		// Calculate move direction
		if (camera != null && canUpdatePlayerDirection)
		{
			if (moveInp != Vector3.zero)
			{
				//Vector3 transformedInput = Quaternion.FromToRotation(camera.up, playerGroundNormal) * (camera.rotation * moveInp);
				Vector3 transformedInput = Quaternion.FromToRotation(camera.up, Vector3.up) * (camera.rotation * moveInp);
				transformedInput.y = 0.0f;
					
				rawInput = transformedInput.normalized;
			}
			else
			{
				rawInput = Vector3.zero;
			}
		}
	}
		

	public Vector3 GetInput () 
	{
		return move;
	}

	public Vector3 GetRawInput () 
	{
		return rawInput;
	}

	public float GetInputMag()
	{
		return inputMag;
	}

	public void ForceSetInput (Vector3 dir)
	{
		// Use this if we have to force a sync with the facingDir
		rawInput = (new Vector3 (dir.x, 0, dir.z)).normalized;
	}
}
