using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhysicsSimulator : MonoBehaviour {

	public GeneralSceneController sc;

	public SubStepUpdater subStepUpdater = new SubStepUpdater();
	public bool isInsideSubUpdater {get; private set;}
	public const int maxVelocitySteps = 20; //A safety in case we are moving very fast we dont want to divide our velocity into to many steps since that can cause lag and freeze the game, so we prefer to have the collision be unsafe.

	List<PhysicsSimulatable> simulatablesToUpdate = new List <PhysicsSimulatable> ();

	void Awake ()
	{
		subStepUpdater.subStepMethod = PerformSimulation;
	}

	void Start () 
	{
		
	}
	

	public void DoUpdate () 
	{
		isInsideSubUpdater = true;
		subStepUpdater.Update();
		isInsideSubUpdater = false;
	}


	void PerformSimulation (float deltaTime)
	{
		simulatablesToUpdate.Clear ();

		foreach (var simulatable in sc.physicsSimulatables) 
		{
			if (simulatable.currentlySimulatable) 
			{
				simulatablesToUpdate.Add (simulatable);
				simulatable.isInsideSubUpdater = true;
				simulatable.DoPreCollisionUpdate (deltaTime);
			}

		}

		PerformCollision (deltaTime);

		foreach (var simulatable in simulatablesToUpdate) 
		{
			simulatable.DoPostCollisionUpdate (deltaTime);
			simulatable.isInsideSubUpdater = false;
		}
	}


	void PerformCollision (float deltaTime)
	{
		Physics.SyncTransforms();

		List<PhysicsSimulatable> nonDepenetratablesToUpdate = new List<PhysicsSimulatable>();
		List<PhysicsSimulatable> depenetratablesToUpdate = new List<PhysicsSimulatable>();



		foreach (var simulatable in simulatablesToUpdate) 
		{
			simulatable.PrepareForCollision (deltaTime);

			if (simulatable.depenetratable) 
			{
				depenetratablesToUpdate.Add (simulatable);
			} 
			else 
			{
				nonDepenetratablesToUpdate.Add (simulatable);
			}
		}



		float biggestDistance = 0;
		if (simulatablesToUpdate.Count == 1) 
		{
			biggestDistance = simulatablesToUpdate [0].normalCollisionVelocity.magnitude;
		} 
		else 
		{
			foreach (var simulatable in simulatablesToUpdate) 
			{
				//cant condense this into previous loop as collision velocity must be set for everyone first
				Vector3 moveVector = simulatable.normalCollisionVelocity;

				//loop through and add distances together to see which pair is the greatest
				foreach (var newSimulatable in simulatablesToUpdate) 
				{
					//if the same
					if (newSimulatable == simulatable) 
					{
						continue;
					} 
					else 
					{
						Vector3 otherMoveVector = newSimulatable.normalCollisionVelocity;
						float combinedDistance = (moveVector - otherMoveVector).magnitude;

						//find what the fastest is
						if (moveVector.magnitude > biggestDistance) 
						{
							biggestDistance = moveVector.magnitude;
						}
						if (combinedDistance > biggestDistance) 
						{
							biggestDistance = combinedDistance;
						}
					}
				}
			}
		}

			

		//calculate time step

		//We cut our velocity up into steps so that we never move more than a certain amount of our radius per step.
		//This prevents tunneling and acts as a "Continuous Collision Detection", but is not as good as using a CapsuleCast.
		float maxRadiusMove = 0.5f/3;
		int steps = 1;

		if(biggestDistance > maxRadiusMove)
		{
			steps = Mathf.CeilToInt(biggestDistance / maxRadiusMove);
			if(steps > maxVelocitySteps)
			{
				steps = maxVelocitySteps;

				#region Debug
				#if UNITY_EDITOR
				//if(infoDebug.printOverMaxVelocitySteps) Debug.LogWarning("PlayerRigidbody GetCollisionSafeVelocity velocity steps is larger than maxVelocitySteps. To avoid major lag we are limiting the amount of steps which means unsafe collision handling.", gameObject);
				#endif
				#endregion
			}
		}

		//calculate each's step velocity before we begin
		foreach (var simulatable in simulatablesToUpdate) 
		{
			simulatable.SetSteps(steps);
			simulatable.SetStepVelocity ();
		}


		List<PhysicsSimulatable> failedSimulatables = new List <PhysicsSimulatable>();

		//do collision updates
		for (int i = 0; i < steps; i++) 
		{
			bool allCollisionsSuccessful = true;

			//update non depentratbles first
			foreach (var nonDepenetratable in nonDepenetratablesToUpdate) 
			{
				bool collisionSuccessful = nonDepenetratable.DoCollisionUpdate (deltaTime, nonDepenetratable.collisionInfo.stepVelocity);
				Physics.SyncTransforms();
			}

			//then do collision and update for depenetratables
			foreach (var depenetratable in depenetratablesToUpdate) 
			{
				bool collisionSuccessful = depenetratable.DoCollisionUpdate (deltaTime, depenetratable.collisionInfo.stepVelocity);
				Physics.SyncTransforms();

				if (!collisionSuccessful) 
				{
					Debug.Log ("someones collision failed!!");
					allCollisionsSuccessful = false;
					//failedSimulatables.Add (depenetratable); //dont continue to update it if its collision failed

				}
			}

			//remove the failed objects
			foreach (var s in failedSimulatables) 
			{
				depenetratablesToUpdate.Remove (s);
			}
			failedSimulatables.Clear ();

		}
			




		foreach (var simulatable in simulatablesToUpdate) 
		{
			simulatable.FinalizeAfterCollision (deltaTime);
		}
	}
}
