using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsSimulatable : Updatable {

	public bool depenetratable;
	public bool currentlySimulatable;

	public bool isInsideSubUpdater {get; set;}
	public CollisionInfo collisionInfo;
	public Vector3 collisionVelocity;

	protected void DoAwake ()
	{
		base.DoAwake ();
	}


	protected void DoStart () 
	{
		base.DoStart ();

		sc.AddPhysicsSimulatableToList (gameObject);
	}


	public override void DoUpdate () 
	{
		base.DoUpdate ();
	}

	public override void DoFinalUpdate ()
	{
		base.DoFinalUpdate ();
	}

	public virtual void DoPreCollisionUpdate (float deltaTime)
	{
		
	}

	public virtual void DoPostCollisionUpdate (float deltaTime)
	{

	}

	public virtual bool DoCollisionUpdate (float deltaTime, Vector3 stepVelocity)
	{
		return true;
	}

	public virtual void PrepareForCollision (float deltaTime)
	{

	}

	public virtual void FinalizeAfterCollision (float deltaTime)
	{

	}


	public void SetStepVelocity (int steps)
	{
		collisionInfo.stepVelocity = collisionVelocity / steps;
	}


	public struct CollisionInfo 
	{
		public Vector3 origin;
		public Vector3 targetPosition;
		public Vector3 safeMoveDirection;
		public Vector3 velocity;
		public Vector3 stepVelocity;
		public bool hasCollided;
		public bool hasFailed;
		public int temporaryAttempts;
		public int attempts;
		public bool collisionSuccessful;
		public bool foundWalkableGroundNormal;
		public bool foundMovingPlatform;
		public List<SphereCollisionDetect.CollisionPointInfo> pointsInfo;
		public float minWallAngle;
		public float maxWallAngle;
		public Vector3 minWallNormal;
		public Vector3 maxWallNormal;
		public Vector3 averageWallNormal;
	}
}
