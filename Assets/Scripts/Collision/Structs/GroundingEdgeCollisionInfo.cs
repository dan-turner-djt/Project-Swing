using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GroundingEdgeCollisionInfo
{
	public Collider collider;
	public float sphereRadius;
	public Vector3 detectionOrigin;
	public ContactInfo detectedGroundNormal;
	public Vector3 interpolatedNormal;
	public Vector3 calculatedGroundNormal;
	public Vector3 calculatedEdgeNormal;
	public Vector3 transformUp;
	public List<ContactInfo> edgeNormals;
	public List<ContactInfo> sortedGroundNormals;
	public List<ContactInfo> sortedEdgeNormals;
	Vector3 gravDir;
	Vector3 velocity;


	public GroundingEdgeCollisionInfo(Collider collider, Vector3 detectionOrigin, float sphereRadius, ContactInfo detectedGroundNormal, Vector3 interpolatedNormal, List<ContactInfo> edgeNormals, Vector3 transformUp, Vector3 gravDir, Vector3 velocity)
	{
		this.collider = collider;
		this.sphereRadius = sphereRadius;
		this.detectionOrigin = detectionOrigin;
		this.detectedGroundNormal = detectedGroundNormal;
		this.interpolatedNormal = interpolatedNormal;
		this.edgeNormals = edgeNormals;
		this.transformUp = transformUp;
		this.gravDir = gravDir;
		this.velocity = velocity;

		sortedGroundNormals = new List <ContactInfo> ();
		sortedEdgeNormals = new List <ContactInfo> ();
		calculatedGroundNormal = Vector3.zero;
		calculatedEdgeNormal = Vector3.zero;

		CleanNormals ();
		SortNormals ();
	}


	public void CleanNormals () 
	{
		List<ContactInfo> checkedNormals = new List <ContactInfo> ();
		checkedNormals.Add (detectedGroundNormal);
		List<ContactInfo> newEdgeNormals = new List <ContactInfo> ();
		Vector3 flattenedInterpolated = Vector3.ProjectOnPlane (interpolatedNormal, detectedGroundNormal.normal);

		float smallestAngleToUp = Vector3.Angle (transformUp, detectedGroundNormal.normal);
		ContactInfo closestNormalToUp = detectedGroundNormal;


		for (int i = 0; i < edgeNormals.Count; i++) 
		{
			Vector3 flattenedEdgeNormal = Vector3.ProjectOnPlane (edgeNormals[i].normal, detectedGroundNormal.normal);

			if (ExtVector3.IsInDirection (flattenedEdgeNormal, flattenedInterpolated)) 
			{
				float angleToUp = Vector3.Angle (transformUp, edgeNormals [i].normal);

				if (angleToUp < smallestAngleToUp) 
				{
					smallestAngleToUp = angleToUp;
					closestNormalToUp = edgeNormals [i];
				}

				checkedNormals.Add (edgeNormals [i]);
			}
		}

		detectedGroundNormal = closestNormalToUp;

		for (int i = 0; i < checkedNormals.Count; i++) 
		{
			if (checkedNormals [i].normal != detectedGroundNormal.normal) 
			{
				newEdgeNormals.Add (checkedNormals [i]);
			}
		}

		edgeNormals = newEdgeNormals;
	}

	public void SortNormals () 
	{
		sortedGroundNormals.Clear ();
		sortedEdgeNormals.Clear ();

		sortedGroundNormals.Add (detectedGroundNormal);

		for (int i = 0; i < edgeNormals.Count; i++) 
		{
			if (CanWalkToSlope (edgeNormals [i].normal, detectedGroundNormal.normal)) 
			{
				sortedGroundNormals.Add (edgeNormals [i]);
			}
			else 
			{
				sortedEdgeNormals.Add (edgeNormals [i]);
			}
		}

		CalculateNewGroundNormal ();
		CalculateNewEdgeNormal ();
	}


	public void CalculateNewGroundNormal()
	{
		//the idea is to partially rebuild the interpolated normal using the walkable ground-edge normals we have collected

		Vector3 newInterpolatedGroundNormal = Vector3.zero;
		Vector3 flattenedInterpolatedNormal = Vector3.ProjectOnPlane(interpolatedNormal, detectedGroundNormal.normal);

		bool found = false;

		for (int i = 0; i < sortedGroundNormals.Count; i++)
		{
			Vector3 flattenedCurrentNormal = Vector3.ProjectOnPlane(sortedGroundNormals[i].normal, detectedGroundNormal.normal);

			float magInDir = ExtVector3.MagnitudeInDirection(flattenedInterpolatedNormal, flattenedCurrentNormal.normalized, false);

			if (magInDir <= 0)
				continue;

			found = true;

			Vector3 lateralVector = flattenedCurrentNormal.normalized * magInDir;

			float height = Mathf.Sqrt(1 - magInDir * magInDir);
			Vector3 verticalVector = detectedGroundNormal.normal.normalized * height;

			Vector3 newNormal = (lateralVector + verticalVector).normalized;

			newInterpolatedGroundNormal += newNormal;
		}


		if (found)
		{
			calculatedGroundNormal = newInterpolatedGroundNormal.normalized;
		}
		else
		{
			calculatedGroundNormal = detectedGroundNormal.normal;
		}

		if (!CollisionController.CanWalkOnSlope(calculatedGroundNormal, -gravDir, gravDir, Vector3.zero))
        {
			//if we can't land on the slope then we should'nt do the next part anyway
			return;
        }


		//now we want to go through and check if any edges are able to be partially walked on slowly if they face the grav up
		flattenedInterpolatedNormal = Vector3.ProjectOnPlane(interpolatedNormal, calculatedGroundNormal); //use the newly calculated ground normal here
		Vector3 flattenedVelocity = Vector3.ProjectOnPlane(velocity, calculatedEdgeNormal);
		newInterpolatedGroundNormal = calculatedGroundNormal;
		found = false;
		List<ContactInfo> edgeNormalsToRemove = new List<ContactInfo>();

		for (int i = 0; i < sortedEdgeNormals.Count; i++)
		{
			Vector3 flattenedCurrentNormal = Vector3.ProjectOnPlane(sortedEdgeNormals[i].normal, calculatedGroundNormal); //use the newly calculated ground normal here

			float magInDir = ExtVector3.MagnitudeInDirection(flattenedInterpolatedNormal, flattenedCurrentNormal, true);

			if (magInDir <= 0)
				continue;


			Vector3 edgeLine = Vector3.Cross(calculatedGroundNormal, sortedEdgeNormals[i].normal).normalized; //perpendicular direction, ie the line of the edge
			Vector3 interpFlattenedAgainstEdgeLine = Vector3.ProjectOnPlane(interpolatedNormal, edgeLine);

			if (!CollisionController.CanWalkOnSlope(interpFlattenedAgainstEdgeLine, -gravDir, gravDir, Vector3.zero))
            {
				continue;
            }

			float angleGroundToGravUp = Vector3.SignedAngle(-gravDir, calculatedGroundNormal, edgeLine);
			float angleEdgeToGravUp = Vector3.SignedAngle(-gravDir, sortedEdgeNormals[i].normal, edgeLine);

			//can normally land on edge and the ground and edge are opposite (-gravDir lies between them)
			if (CollisionController.CanWalkOnSlope(sortedEdgeNormals[i].normal, -gravDir, gravDir, Vector3.zero)
			&& (Mathf.Sign(angleGroundToGravUp) != Mathf.Sign(angleEdgeToGravUp) || (calculatedGroundNormal == -gravDir || sortedEdgeNormals[i].normal == -gravDir)))
            {
				float velocityMagInDir = ExtVector3.MagnitudeInDirection(flattenedVelocity, flattenedCurrentNormal);

				if (velocityMagInDir < SlopeInfo.maxVelocityForHardEdgeWrapping)
				{
					found = true;

					Vector3 remaining = calculatedGroundNormal - interpFlattenedAgainstEdgeLine;
					newInterpolatedGroundNormal -= remaining;

					edgeNormalsToRemove.Add(sortedEdgeNormals[i]); //we have to remove the edge normal we used or else it will still think there is a hard edge and stop us from being grounded
				}
			}

		}

		if (found)
		{
			//Debug.Log("WALK ON IT");

			calculatedGroundNormal = newInterpolatedGroundNormal.normalized;

			foreach (ContactInfo edgeNormal in edgeNormalsToRemove)
            {
				sortedEdgeNormals.Remove(edgeNormal);
            }
		}
		else
        {
			//Debug.Log("DONT WALK ON IT DARGHHH");
        }
	}

	public void CalculateNewEdgeNormal () 
	{
		//the idea is to calculate a new normal which is an average of the edge normals weighted by their alignment to the interpolated normal 

		if (sortedEdgeNormals.Count == 0) 
		{
			calculatedEdgeNormal = Vector3.zero;
			return;
		}

		Vector3 newInterpolatedEdgeNormal = Vector3.zero;
		Vector3 flattenedInterpolatedNormal = Vector3.ProjectOnPlane (interpolatedNormal, calculatedGroundNormal); //use the newly calculated ground normal here

		bool found = false;

		for (int i = 0; i < sortedEdgeNormals.Count; i++) 
		{
			Vector3 flattenedCurrentNormal = Vector3.ProjectOnPlane (sortedEdgeNormals [i].normal, calculatedGroundNormal); //again use the newly calculated ground normal here
			//Vector3 currentNormal = sortedEdgeNormals[i].normal;

			float magInDir = ExtVector3.MagnitudeInDirection (flattenedInterpolatedNormal, flattenedCurrentNormal, true);

			if (magInDir <= 0)
				continue;

			found = true;

			newInterpolatedEdgeNormal += flattenedCurrentNormal * magInDir;
		}

		if (found) 
		{
			calculatedEdgeNormal = newInterpolatedEdgeNormal.normalized;
		} 
		else 
		{
			calculatedEdgeNormal = Vector3.zero;
		}
	}



	public bool CanWalkToSlope(Vector3 normal, Vector3 comparedNormal)
	{
		if(normal == Vector3.zero) return false;
		return ExtVector3.Angle(normal, comparedNormal) <= SlopeInfo.convexSlopeLimit;
	}
		

	public bool GetOnHardEdge ()
	{
		//a hard edge is an edge with an edge normal that is unwalkable

		if (calculatedEdgeNormal != Vector3.zero) 
		{
			return true;
		}

		return false;
	}
		
} 
