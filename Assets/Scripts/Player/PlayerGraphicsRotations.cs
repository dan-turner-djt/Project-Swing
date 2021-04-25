using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGraphicsRotations : MonoBehaviour {

	public Transform objectUp;
	public GameObject finPointer;


	public void DoStart()
	{
		finPointer = GameObject.Find("Fin");
	}

	public void DoUpdate (float deltaTime, Vector3 groundPivotUp, bool grounded, bool isSwinging, Vector3 facingDir) 
	{
		if (isSwinging) 
		{
			objectUp.rotation = Quaternion.FromToRotation (Vector3.up, groundPivotUp);

			TurnDirectionPointer(facingDir, deltaTime);

			return;
		}

		float lerpSpeed = (grounded)? 35 : 30;

		objectUp.rotation = Quaternion.Lerp (objectUp.rotation, Quaternion.FromToRotation (Vector3.up, groundPivotUp), lerpSpeed * deltaTime);


		TurnDirectionPointer(facingDir, deltaTime);
	}


	public void TurnDirectionPointer (Vector3 facingDir, float deltaTime)
    {
		float currentAngle = finPointer.transform.localEulerAngles.y;
		float targetAngle = Vector3.SignedAngle(Vector3.forward, facingDir, transform.up);
		float newAngle = currentAngle;

		float angleDif = Mathf.DeltaAngle(currentAngle, targetAngle);
		float snapTolerance = 0.6f;

		if (false)
		{
			newAngle = ExtVector3.CustomLerpAngle(currentAngle, targetAngle, 5, deltaTime, snapTolerance);
		}
		else
        {
			newAngle = targetAngle;
        }

		finPointer.transform.localEulerAngles = new Vector3(finPointer.transform.localEulerAngles.x, newAngle, finPointer.transform.localEulerAngles.z);

	}
}
