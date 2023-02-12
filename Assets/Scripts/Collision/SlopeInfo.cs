using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SlopeInfo
{
	public enum SlopeType
	{
		None, Shallow, Moderate, Steep, SuperSteep
	}

	public static float walkableSlopeLimit = 70;
	public static float concaveSlopeLimit = 60;
	public static float convexSlopeLimit = 45;
	public static float staircaseSlopeLimit = 89;
	public static float maxVelocityForHardEdgeWrapping = 5;


	public static bool IsSlopeSteepOrUp (float slopeAngle)
	{
		SlopeType slopeType = GetSlopeType (slopeAngle);

		if (slopeType == SlopeType.Steep || slopeType == SlopeType.SuperSteep) 
		{
			return true;
		}

		return false;
	}

	public static bool IsSlopeSteepOrUp(Vector3 gravDir, Vector3 transformUp)
	{
		float slopeAngle = Vector3.Angle(-gravDir, transformUp);

		SlopeType slopeType = GetSlopeType(slopeAngle);

		if (slopeType == SlopeType.Steep || slopeType == SlopeType.SuperSteep)
		{
			return true;
		}

		return false;
	}


	

	public static SlopeType GetSlopeType (float slopeAngle)
	{
		if (Mathf.Approximately (slopeAngle, 0)) 
		{
			return SlopeType.None;
		} 
		else if (slopeAngle < 30) 
		{
			return SlopeType.Shallow;
		} 
		else if (slopeAngle < 45) 
		{
			return SlopeType.Moderate;
		} 
		else if (slopeAngle <= 70) 
		{
			return SlopeType.Steep;
		} 
		else return SlopeType.SuperSteep;
	}

}
