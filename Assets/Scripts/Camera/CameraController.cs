using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq; 

public class CameraController : Updatable 
{
	private GeneralInput input;

	public LayerMask collisionLayers;

	public float sensitivity = 10f;
	public Transform player;
	PlayerPhysicsController ppc;
	public Transform followPosition;
	public Transform cameraTransform;
	public Transform cameraHolder;

	float defaultPitch = 10;
	float pitch;
	float yMax = 80f;
	float defaultDistance = 5;
	float manualTurnSpeed = 100;
	float autoYTurnSpeed = 7;

	public bool inFirstPerson;

	Vector3 savedForwardDirection;


	public enum CameraControl
    {
		Mouse, Joypad
    }

	public enum CameraAuto
    {
		On, Off
    }

	public CameraControl cameraControlSetting = CameraControl.Joypad;
	public CameraAuto cameraAutoSetting = CameraAuto.On;

	void Awake ()
	{
		base.DoAwake ();
	}

	void Start () 
	{
		base.lateUpdatable = true;
		base.DoStart ();

		input = sc.GetComponent<GeneralInput>();
		sc.AddCameraToList (gameObject);

		player = GameObject.Find ("PlayerController").transform;
		followPosition = GameObject.Find ("CameraFollowPos").transform;

		ppc = player.GetComponent<PlayerPhysicsController>();

		cameraHolder.position = followPosition.position;
		pitch = defaultPitch;
		transform.position = followPosition.position;
		transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, transform.localEulerAngles.z);
		cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, 0.5f, cameraTransform.localPosition.z);

	}
		

	public override void DoUpdate() 
	{
		
		base.DoUpdate ();

	}


	public override void DoFinalUpdate ()
	{
		Vector3 playerVelocity = ppc.velocity;

		if (inFirstPerson && playerVelocity.magnitude > 0)
        {
			LeaveFirstPerson();
        }

		float firstPersonInp = input.GetRawInput(GeneralInput.AxesNames.FirstPerson);
		if (firstPersonInp != 0)
        {
			if (inFirstPerson && firstPersonInp < 0)
            {
				LeaveFirstPerson();
            }
			else if (!inFirstPerson && firstPersonInp > 0 && playerVelocity.magnitude == 0)
            {
				GoIntoFirstPerson();
            }
        }


		if (inFirstPerson)
        {
			DoFirstPerson();
        }
		else
        {
			DoNormalBehaviour();
        }
	}

	public void DoNormalBehaviour ()
    {
		cameraHolder.position = followPosition.position;
		transform.position = followPosition.position;

		Vector3 playerVelocity = ppc.velocity;

		if (cameraControlSetting is CameraControl.Mouse || !input.GetControllerConnected())
		{
			float yaw = input.GetRawInput(GeneralInput.AxesNames.MouseX) * sensitivity;
			//player.Rotate(0, yaw, 0);
			cameraHolder.Rotate(0, yaw, 0);


			transform.position = followPosition.position;


			pitch += -input.GetRawInput(GeneralInput.AxesNames.MouseY) * sensitivity;
			pitch = Mathf.Clamp(pitch, -yMax, yMax);

			transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, transform.localEulerAngles.z);
		}
		else
		{
			Vector2 rawCamInput = new Vector2(input.GetRawInput(GeneralInput.AxesNames.CameraX), input.GetRawInput(GeneralInput.AxesNames.CameraY));
			Vector2 normCamInput = rawCamInput.normalized;
			Vector2 camInput = new Vector2(normCamInput.x * Mathf.Abs(rawCamInput.x), normCamInput.y * Mathf.Abs(rawCamInput.y));

			if (cameraAutoSetting == CameraAuto.On && !SlopeInfo.IsSlopeSteepOrUp(sc.gravityDir, ppc.groundInfo.up))
			{

				Vector3 playerInput = new Vector3(input.GetSmoothedInput(GeneralInput.AxesNames.Horizontal), 0, input.GetSmoothedInput(GeneralInput.AxesNames.Vertical));
				playerInput = playerInput.normalized;

				float xVelocity = ExtVector3.MagnitudeInDirection(playerVelocity, cameraHolder.right);
				xVelocity = (Mathf.Sign(xVelocity) == Mathf.Sign(playerInput.x)) ? xVelocity : 0;

				cameraHolder.Rotate(0, autoYTurnSpeed * (playerInput.x) * Mathf.Abs(xVelocity) * Time.deltaTime, 0);


			}

			cameraHolder.Rotate(0, manualTurnSpeed * camInput.x * Time.deltaTime, 0);

			if (Mathf.Approximately(camInput.x, 0))
			{
				//SnapToCardinalDirection(camInput.x);
			}



			transform.position = followPosition.position;

			pitch -= camInput.y * manualTurnSpeed * Time.deltaTime;
			pitch = Mathf.Clamp(pitch, -yMax, yMax);

			transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, transform.localEulerAngles.z);



			cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraTransform.localPosition.y, -defaultDistance);

			//float hitDistance = CheckCameraDistanceCollision(defaultDistance);
			//cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraTransform.localPosition.y, -hitDistance);
		}
	}

	float CheckCameraDistanceCollision (float currentDistance)
    {
		float hitDistance = currentDistance;
		RaycastHit hitInfo = new RaycastHit();

		Debug.DrawRay(followPosition.position, -cameraTransform.forward * currentDistance, Color.green, 5);
		if (Physics.SphereCast (followPosition.position, 0.4f, -cameraTransform.forward, out hitInfo, currentDistance, collisionLayers))
        {
			Debug.Log("hit");

			if (hitInfo.distance < Mathf.Abs(currentDistance))
            {
				hitDistance = hitInfo.distance;
            }
        }


		return hitDistance;
    }



	public void GoIntoFirstPerson ()
    {
		inFirstPerson = true;
		transform.position = player.position;
		cameraTransform.localPosition = Vector3.zero;
		Vector3 camForward = Vector3.ProjectOnPlane (ppc.transform.TransformDirection (ppc.facingDir), sc.gravityDir).normalized;
		float camAngle = Vector3.SignedAngle(camForward, Vector3.forward, -Vector3.up);
		cameraHolder.position = player.position;
		cameraHolder.rotation = Quaternion.identity;
		transform.rotation = Quaternion.identity;
		cameraHolder.eulerAngles = new Vector3(0, camAngle, 0);
		transform.position = player.position;
		cameraTransform.localPosition = Vector3.zero;
		pitch = 0;

		savedForwardDirection = ppc.facingDir;
	}

	public void LeaveFirstPerson ()
    {
		inFirstPerson = false;
		cameraHolder.position = followPosition.position;
		transform.position = followPosition.position;
		cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, 0.5f, cameraTransform.localPosition.z);
		cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraTransform.localPosition.y, -defaultDistance);
		pitch = defaultPitch;
		transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, transform.localEulerAngles.z);
		float forwardAngle = Vector3.SignedAngle(savedForwardDirection, Vector3.forward, -Vector3.up);
		cameraHolder.eulerAngles = new Vector3(cameraHolder.eulerAngles.x, forwardAngle, cameraHolder.eulerAngles.z);
	}



	public void DoFirstPerson ()
    {
		savedForwardDirection = ppc.facingDir;
		cameraHolder.position = player.position;
		transform.position = player.position;

		Vector2 rawCamInput = new Vector2(input.GetRawInput(GeneralInput.AxesNames.CameraX), input.GetRawInput(GeneralInput.AxesNames.CameraY));
		Vector2 normCamInput = rawCamInput.normalized;
		Vector2 camInput = new Vector2(normCamInput.x * Mathf.Abs(rawCamInput.x), normCamInput.y * Mathf.Abs(rawCamInput.y));

		cameraHolder.Rotate(0, manualTurnSpeed * camInput.x * Time.deltaTime, 0);
		float forwardAngle = Vector3.SignedAngle(ppc.facingDir, Vector3.forward, -Vector3.up);
		float difference = Mathf.DeltaAngle(forwardAngle, cameraHolder.eulerAngles.y);

		if (difference > 90)
        {
			cameraHolder.eulerAngles = new Vector3(cameraHolder.eulerAngles.x, forwardAngle+90, cameraHolder.eulerAngles.z);
        }
		if (difference < -90)
		{
			cameraHolder.eulerAngles = new Vector3(cameraHolder.eulerAngles.x, forwardAngle - 90, cameraHolder.eulerAngles.z);
		}

		//cameraHolder.eulerAngles = new Vector3(cameraHolder.eulerAngles.x, cameraHolder.eulerAngles.y, cameraHolder.eulerAngles.z);

		pitch -= camInput.y * manualTurnSpeed * Time.deltaTime;
		pitch = Mathf.Clamp(pitch, -yMax, 89);

		transform.localEulerAngles = new Vector3(pitch, transform.localEulerAngles.y, transform.localEulerAngles.z);
	}




    public void SnapToCardinalDirection (float camInput)
    {
		float camAngle = Vector3.SignedAngle(cameraHolder.forward, Vector3.forward, -Vector3.up);
		float snapTolerance = 5f;
		bool snapped = false;

		float delta = Mathf.DeltaAngle(camAngle, 90);
		if (Mathf.Abs(delta) <= snapTolerance)
        {
			camAngle = 90;
			snapped = true;

			Debug.Log(delta);
		}

		delta = Mathf.DeltaAngle(camAngle, -90);
		if (Mathf.Abs(delta) <= snapTolerance)
		{
			camAngle = -90;
			snapped = true;

			Debug.Log(delta);
		}

		delta = Mathf.DeltaAngle(camAngle, 0);
		if (Mathf.Abs(delta) <= snapTolerance)
		{
			camAngle = 0;
			snapped = true;

			Debug.Log(delta);
		}

		delta = Mathf.DeltaAngle(camAngle, 180);
		if (Mathf.Abs(delta) <= snapTolerance)
		{
			camAngle = 180;
			snapped = true;

			Debug.Log(delta);
		}

		Debug.Log("snapped");

		cameraHolder.eulerAngles = new Vector3(cameraHolder.eulerAngles.x, camAngle, cameraHolder.eulerAngles.z);
    }


	public void SetCameraControlSetting(int setting)
    {
		cameraControlSetting = (CameraControl)setting;
    }

	public void SetCameraAutoSetting(int setting)
	{
		cameraAutoSetting = (CameraAuto)setting;
	}


	public List<string> GetCameraControlList ()
    {
		return Enum.GetNames(typeof(CameraControl)).ToList();
    }

	public List<string> GetCameraAutoList()
	{
		return Enum.GetNames(typeof(CameraAuto)).ToList();
	}
}
