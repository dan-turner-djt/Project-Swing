using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundCastInfo
{
	public Vector3 detectionOrigin;
	public Vector3 point;
	public float sphereRadius;
	public Vector3 normal;
	public Collider collider;
	public bool onEdge;
	public bool isValidStep;
	public Vector3 staircaseNormal;
	public bool hasHit {get {return point != Vector3.zero;}}
	public GroundingEdgeCollisionInfo edgeInfo;
	public bool walkable;
	public bool partiallyWalkable;
	public bool previouslyWall;
	public float velocityAgainstNormal;
	public bool wallDepenetrated;
	public bool wallActingAsFloor;
	public bool ignoreWallForVelocityLimiting;

	// Used for wall collision only
	public Vector3 wallDepenDir;

	public GroundCastInfo() { }

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
		this.staircaseNormal = Vector3.zero;
		this.wallDepenDir = Vector3.zero;
		this.velocityAgainstNormal = 0;
		this.wallDepenetrated = false;
		this.wallActingAsFloor = false;
		this.ignoreWallForVelocityLimiting = false;

		if (collider.gameObject.tag == "Staircase")
		{
			staircaseNormal = GetCalculatedGroundNormal();
		}
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
