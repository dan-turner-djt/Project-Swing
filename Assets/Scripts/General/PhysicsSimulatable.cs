using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsSimulatable : Updatable {

	public bool depenetratable;
	public bool currentlySimulatable;

	public bool isInsideSubUpdater {get; set;}
	public CollisionInfo collisionInfo;
	public Vector3 normalCollisionVelocity;
	public Vector3 movingPlatformVelocity;
	public Vector3 extraCollisionVelocity;

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

	public virtual void FinalizeAfterCollisionStep(float deltaTime)
    {

    }

	public virtual void FinalizeAfterCollision (float deltaTime)
	{

	}

	public void SetSteps (int steps)
    {
		collisionInfo.totalSteps = steps;
    }

	public void SetStepVelocity ()
	{
		//Debug.Log(normalCollisionVelocity.magnitude);
		collisionInfo.stepVelocity = (normalCollisionVelocity + movingPlatformVelocity + extraCollisionVelocity) / collisionInfo.totalSteps; //collisionVelocity is edited after a step (it comes from collisionInfo.velocity which is the running editted velocity)
	}

	public void SetNormalCollisionVelocity (Vector3 newVelocity)
    {
		normalCollisionVelocity = newVelocity;
    }

	public void SetMovingPlatformVelocity(Vector3 newMovingPlatformVelocity)
	{
		movingPlatformVelocity = newMovingPlatformVelocity;
	}

	public void SetExtraCollisionVelocity(Vector3 newExtraVelocity)
	{
		extraCollisionVelocity = newExtraVelocity;
	}


	public struct CollisionInfo 
	{
		public Vector3 origin;
		public Vector3 targetPosition;
		public Vector3 safeMoveDirection;
		public Vector3 velocity;
		public int totalSteps;
		public Vector3 totalPreviousStepsVelocity;
		public Vector3 stepVelocity;
		public bool hasCollided;
		public bool hasFailed;
		public int temporaryAttempts;
		public int attempts;
		public bool collisionSuccessful;
		public bool foundWalkableGroundNormal;
		public bool foundMovingPlatform;
		public List<GroundCastInfo> wallPoints;
	}
}
