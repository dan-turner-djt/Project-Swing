using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class PathLauncherController : MonoBehaviour {

	public PathCreator pathCreator;

	public float power;

	public Vector3 GetVelocity ()
	{
		return transform.up * power;
	}


	public Vector3 GetNextTargetPosition (float distance)
	{
		Vector3 newPathPoint = pathCreator.path.GetPointAtDistance (distance, EndOfPathInstruction.Stop);
		return newPathPoint;
	}

	public bool CheckIfAtEndOfPath (Vector3 playerPos)
	{
		float time = pathCreator.path.GetClosestTimeOnPath (playerPos);

		if (time >= 0.99f) 
		{
			return true;
		}

		return false;
	}
}
