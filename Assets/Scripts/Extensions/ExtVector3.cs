using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtVector3 {

	public static readonly Vector3[] GeneralDirections = new Vector3[] {Vector3.right, Vector3.up, Vector3.forward, Vector3.left, Vector3.down, Vector3.back};

	public static float Maximum(this Vector3 vector)
	{
		return ExtMathf.Max(vector.x, vector.y, vector.z);
	}

	public static float Minimum(this Vector3 vector)
	{
		return ExtMathf.Min(vector.x, vector.y, vector.z);
	}

	public static bool IsParallel(Vector3 direction, Vector3 otherDirection, float precision = .000001f)
	{
		return Vector3.Cross(direction, otherDirection).sqrMagnitude < precision;
	}

	public static Vector3 ClosestDirectionTo(Vector3 direction1, Vector3 direction2, Vector3 targetDirection)
	{
		return (Vector3.Dot(direction1, targetDirection) > Vector3.Dot(direction2, targetDirection)) ? direction1 : direction2;
	}

	//from and to must be normalized
	public static float Angle(Vector3 from, Vector3 to)
	{
		return Mathf.Acos(Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f)) * Mathf.Rad2Deg;
	}

	public static Vector3 Direction(Vector3 startPoint, Vector3 targetPoint)
	{
		return (targetPoint - startPoint).normalized;
	}

	public static Vector3 Direction(Vector3 startPoint, Vector3 targetPoint, out float distance)
	{
		return Normalize(targetPoint - startPoint, out distance);
	}

	public static Vector3 Normalize(this Vector3 vector, out float magnitude)
	{
		magnitude = vector.magnitude;
		if(magnitude == 0) return Vector3.zero;
		return vector / magnitude;
	}

	public static bool IsInDirection(Vector3 direction, Vector3 otherDirection, float precision, bool normalizeParameters = true)
	{
		if(normalizeParameters)
		{
			direction.Normalize();
			otherDirection.Normalize();
		}
		return Vector3.Dot(direction, otherDirection) > 0f + precision;
	}
	public static bool IsInDirection(Vector3 direction, Vector3 otherDirection)
	{
		return Vector3.Dot(direction, otherDirection) > 0f;
	}

	public static float MagnitudeInDirection(Vector3 vector, Vector3 direction, bool normalizeParameters = true)
	{
		if(normalizeParameters) direction.Normalize();
		return Vector3.Dot(vector, direction);
	}

	public static Vector3 Abs(this Vector3 vector)
	{
		return new Vector3(Mathf.Abs(vector.x), Mathf.Abs(vector.y), Mathf.Abs(vector.z));
	}

	public static Vector3 ClosestGeneralDirection(Vector3 vector) {return ClosestGeneralDirection(vector, GeneralDirections);}
	public static Vector3 ClosestGeneralDirection(Vector3 vector, IList<Vector3> directions)
	{
		float maxDot = float.MinValue;
		int closestDirectionIndex = 0;

		for(int i = 0; i < directions.Count; i++)
		{ 
			float dot = Vector3.Dot(vector, directions[i]);
			if(dot > maxDot)
			{
				closestDirectionIndex = i;
				maxDot = dot;
			}
		}

		return directions[closestDirectionIndex];
	}


	public static string PrintFullVector3 (Vector3 v)
	{
		return "x: " + v.x + ", y: " + v.y + ", z: " + v.z;
	}


	public static Vector3 CustomLerpVector (Vector3 frm, Vector3 to, float turnSpd, float deltaTime, float snapTolerance, bool normalize)
	{
		Vector3 result = Vector3.Lerp (frm, to, turnSpd * deltaTime);

		if (normalize) 
		{
			result = result.normalized;

			if (Vector3.Angle (result, to) < snapTolerance) 
			{
				result = to;
			}
		}

		//doesnt work for non zero to vector
		else if (to == Vector3.zero && Mathf.Abs (to.magnitude-frm.magnitude) < snapTolerance) 
		{
			result = to;
		}


		return result;
	}


	public static Vector3 CustomLerpAngleFromVector (Vector3 frm, Vector3 to, float turnSpd, float deltaTime, Vector3 axis, float snapTolerance)
	{
		float frmAngle = Vector3.SignedAngle (frm, Vector3.forward, axis);
		float toAngle = Vector3.SignedAngle (to, Vector3.forward, axis);

		float resultAngle = CustomLerpAngle (frmAngle, toAngle, turnSpd, deltaTime, snapTolerance);

		Vector3 result = Quaternion.AngleAxis (resultAngle, -axis) * Vector3.forward;

		return result;
	}


	public static float CustomLerpAngle (float frm, float to, float turnSpd, float deltaTime, float snapTolerance)
	{
		float result = Mathf.LerpAngle (frm, to, turnSpd * deltaTime);

		if (Mathf.Abs (Mathf.DeltaAngle (result, to)) < snapTolerance) 
		{
			result = to;
		}

		return result;
	}


	public static Vector3 CustomMoveTowardsAngleFromVector(Vector3 frm, Vector3 to, float turnSpd, float deltaTime, Vector3 axis)
	{
		turnSpd = turnSpd * 18;

		float frmAngle = Vector3.SignedAngle(frm, Vector3.forward, axis);
		float toAngle = Vector3.SignedAngle(to, Vector3.forward, axis);

		float resultAngle = CustomMoveTowardsAngle(frmAngle, toAngle, turnSpd, deltaTime);

		Vector3 result = (Quaternion.AngleAxis(resultAngle, -axis) * Vector3.forward).normalized;

		return result;
	}

	public static float CustomMoveTowardsAngle(float frm, float to, float turnSpd, float deltaTime)
	{
		float result = Mathf.MoveTowardsAngle(frm, to, turnSpd * deltaTime);

		return result;
	}


	public static Vector3 CustomControlledLerpPosition (Vector3 frm, Vector3 to, float lerpSpeed, float min, float max, float deltaTime)
    {
		Vector3 result = new Vector3();

		Vector3 lerpResult = Vector3.Lerp(frm, to, lerpSpeed * deltaTime);
		Vector3 maxResult = Vector3.MoveTowards(frm, to, max * deltaTime);
		Vector3 minResult = Vector3.MoveTowards(frm, to, min * deltaTime);

		float lerpDelta = (frm - lerpResult).magnitude;
		float maxDelta = (frm - maxResult).magnitude;
		float minDelta = (frm - minResult).magnitude;

		//Debug.Log("lerp: " + lerpDelta + ", max: " + maxDelta + ", min: " + minDelta);

		if (lerpDelta > maxDelta)
        {
			result = maxResult;
        }
		else if (lerpDelta < minDelta)
        {
			result = minResult;
        }
		else
        {
			result = lerpResult;
        }

		return result;
    }
}
