using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour {

	private Transform camera;
	private Transform playerTransform;
	public Transform directionPivot;

	public bool canUpdatePlayerDirection = true;

	private Vector3 rawInput;
	private float inputMag;
	private Vector3 smoothedInput = Vector3.zero;
	private Vector3 lastInput = Vector3.zero;
	private Vector3 move;
	private float inputAngle;
	public Vector3 progressiveInput;
	bool isSkidding;

	public AnimationCurve InputLerpingRateOverSpeed;
	public AnimationCurve UtopiaInputLerpingRateOverSpeed;
	public Vector3 UtopiaInput { get; set; }
	public float UtopiaIntensity;
	public float UtopiaInitialInputLerpSpeed;
	public float UtopiaLerpingSpeed { get; set; }
	public float skidAngle;
	float InitialInputMag;
	float InitialLerpedInput;

	public bool jumpAxis;
	public bool glideAxis;
	public bool grabAxis;

	void Start ()
	{
		camera = (Camera.main).transform;
		playerTransform = this.transform;
	}


	public void DoUpdate(float deltaTime, Vector3 playerVelocity, float playerMaxSpeed, Vector3 playerGroundNormal)
	{
		//move = Vector3.zero;
		// Get curve position

		UtopiaLerpingSpeed = UtopiaInputLerpingRateOverSpeed.Evaluate((playerVelocity.sqrMagnitude / playerMaxSpeed) / playerMaxSpeed);
		//UtopiaLerpingSpeed = 50;

		// Get the axis and jump input.

		float h = Input.GetAxisRaw("Horizontal");
		float v = Input.GetAxisRaw("Vertical");
		Vector3 moveInp = new Vector3(h, 0, v);
		Vector3 normInp = moveInp.normalized;
		inputMag = (new Vector3(moveInp.x * normInp.x, 0, moveInp.z * normInp.z)).magnitude;

		// calculate move direction
		if (camera != null && canUpdatePlayerDirection)
		{
			if (moveInp != Vector3.zero)
			{
				lastInput = smoothedInput;

				//Vector3 transformedInput = Quaternion.FromToRotation(camera.up, playerGroundNormal) * (camera.rotation * moveInp);
				Vector3 transformedInput = Quaternion.FromToRotation(camera.up, Vector3.up) * (camera.rotation * moveInp);
				transformedInput.y = 0.0f;

				if (Vector3.ProjectOnPlane (playerVelocity, playerGroundNormal).magnitude < 1.5f) 
				{
					progressiveInput = transformedInput.normalized;
				} 
				else 
				{
					//progressiveInput = Vector3.Lerp (progressiveInput.normalized, transformedInput.normalized, 12 * deltaTime).normalized;
					progressiveInput = ExtVector3.CustomLerpAngleFromVector (progressiveInput.normalized, transformedInput.normalized, 12, deltaTime, Vector3.up, 0.6f).normalized;
				}
					
				rawInput = transformedInput.normalized;
				smoothedInput = progressiveInput.normalized;

				//Debug.Log (progressiveInput);
			}
			else
			{
				rawInput = Vector3.zero;
				smoothedInput =  Vector3.zero;
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

	public Vector3 GetSmoothedInput () 
	{
		return smoothedInput;
	}

	public Vector3 GetLastInput () 
	{
		return smoothedInput;
	}


	public void ForceSetInput (Vector3 dir)
	{
		//use this if we have to force a sync with the facingDir
		rawInput = (new Vector3 (dir.x, 0, dir.z)).normalized;
		smoothedInput = (new Vector3 (dir.x, 0, dir.z)).normalized;
		progressiveInput = (new Vector3 (dir.x, 0, dir.z)).normalized;
	}
}
