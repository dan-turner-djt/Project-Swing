using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GroundCastInfo
{
	public Vector3 detectionOrigin;
	public Vector3 point;
	public float sphereRadius;
	public Vector3 normal;
	public Collider collider;
	public bool onEdge;
	public bool isValidStep;
	public bool hasHit {get {return point != Vector3.zero;}}
	public GroundingEdgeCollisionInfo edgeInfo;
	public bool walkable;
	public bool partiallyWalkable;
	public bool previouslyWall;

	public GroundCastInfo(Vector3 detectionOrigin, float sphereRadius, Vector3 point, Vector3 normal, GroundingEdgeCollisionInfo edgeInfo, Collider collider, bool onEdge = false)
	{
		this.detectionOrigin = detectionOrigin;
		this.sphereRadius = sphereRadius;
		this.point = point;
		this.normal = normal;
		this.edgeInfo = edgeInfo;
		this.collider = collider;
		this.onEdge = onEdge;
		this.isValidStep = false;
		this.walkable = false;
		this.partiallyWalkable = false;
		this.previouslyWall = false;
	}



	public Vector3 GetCalculatedGroundNormal()
	{
		if (onEdge) 
		{
			return edgeInfo.calculatedGroundNormal;
		}

		return normal;
	}


	public Vector3 GetInterpolatedNormal()
	{
		if (onEdge)
		{
			return edgeInfo.interpolatedNormal;
		}

		return normal;
	}



	public bool GetIsSteppable ()
	{
		if (edgeInfo.GetOnHardEdge ()) 
		{
			return isValidStep;
		}

		return true;
	}
}
