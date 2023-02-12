using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PathCreation;

public class MovingPlatformController : PhysicsSimulatable {


	public Transform platformObject;
	public PathCreator pathCreator;

	public enum MoveMode
	{
		Constant, Smoothed
	}
	public MoveMode moveMode;
	public EndOfPathInstruction endOfPathInstruction;

	public float moveSpeed = 2;
	public float minMoveSpeedDivider = 2;
	public float smoothingPointDist = 0.02f;

	float distanceTravelled;
	float currentSpeed;
	int dir = 1;

	public Vector3 startingPos;
	public Vector3 velocity = Vector3.zero;

	void Awake ()
	{
		base.DoAwake ();
	}

	void Start () 
	{
		base.DoStart ();
		sc.AddMovingPlatformToList (gameObject);
		currentlySimulatable = true;

		Vector3 newStartPos = pathCreator.path.GetPointAtDistance (0, endOfPathInstruction);
		platformObject.position = newStartPos;
	}
	

	public override void DoUpdate () 
	{
		base.DoUpdate ();





	}

	public override void DoFinalUpdate ()
	{
		base.DoFinalUpdate ();
	}


	public override void DoPreCollisionUpdate (float deltaTime)
	{
		
		if (endOfPathInstruction != EndOfPathInstruction.Loop) 
		{
			if (moveMode == MoveMode.Constant) 
			{
				distanceTravelled += moveSpeed * dir * deltaTime;
			} 
			else if (moveMode == MoveMode.Smoothed) 
			{
				Vector3 currentPathPoint = pathCreator.path.GetPointAtDistance (distanceTravelled, endOfPathInstruction);
				float time = pathCreator.path.GetClosestTimeOnPath (currentPathPoint);

				if (time < smoothingPointDist) 
				{
					currentSpeed = Mathf.Max (moveSpeed * time * (1 / smoothingPointDist), moveSpeed / minMoveSpeedDivider);
				} 
				else if (time > (1 - smoothingPointDist)) 
				{
					currentSpeed = Mathf.Max (moveSpeed * (1 - time) * (1 / smoothingPointDist), moveSpeed / minMoveSpeedDivider);
				}

				distanceTravelled += currentSpeed * dir * deltaTime;
			}
		} 
		else 
		{
			distanceTravelled += moveSpeed * dir * deltaTime;
		}



		Vector3 newPathPoint = pathCreator.path.GetPointAtDistance (distanceTravelled, endOfPathInstruction);
		velocity = newPathPoint - platformObject.position;

	}

	public override void DoPostCollisionUpdate (float deltaTime)
	{
		//float time = pathCreator.path.GetClosestTimeOnPath (pathPoint);
	}


	public override void PrepareForCollision (float deltaTime)
	{
		SetNormalCollisionVelocity (velocity);
	}
	
	public override bool DoCollisionUpdate (float deltaTime, Vector3 stepVelocity)
	{
		platformObject.position += stepVelocity;
		return true;
	}
}
