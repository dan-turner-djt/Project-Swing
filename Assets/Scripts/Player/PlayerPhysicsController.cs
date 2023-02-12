using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPhysicsController : PhysicsSimulatable {

	PlayerActionsController playerActionsController;
	public PlayerInteractionsController pic;

	CollisionHandleInfo collisionHandleInfo = new CollisionHandleInfo ();


	// Coroutines
	[HideInInspector]
	public IEnumerator cLeftGroundTimer;

	public GameObject sphereObject;
	public GameObject capsuleObject;
	public GameObject directionPivot;
	public Transform groundPivot;
	public Collider currentCollider {get; private set;}
	public CapsuleCollider capsuleCollider {get; private set;}
	public SphereCollider sphereCollider {get; private set;}
	public MeshRenderer sphereObjectRenderer {get; private set;}
	public MeshRenderer capsuleObjectRenderer {get; private set;}

	// Assumes uniform scale
	public float capsuleHeight {get {return capsuleCollider.height * transform.lossyScale.x;}}
	public float capsuleRadius {get {return capsuleCollider.radius * transform.lossyScale.x;}}
	public float sphereRadius {get {return sphereCollider.radius * transform.lossyScale.x;}}

	public LayerMask collisionLayers;
	public List<Component> ignoreColliders = new List<Component>();

	const float maxRadiusMoveDivider = 3f;
	float maxRadiusMove {get {return sphereRadius / maxRadiusMoveDivider;}}
	public const float tinyOffset = .0001f;
	public const float smallOffset = .002f;

	float maxSpeed = 20;
	float maxVerticalVelocity = 30;
	float absoluteMaxVelocity = 45;

	public Vector3 velocity;
	public Vector3 fallVelocity;
	public Vector3 facingDir;
	public Vector3 additionalVelocity = Vector3.zero;
	public Vector3 objectAliginingVelocity = Vector3.zero;

	private Vector3 lastSafePos = Vector3.zero;

	public GroundInfo groundInfo = new GroundInfo();

	public enum GroundType
	{
		Normal, Ice
	}

	public enum MoveMode
	{
		Default, Gliding
	}
	public MoveMode moveMode;

	public bool isJumping { get; set; }
	protected bool justStartedJumping;
	protected bool ignoreSlopePhysics = true;
	public bool leavingGround = false;
	public List<NormalInfo> hitNormals = new List<NormalInfo> ();
	public float maxStepHeight {get {return sphereRadius / 2.2f;}}
	float wallAngleZeroAngle = 20;

	protected Vector3 gravityDir;


	protected void DoAwake () {

		base.DoAwake ();

		playerActionsController = GetComponent<PlayerActionsController> ();
		pic = GetComponent <PlayerInteractionsController> ();

		capsuleCollider = capsuleObject.GetComponent <CapsuleCollider>();
		capsuleObjectRenderer = capsuleObject.GetComponent <MeshRenderer> ();

		sphereCollider = sphereObject.GetComponent <SphereCollider>();
		sphereObjectRenderer = sphereObject.GetComponent <MeshRenderer>();

		currentCollider = sphereCollider;
		//currentCollider = capsuleCollider;
		//sphereObjectRenderer.enabled = true;
		sphereObjectRenderer.enabled = false;
		capsuleObjectRenderer.enabled = false;

		//if(!ignoreColliders.Contains(sphereCollider)) ignoreColliders.Add(sphereCollider);
		//if(!ignoreColliders.Contains(capsuleCollider)) ignoreColliders.Add(capsuleCollider);

		//subStepUpdater is needed for framerate indepenetent movement consistency 
		//Keep in mind that this is for variable timesteps. If you use FixedUpdate than you wont have to worry about this.
		//collisionHandleInfo.subStepUpdater.subStepMethod = HandleMovementForces;
	}


	protected void DoStart () {

		base.DoStart ();
		depenetratable = true;
		currentlySimulatable = true;
		facingDir = transform.forward;
		groundInfo.wasGroundedBefore = false;

		CollisionTests.SetInitialParameters(sphereRadius, maxRadiusMoveDivider, collisionLayers);
	}


	public override void DoUpdate()
	{
		base.DoUpdate ();

		if (sc.gameController.debugMode)
        {
			if (!sphereObjectRenderer.enabled)
            {
				sphereObjectRenderer.enabled = true;

			}
        }
		else
        {
			if (sphereObjectRenderer.enabled)
			{
				sphereObjectRenderer.enabled = false;
			}
		}
	}

	public override void DoFinalUpdate ()
	{
		base.DoFinalUpdate ();
	}


	public override void DoPreCollisionUpdate (float deltaTime)
	{
		DoPlayerInputUpdate(deltaTime);

		if (sc.debugModeType != ActionSceneController.DebugModeType.Player)
        {
			// Set the gravityDir for this update
			gravityDir = sc.gravityDir;
			groundInfo.gravityDir = gravityDir;

			PreCollisionControl(deltaTime);
		}
	}

	protected virtual void DoPlayerInputUpdate (float deltaTime)
    {
		// Overrided by ActionsController
    }

	public override void DoPostCollisionUpdate (float deltaTime)
	{
		if (sc.debugModeType != ActionSceneController.DebugModeType.Player)
        {
			PostCollisionControl(deltaTime);
		}	
	}

	public override bool DoCollisionUpdate (float deltaTime, Vector3 stepVelocity)
	{
		if (sc.debugModeType != ActionSceneController.DebugModeType.Player)
        {
			DoCollisionIteration(deltaTime, stepVelocity);


			if (!collisionInfo.collisionSuccessful)
			{
				//Debug.Log ("Aborting collision");
				return false;
			}

			return true;
		}

		return true;
	}
	


	void ChangeCollider() {

		if (currentCollider is SphereCollider) 
		{
			SetCollider (capsuleCollider);
		} 
		else if (currentCollider is CapsuleCollider) 
		{
			SetCollider (sphereCollider);
		}
	}

	void SetCollider (Collider newCol) {

		if (newCol is SphereCollider && !(currentCollider is SphereCollider)) 
		{
			currentCollider = newCol;
			capsuleObjectRenderer.enabled = false;
			sphereObjectRenderer.enabled = true;
		} 
		else if (newCol is CapsuleCollider && !(currentCollider is CapsuleCollider)) 
		{
			currentCollider = newCol;
			sphereObjectRenderer.enabled = false;
			capsuleObjectRenderer.enabled = true;
		}
	}


	protected virtual void PreCollisionControl (float deltaTime) {}
	protected virtual void PostCollisionControl (float deltaTime) {}



	public override void PrepareForCollision (float deltaTime)
	{
		collisionInfo = new CollisionInfo();
		collisionInfo.origin = transform.position;
		hitNormals.Clear();

		//velocity = ConstrainVelocity (velocity);

		// Note: groundInfo.up is continuously live and doesn't need resetting

		// Set any moving platform velocity
		GetAndSetMovingPlatformVelocity(deltaTime);

		// Only do this once at the start because these velocities shouldn't change after steps
		SetExtraCollisionVelocity((additionalVelocity + objectAliginingVelocity));

		// Do this once at the start
		collisionInfo.velocity = velocity;

		SetNormalCollisionVelocity(collisionInfo.velocity * deltaTime);
	}

	public Vector3 ConstrainVelocity (Vector3 v)
	{
		float sqrmag = velocity.sqrMagnitude;

		if (sqrmag > absoluteMaxVelocity*absoluteMaxVelocity) 
		{
			return absoluteMaxVelocity * v.normalized;
		}

		return v;
	}

	public override void FinalizeAfterCollisionStep (float deltaTime)
    {
		// Set any moving platform velocity
		GetAndSetMovingPlatformVelocity(deltaTime);

		SetNormalCollisionVelocity(collisionInfo.velocity * deltaTime); //This velocity should be the edited velocity based on the collision
		SetStepVelocity();
	}

	public override void FinalizeAfterCollision (float deltaTime)
	{
		if (collisionInfo.collisionSuccessful)
		{
			
		}

		CollisionController.WallInfo wallInfo = CollisionController.WallCollision(false, transform.position, groundInfo.up, collisionInfo.velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, groundInfo.GetIsGrounded(), maxRadiusMove, sc.gameController.debugMode);
		collisionInfo.wallPoints = new List<GroundCastInfo>();
		collisionInfo.wallPoints = wallInfo.wallDepenPoints;

		// Set the actual transform rotation at the end
		SetGroundPivot(Quaternion.FromToRotation(Vector3.up, groundInfo.up));

		additionalVelocity = Vector3.zero;
		objectAliginingVelocity = Vector3.zero;
		velocity = collisionInfo.velocity;
	}


	public void MovePlayerAfterCollision (float deltaTime)
	{
		//velocity = Constrain(velocity);



		//collisionInfo.safeMoveDirection = Constrain(collisionInfo.safeMoveDirection); //Doing this is probably not safe since it might be required to move to depenetrate properly.
		//collisionInfo.velocity = Constrain(collisionInfo.velocity);

		//transform.Translate (velocity);

		if (collisionInfo.collisionSuccessful) 
		{
			transform.Translate(collisionInfo.safeMoveDirection, Space.World);
			lastSafePos = transform.position;

			//Debug.Log ("x: " + transform.position.x + ", y: " + transform.position.y + ", z: " + transform.position.z);
		} 
		else 
		{
			transform.position = lastSafePos;
			//collisionInfo.velocity = Vector3.zero;
		}


		collisionInfo.origin = transform.position;
		//Debug.Log (groundInfo.isOnEdge);
	}

	public void DoCollisionIteration (float deltaTime, Vector3 stepVelocity)
    {
		Vector3 previousOrigin = collisionInfo.origin;
		collisionInfo.origin += stepVelocity;
		collisionInfo.targetPosition = collisionInfo.origin;

		Vector3 origin = collisionInfo.origin;

		for (collisionInfo.temporaryAttempts = 0; collisionInfo.temporaryAttempts < collisionHandleInfo.maxCollisionCheckIterations; collisionInfo.temporaryAttempts++)
		{
			bool wallDepenetrated = false;
			bool groundDepenetrated = false;
			//Debug.Log("iteration: " + collisionInfo.temporaryAttempts);

			// Do wall collision
			if ((collisionInfo.temporaryAttempts == 0 || groundDepenetrated) && false)
            {
				CollisionController.WallLoopInfo wallLoopInfo = CollisionController.WallLoop(origin, groundInfo.up, collisionInfo.velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, groundInfo.GetIsGrounded(), maxRadiusMove, wallAngleZeroAngle, sc.gameController.debugMode);
				origin = wallLoopInfo.newPosition;
				wallDepenetrated = wallLoopInfo.depenetrated;
				collisionInfo.velocity = wallLoopInfo.newVelocity;
			}

			// Do ground collision
			if (collisionInfo.temporaryAttempts == 0 || wallDepenetrated)
            {
				CollisionController.GroundingLoopInfo groundingLoopInfo = CollisionController.GroundingLoop(groundInfo, origin, collisionInfo.velocity, gravityDir, sphereRadius, maxRadiusMove, collisionLayers, ignoreColliders, sc.gameController.debugMode, ignoreSlopePhysics);

				// Use whatever grounding info we got
				groundInfo.previousUp = groundInfo.up;
				SetGrounded(groundingLoopInfo.groundingInfo.grounded);
				groundInfo.groundNormal = groundingLoopInfo.groundingInfo.groundNormal;
				groundInfo.canJump = groundingLoopInfo.groundingInfo.grounded;
				groundInfo.collider = groundingLoopInfo.groundingInfo.collider;
				groundInfo.staircaseNormal = groundingLoopInfo.groundingInfo.staircaseNormal;
				groundInfo.up = groundingLoopInfo.groundingInfo.groundNormal;

				origin = groundingLoopInfo.groundingInfo.newPosition;
				groundDepenetrated = groundingLoopInfo.groundingInfo.depenetrated;
				collisionInfo.velocity = groundingLoopInfo.newVelocity;

				if (groundingLoopInfo.iterationsDone > 5)
                {
					sc.debugInfoManager.CreateGroundingLoopResultDebug(sc.gameController.debugMode, groundingLoopInfo.allDebugInfo);
                }
			}

			if (groundDepenetrated || wallDepenetrated)
            {
				FinalizeAfterCollisionStep(deltaTime);
			}
			else
            {
				break;
			}

		}

		if (collisionInfo.temporaryAttempts > 1)
        {
			Debug.Log("main:" + collisionInfo.temporaryAttempts);
		}

		//Debug.Log("main:" + collisionInfo.temporaryAttempts);

		collisionInfo.origin = origin;
		collisionInfo.targetPosition = collisionInfo.origin;
		collisionInfo.collisionSuccessful = true;
		collisionInfo.safeMoveDirection = collisionInfo.targetPosition - transform.position;

		MovePlayerAfterCollision(deltaTime);
		playerActionsController.pic.CheckForInteractables(); //it is necessary to call this here or else we could pass through thin interactables without detecting them 
	}

	protected virtual void SetGroundPivot (Quaternion q) 
	{
		groundPivot.rotation = q;
	}


	Vector3 GetMovingPlatformVelocity (float deltaTime)
	{
		if (groundInfo.GetIsGrounded()) 
		{
			Collider movingPlatformCollider = groundInfo.collider;
			if (movingPlatformCollider != null) 
			{
				MovingPlatformController movingPlatform = movingPlatformCollider.gameObject.GetComponentInParent<MovingPlatformController> ();
				if (movingPlatform != null) 
				{
					//standing on a moving platform
					Vector3 platformVelocity = movingPlatform.velocity;
					//Debug.Log (platformVelocity/deltaTime);
					return platformVelocity;
					
				}
			}
		}
		return Vector3.zero;

	}


	public static VelocityAgainstWallsNormalsInfo SetWallsInfo (bool isReactive, Vector3 velocity, List<GroundCastInfo> wallPoints, Vector3 playerUp, Vector3 gravityDir)
	{
		VelocityAgainstWallsNormalsInfo info = new ();

		// Expects velocity to be local

		Vector3 lateralVelocityDir = new Vector3 (velocity.x, 0, velocity.z).normalized;

		if (lateralVelocityDir == Vector3.zero) 
		{
			info.minWallAngle = 1;
			info.minWallNormal = Vector3.zero;
			info.maxWallAngle = -1;
			info.maxWallNormal = Vector3.zero;
			info.averageWallNormal = Vector3.zero;

			return info;
		}

		float biggestPosAngle = -1;
		Vector3 normalOfBiggestPosAngle = Vector3.zero;
		float smallestNegAngle = 1;
		Vector3 normalOfSmallestNegAngle = Vector3.zero;


		for (int i = 0; i < wallPoints.Count; i++) 
		{
			// wallActingAsFloor as floor points were not used for depenetration but we still want to use them now
			if (isReactive && !(wallPoints[i].wallDepenetrated || wallPoints[i].wallActingAsFloor)) continue;
			// Ignore these points
			if (wallPoints[i].ignoreWallForVelocityLimiting) continue;

			Vector3 normal = wallPoints[i].wallDepenDir;

			if (CollisionController.CanWalkOnSlope(normal, playerUp, gravityDir, Vector3.zero))
			{
				continue;
			}

			normal = ExtVector3.InverseTransformDirection(playerUp, normal);

			Vector3 flattenedNormal = new Vector3 (normal.x, 0, normal.z);

			if (flattenedNormal == Vector3.zero) {
				continue;
			}

			if (!ExtVector3.IsInDirection (lateralVelocityDir, -flattenedNormal)) {
				continue;
			}


			float angleToVelocity = Vector3.SignedAngle (lateralVelocityDir, -flattenedNormal, playerUp);

			if (angleToVelocity >= 0 && angleToVelocity > biggestPosAngle) 
			{
				/*if (angleToVelocity >= 70 && flattenedNormal != flattenedInterpolated) 
				{
					//allow clipping on wall edges that barely face the vdir
					continue;
				}*/

				biggestPosAngle = angleToVelocity;
				normalOfBiggestPosAngle = flattenedNormal;
			}
				
			else if (angleToVelocity <= 0 && angleToVelocity < smallestNegAngle) 
			{
				/*if (angleToVelocity <= -70 && flattenedNormal != flattenedInterpolated) 
				{
					//allow clipping on wall edges that barely face the vdir
					continue;
				}*/

				smallestNegAngle = angleToVelocity;
				normalOfSmallestNegAngle = flattenedNormal;
			}
		}

		Vector3 averageNormal = (normalOfBiggestPosAngle + normalOfSmallestNegAngle).normalized;

		info.minWallAngle = smallestNegAngle;
		info.minWallNormal = normalOfSmallestNegAngle;
		info.maxWallAngle = biggestPosAngle;
		info.maxWallNormal = normalOfBiggestPosAngle;
		info.averageWallNormal = averageNormal;
		info.infoSet = averageNormal != Vector3.zero;

		return info;
	}
		

	public static Vector3 GetWallNormalInDir (VelocityAgainstWallsNormalsInfo info)
	{
		Vector3 w = Vector3.zero;

		if (info.minWallAngle != 1 && info.maxWallAngle != -1) 
		{
			return w;
		} 
		else 
		{
			if (info.minWallAngle != 1) 
			{
				w = info.minWallNormal;
			} 
			else if (info.maxWallAngle != -1) 
			{
				w = info.maxWallNormal;
			}
		}

		return w;
	}


	public static float GetAngleBetweenDirAndWall (Vector3 v, VelocityAgainstWallsNormalsInfo info, Vector3 playerUp)
	{
		// Expects velocity to be local

		float angle = 0;
		
		if (info.minWallAngle != 1 && info.maxWallAngle != -1) 
		{
			angle = 0;
		} 
		else 
		{
			if (info.minWallAngle != 1) 
			{
				angle = Vector3.SignedAngle (-info.minWallNormal, v.normalized, playerUp);
			} 
			else if (info.maxWallAngle != -1) 
			{
				angle = Vector3.SignedAngle (-info.maxWallNormal, v.normalized, playerUp);
			}
		}

		return angle;
	}

	public static Vector3 LimitVelocityOnWalls (Vector3 velocity, VelocityAgainstWallsNormalsInfo info, float maxAngleStopTolerance, bool zeroOnSmallAngles = true)
	{
		// Expects velocity to be local

		if (info.minWallAngle != 1 && info.maxWallAngle != -1) 
		{
			// If there is a wall on either side then we know we are entering a v shape, so just kill all lateral velocity
			Vector3 vertical = new Vector3 (0, velocity.y, 0);
			velocity = vertical;
		} 
		else 
		{
			if (info.minWallAngle != 1) 
			{
				if (info.minWallAngle >= -maxAngleStopTolerance) 
				{
					if (zeroOnSmallAngles) 
					{
						Vector3 vertical = new Vector3 (0, velocity.y, 0);
						velocity = vertical;
					} 
					else 
					{
						velocity = Vector3.ProjectOnPlane (velocity, info.minWallNormal);
					}

				} 
				else 
				{
					velocity = Vector3.ProjectOnPlane (velocity, info.minWallNormal);
				}
			} 
			else if (info.maxWallAngle != -1) 
			{
				if (info.maxWallAngle <= maxAngleStopTolerance) 
				{
					if (zeroOnSmallAngles) 
					{
						Vector3 vertical = new Vector3 (0, velocity.y, 0);
						velocity = vertical;
					} 
					else 
					{
						velocity = Vector3.ProjectOnPlane (velocity, info.maxWallNormal);
					}

				} 
				else 
				{
					velocity = Vector3.ProjectOnPlane (velocity, info.maxWallNormal);
				}
			}
		}

		return velocity;
	}


	protected bool CheckOverlaps (Collider collider, Vector3 origin, Vector3 transformUp, float height, float radius, List<Component> ignoreColliders, LayerMask mask) {

		if (collider is CapsuleCollider) return ExtPhysics.CheckCapsule (origin, transformUp, height, radius, ignoreColliders, mask); 
		else if (collider is SphereCollider) return ExtPhysics.CheckSphere (origin, radius, ignoreColliders, mask);

		return false;
	}

	protected bool FinalOverlapCheck(Vector3 origin, Vector3 transformUp, float height, float radius, List<Component> ignoreColliders, LayerMask mask) {

		if (currentCollider is SphereCollider) 
		{
			return ExtPhysics.CheckSphere (origin, radius, ignoreColliders, mask);
		} 
		else if (currentCollider is CapsuleCollider) 
		{
			return ExtPhysics.CheckCapsule (origin, transformUp, height, radius, ignoreColliders, mask);
		}

		return false;
	}


	public GroundingEdgeCollisionInfo GetEdgeInfo (Collider col, Vector3 point, Vector3 realNormal, Vector3 interpolatedNormal, Vector3 velocity)
	{
		return GroundingSphereCollisionDetect.DetectEdgeCollisions (col, point, 0.0001f, realNormal, interpolatedNormal, groundInfo.up, gravityDir, velocity, 0);

	}
	

	Vector3 ClampVelocity(Vector3 velocity)
	{
		/*if (Mathf.Abs (velocity.x) < 0.001f)
			velocity.x = 0;
		if (Mathf.Abs (velocity.y) < 0.001f)
			velocity.y = 0;
		if (Mathf.Abs (velocity.z) < 0.001f)
			velocity.z = 0;*/

		velocity = ClampVerticalVelocity(velocity, groundInfo.up, maxVerticalVelocity);
		velocity = ClampHorizontalVelocity(velocity, groundInfo.up, maxSpeed);
		return velocity;
	}
		

	public static Vector3 ClampHorizontalVelocity(Vector3 velocity, Vector3 transformUp, float maxVelocity)
	{
		Vector3 horizontal, vertical;
		GetVelocityAxis(velocity, transformUp, out horizontal, out vertical);
		return vertical + Vector3.ClampMagnitude(horizontal, maxVelocity);
	}


	public static Vector3 ClampVerticalVelocity(Vector3 velocity, Vector3 transformUp, float maxVelocity)
	{
		Vector3 horizontal, vertical;
		GetVelocityAxis(velocity, transformUp, out horizontal, out vertical);
		return horizontal + Vector3.ClampMagnitude(vertical, maxVelocity);
	}


	public static void GetVelocityAxis(Vector3 velocity, Vector3 transformUp, out Vector3 horizontal, out Vector3 vertical)
	{
		vertical = transformUp * Vector3.Dot (velocity, transformUp);
		horizontal = velocity - vertical;
	}


	Vector3 GetCapsulePoint(Vector3 origin, Vector3 direction)
	{
		return origin + (direction * (CapsulePointsDistance() * .5f));
	}

	float CapsulePointsDistance()
	{
		return CapsuleShape.PointsDistance(capsuleHeight, capsuleRadius);
	}


	public static Vector3 LimitVelocityOnCeiling (Vector3 velocity, Vector3 gravityDir, List<GroundCastInfo> wallPoints)
	{
		for (int i = 0; i < wallPoints.Count; i++) 
		{
			GroundCastInfo pointInfo = wallPoints[i];

			if (!pointInfo.wallDepenetrated) continue;

			// wallDepenDir will have been edited to interpolatedNormal if iterations done was high enough
			Vector3 normalToUse = pointInfo.wallDepenDir;

			if (ExtVector3.IsInDirection(velocity, -normalToUse, tinyOffset, true))
			{
				if (ExtVector3.IsInDirection (normalToUse, gravityDir) && Vector3.Angle (normalToUse, gravityDir) < 89.9) 
				{
					Debug.Log("ceiling!");
					return Vector3.ProjectOnPlane(velocity, gravityDir);
				}
			}
		}

		return velocity;
	}


	public static Vector3 LimitVelocityOnStuckGround (Vector3 velocity, Vector3 gravityDir, List<GroundCastInfo> wallPoints, int depenIterationsDone, bool grounded)
	{
		for (int i = 0; i < wallPoints.Count; i++)
		{
			GroundCastInfo pointInfo = wallPoints[i];

			// This is the number at which we start using the interp normal rather than flattened wall normal and when these kind of stuck between ground edges will exist
			if (depenIterationsDone >= 10 && !grounded)
            {
				Vector3 normalToUse = pointInfo.GetInterpolatedNormal();

				if (ExtVector3.IsInDirection(velocity, -normalToUse, tinyOffset, true))
				{
					if (ExtVector3.IsInDirection(normalToUse, -gravityDir) && Vector3.Angle(normalToUse, -gravityDir) < 89.9)
					{
						Debug.Log("stuck ground!");
						return Vector3.ProjectOnPlane(velocity, gravityDir);
					}
				}
			}
		}

		return velocity;
	}

	void GetAndSetMovingPlatformVelocity (float deltaTime)
    {
		Vector3 movingPlatformVelocity = GetMovingPlatformVelocity(deltaTime);
		SetMovingPlatformVelocity(movingPlatformVelocity);
	}

	public IEnumerator LeftGroundTimer ()
	{
		leavingGround = true;
		float timer = 0;

		while (true) 
		{
			while (sc.gamePaused || sc.gameController.frameByFrame) 
			{
				yield return new WaitForEndOfFrame ();
			}

			timer += Time.deltaTime;

			if (timer >= 0.3f) 
			{
				leavingGround = false;
				break;
			}

			yield return new WaitForEndOfFrame ();
		}
	}




	#region Debug
	void DrawContactsDebug(List<GroundingSphereCollisionInfo> collisionPoints, float size, Color planeColor, Color rayColor)
	{
		//if(infoDebug.drawContacts)
		//{
			for(int i = 0; i < collisionPoints.Count; i++)
			{
				//ExtDebug.DrawPlane(collisionPoints[i].closestPointOnSurface, collisionPoints[i].interpolatedNormal, .5f, planeColor, infoDebug.drawContactsDuration);
				//Debug.DrawRay(collisionPoints[i].closestPointOnSurface, collisionPoints[i].normal, rayColor, infoDebug.drawContactsDuration);
			Debug.DrawRay(collisionPoints[i].closestPointOnSurface, collisionPoints[i].interpolatedNormal, rayColor, Mathf.Infinity);
				//Debug.DrawRay(collisionPoints[i].closestPointOnSurface - Vector3.up, Vector3.up, Color.black, Mathf.Infinity);
				
				//Debug.Log (collisionPoints[i].normal);
			}
		//}
	}

	void DrawContactsDebug(GroundingSphereCollisionInfo collisionPoint, float size, Color planeColor, Color rayColor)
	{
		Debug.DrawRay(collisionPoint.closestPointOnSurface, collisionPoint.normal, rayColor, Mathf.Infinity);
		Debug.DrawRay(collisionPoint.closestPointOnSurface - Vector3.up, Vector3.up, Color.black, Mathf.Infinity);

		//Debug.Log (collisionPoint.normal);
	}

	void DrawGroundDebug(Vector3 hitPoint, Vector3 normal, float size, Color planeColor, Color rayColor)
	{
		if(Input.GetKeyDown (KeyCode.O))
		{
			//ExtDebug.DrawPlane(hitPoint, normal, size, planeColor);
			Debug.DrawRay(hitPoint, normal, rayColor, Mathf.Infinity);
			//Debug.Log (normal);
		}
	}
	#endregion	



	public struct GroundInfo
	{
		private bool isGrounded;
		public bool isPartiallyGrounded;
		public bool wasGroundedBefore;
		public bool isOnStep;
		public Vector3 staircaseNormal;
		public Vector3 groundNormal;
		public Vector3 up;
		public Vector3 previousUp;
		public Vector3 gravityDir;
		public bool canJump;
		public bool isGoingTowardsEdge;
		public bool previouslyWall;
		public GroundCastInfo groundCastInfo;
		public GroundType groundType;
		public Collider collider;

		public void Set(bool isPartiallyGrounded, bool wasGroundedBefore, GroundCastInfo groundCastInfo, bool isOnStep, bool isGoingTowardsEdge, bool previouslyWall) 
		{
			this.groundCastInfo = groundCastInfo;
			this.wasGroundedBefore = wasGroundedBefore;
			this.isOnStep = isOnStep;
			this.isGoingTowardsEdge = isGoingTowardsEdge;
			this.previouslyWall = previouslyWall;
			this.isPartiallyGrounded = isPartiallyGrounded;

			SetGroundType ();
		}


		public void SetGrounded(bool b)
        {
			isGrounded = b;

			if (!b)
            {
				previousUp = up;
				up = -gravityDir;
				isPartiallyGrounded = false;
            }
        }

		public void SetGroundType ()
		{
			if (groundCastInfo.collider != null) 
			{
				PhysicMaterial material = groundCastInfo.collider.sharedMaterial;

				if (material != null) 
				{
					if (material.name == "Ice") 
					{
						this.groundType = GroundType.Ice;
					}
				} 
				else 
				{
					this.groundType = GroundType.Normal;
				}
			}
			else 
			{
				this.groundType = GroundType.Normal;
			}
		}


		public bool GetIsGrounded (bool disallowPartial = false)
        {
			if (disallowPartial)
            {
				return isGrounded;
            }
			else
            {
				return isGrounded || isPartiallyGrounded;
            }
        }

		public bool GetIsOnStaircase ()
        {
			return staircaseNormal != Vector3.zero;
        }



	}


	public void SetGrounded(bool newGrounded, bool partiallyGrounded = false) 
	{
		if (!newGrounded) 
		{
			if (groundInfo.GetIsGrounded()) 
			{
				// Leaving ground
				groundInfo.canJump = false;

				if (cLeftGroundTimer != null)
					StopCoroutine (cLeftGroundTimer);
				cLeftGroundTimer = LeftGroundTimer ();
				StartCoroutine (cLeftGroundTimer);
			}

			groundInfo.isOnStep = false;
		}
		else
        {
			isJumping = false;
        }

		groundInfo.SetGrounded(newGrounded);
	}

	public float GetMaxSpeed()
    {
		return maxSpeed;
    }

	public float GetWallAngleZero()
    {
		return wallAngleZeroAngle;
    }
	public struct NormalInfo
	{
		public Vector3 realNormal;
		public Vector3 interpolatedNormal;

		public void Set (Vector3 rn, Vector3 inn) 
		{
			realNormal = rn;
			interpolatedNormal = inn;
		}
	}


	public struct DepenetrationInfo
	{
		public Vector3 totalDepenetration;
		public List<SphereCollisionDetect.CollisionPointInfo> pointsInfo;
		public Vector3 averageNormal;
		public bool foundWalkableGroundNormal;

		public void Initialize ()
		{
			pointsInfo = new List <SphereCollisionDetect.CollisionPointInfo> ();
		}
	}

	public struct VelocityAgainstWallsNormalsInfo
    {
		public bool infoSet;
		public float minWallAngle;
		public Vector3 minWallNormal;
		public float maxWallAngle;
		public Vector3 maxWallNormal;
		public Vector3 averageWallNormal;
    }
		

	[Serializable]
	public class CollisionHandleInfo
	{
		public int maxCollisionCheckIterations = 10; //On average it runs 2 to 3 times, but on surfaces with opposing normals it could run much more.
		public int maxDepenetrationIterations = 10;
		public int maxVelocitySteps = 20; //A safety in case we are moving very fast we dont want to divide our velocity into to many steps since that can cause lag and freeze the game, so we prefer to have the collision be unsafe.
		public bool abortIfFailedThisFrame = true; //Prevents us from constantly trying and failing this frame which causes lots of lag if using subUpdater, which would make subUpdater run more and lag more...
		public bool tryBlockAtSlopeLimit = true;
		public bool cleanByIgnoreBehindPlane;
		public bool depenetrateEvenIfUnsafe;
	}
}
