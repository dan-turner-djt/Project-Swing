using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerPhysicsController : PhysicsSimulatable {

	PlayerActionsController playerActionsController;
	public PlayerInteractionsController pic;

	CollisionHandleInfo collisionHandleInfo = new CollisionHandleInfo ();


	//coroutines
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

	//Assumes uniform scale.
	public float capsuleHeight {get {return capsuleCollider.height * transform.lossyScale.x;}}
	public float capsuleRadius {get {return capsuleCollider.radius * transform.lossyScale.x;}}
	public float sphereRadius {get {return sphereCollider.radius * transform.lossyScale.x;}}

	public LayerMask collisionLayers;
	public List<Component> ignoreColliders = new List<Component>();

	const float maxRadiusMoveDivider = 3f;
	float maxRadiusMove {get {return sphereRadius / maxRadiusMoveDivider;}}
	public const float tinyOffset = .0001f;
	public const float smallOffset = .002f; //.002f
	const float maxGroundCheckOffset = 0.2f;
	const float minGroundCheckOffset = 0;//.008f;
	float groundCheckOffset;
	//const float groundOffset = 0.006f;//.006f; //This value should be at least .004 and less than groundCheckOffset by at least .002 to be safe
	//We need a safeCheckOffset for our collision detection so that if a normal causes us to move into another wall,
	//we would have already had that wall detected and were able to take it into account in our depenetration. Too large of a value would cause issues.
	const float safeCheckOffset = 0.04f;//.02f; //should be greater (LESS SEEMS TO STOP IT DETECTING GROUND (GOOD), BUT 0 BREAKS THE COLLISION) than groundOffset

	public float AccellShiftOverSpeed;
	public float TangentialDragShiftOverSpeed;
	public float TangentialDrag;
	public float friction;
	public float maxSpeed;
	public float maxVerticalVelocity;
	public float absoluteMaxVelocity;

	public float moveTowardSlopeSpeed;
	public float moveAwayFromSlopeSpeed;


	public Vector3 velocity;
	public Vector3 fallVelocity;
	public Vector3 facingDir;
	public Vector3 gravityDir = -Vector3.up;//Vector3.right;// 
	public Vector3 additionalVelocity = Vector3.zero;
	public Vector3 objectAliginingVelocity = Vector3.zero;
	public Vector3 currentForcesWithDeltas {get; set;}
	public Vector3 currentSubForcesWithDeltas {get; set;}

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



	public AnimationCurve AccellOverSpeed;
	public AnimationCurve TangDragOverSpeed;
	public float curvePosAcell { get; set; }
	public float curvePosTang { get; set; }
	public float curvePosSlope { get; set; }

	public bool isJumping { get; set; }
	public bool ignoreSlopePhysics = false;
	public bool leavingGround = false;
	public float stickToSlopeThreshold;
	public List<NormalInfo> hitNormals = new List<NormalInfo> ();
	public float maxStepHeight {get {return sphereRadius / 2.2f;}}
	public float wallAngleZeroAngle;


	protected void DoAwake () {

		base.DoAwake ();

		playerActionsController = GetComponent<PlayerActionsController> ();
		pic = GetComponent <PlayerInteractionsController> ();

		capsuleCollider = capsuleObject.GetComponent <CapsuleCollider>();
		capsuleObjectRenderer = capsuleObject.GetComponent <MeshRenderer> ();

		sphereCollider = sphereObject.GetComponent <SphereCollider>();
		sphereObjectRenderer = sphereObject.GetComponent <MeshRenderer>();

		currentCollider = sphereCollider;
		sphereObjectRenderer.enabled = true;
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

		//SetStepCheckLateralDist (); //we are just doing this once to begin with because the sqrt is expensive (note: needs to be recalculated if the radius changes)
	}


	public override void DoUpdate()
	{
		base.DoUpdate ();
	}

	public override void DoFinalUpdate ()
	{
		base.DoFinalUpdate ();
	}


	public override void DoPreCollisionUpdate (float deltaTime)
	{
		PreCollisionControl(deltaTime);
	}

	public override void DoPostCollisionUpdate (float deltaTime)
	{
		PostCollisionControl (deltaTime);
	}

	public override bool DoCollisionUpdate (float deltaTime, Vector3 stepVelocity)
	{
		DoCollisionIteration (deltaTime, stepVelocity);


		if (!collisionInfo.collisionSuccessful) 
		{
			//Debug.Log ("Aborting collision");
			return false;
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
		velocity = ConstrainVelocity (velocity);

		//adds any platform velocity to additionalVelocity
		GetMovingPlatformVelocity (deltaTime);

		Vector3 constrainedVelocity = ConstrainVelocity (velocity);
		Vector3 totalVelocity = velocity * deltaTime + additionalVelocity + objectAliginingVelocity; 

		collisionVelocity = totalVelocity;


		collisionInfo = new CollisionInfo();
		collisionInfo.pointsInfo = new List<SphereCollisionDetect.CollisionPointInfo> ();
		collisionInfo.origin = transform.position;
		collisionInfo.targetPosition = collisionInfo.origin + collisionVelocity;
		Vector3 previousHitNormal = Vector3.zero;
		hitNormals.Clear ();
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


	public override void FinalizeAfterCollision (float deltaTime)
	{
		if(collisionInfo.collisionSuccessful)
		{
			//We handle redirecting our velocity. First we just default it to the targetVelocity, and not include the additional velocity added because it should be one-time only.
			collisionInfo.velocity = velocity*deltaTime;
			Vector3 previousFacingDir = facingDir;

			Vector3 oldUp = groundPivot.up;
			Vector3 oldLocal = groundPivot.InverseTransformDirection (collisionInfo.velocity);
			Vector3 previousLateralVelocity = new Vector3 (oldLocal.x, 0, oldLocal.z);

			//previousLateralVelocity = Vector3.ProjectOnPlane (collisionInfo.velocity, groundPivot.up);
			Vector3 savedVerticalVelocity = collisionInfo.velocity - previousLateralVelocity;

			//Debug.Log ("collision attempts: " + collisionInfo.attempts);

			//leftGroundTimer is to force a delay between jumping and regrounding 
			if (!(leavingGround && ExtVector3.MagnitudeInDirection (collisionInfo.velocity, -gravityDir) > 0)) 
			{
				Grounding (collisionInfo.foundWalkableGroundNormal, collisionInfo.origin, collisionInfo.origin, collisionLayers, deltaTime, velocity);
				//Debug.Log (groundInfo.isGrounded);
			} 

			if (groundInfo.GetIsGrounded())
			{

				float currentSlopeAngle = Vector3.Angle (-gravityDir, groundPivot.up);


				if (!groundInfo.wasGroundedBefore) //hit from in air
				{
					Vector3 moveInput = playerActionsController.playerInput.GetRawInput();
					float slopeAngle = 0;
					SlopeInfo.SlopeType slopeType = SlopeInfo.SlopeType.None;
					Vector3 slopeDir = Vector3.zero;
					float downSlopeAcceleration = 0;
					float downSlopeMod = 0;

					if (!ignoreSlopePhysics) 
					{
						slopeAngle = Vector3.Angle (-gravityDir, groundPivot.up);
						slopeType = SlopeInfo.GetSlopeType (slopeAngle);
						slopeDir = groundPivot.InverseTransformDirection (gravityDir);
						slopeDir.y = 0;
						slopeDir = slopeDir.normalized;

						downSlopeMod = 1 - Mathf.Max (0, ExtVector3.MagnitudeInDirection (groundPivot.up, -gravityDir));  //angles greater than 90 return 1
					}

					if (SlopeInfo.IsSlopeSteepOrUp (slopeAngle)) 
					{
						
					} 
					else if (slopeType == SlopeInfo.SlopeType.Shallow) 
					{
						collisionInfo.velocity = Vector3.ProjectOnPlane (collisionInfo.velocity, gravityDir);
					}
					else if (slopeType == SlopeInfo.SlopeType.Moderate) 
					{

						if (moveInput != Vector3.zero) 
						{
							collisionInfo.velocity = Vector3.ProjectOnPlane (collisionInfo.velocity, gravityDir);
						}


					}

					Vector3 localVelocity = groundPivot.InverseTransformDirection (collisionInfo.velocity);
					Vector3 verticalVelocity = new Vector3 (0, localVelocity.y, 0);
					Vector3 lateralVelocity =  new Vector3 (localVelocity.x, 0, localVelocity.z);

				}
				else  //hit and already on ground
				{
					collisionInfo.velocity = groundPivot.TransformDirection(previousLateralVelocity);

					if (groundInfo.previouslyWall)
					{
						//if we returned to the ground from a non-gravity walkable wall, then limit the speed against that normal first
						//collisionInfo.velocity = Vector3.ProjectOnPlane(collisionInfo.velocity, oldUp);
						collisionInfo.velocity = Vector3.zero;
						//Debug.Log("done it");
					}
				}

			}
			else 
			{
				if (groundInfo.wasGroundedBefore) //first update after leaving the ground
				{
					

				}

			}
				

			//do speed limiting on walls

			bool hitWall = SetWallsInfo (groundPivot.InverseTransformDirection (collisionInfo.velocity), false);
			if (hitWall) 
			{
				Vector3 lateralVelocity = groundPivot.InverseTransformDirection (collisionInfo.velocity);
				Vector3 vertical = new Vector3 (0, lateralVelocity.y, 0);
				lateralVelocity.y = 0;

				float speedLimit = 12;
				bool zeroedWall = false;
				Vector3 vBefore = lateralVelocity;
	
				Vector3 wallNormal = GetWallNormalInDir (lateralVelocity.normalized).normalized;

				if (wallNormal == Vector3.zero) 
				{
					//v shape
					zeroedWall = true;
					lateralVelocity = Vector3.zero;
					speedLimit = 0;
				} 
				else 
				{
					float mod = 1 - ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, -wallNormal, true);

					if (Vector3.Angle (lateralVelocity.normalized, -wallNormal) <= wallAngleZeroAngle) 
					{
						mod = 0;
					}

					speedLimit = speedLimit * Mathf.Pow (mod, 1.2f);

					lateralVelocity = vBefore.magnitude * Vector3.ProjectOnPlane (lateralVelocity, wallNormal).normalized;
				}
					
				float currentSpeed = lateralVelocity.magnitude;
				if (currentSpeed/deltaTime > speedLimit && speedLimit != 12) 
				{
					float slopeAngle = Vector3.Angle (-gravityDir, groundPivot.up);
					Vector3 slopeDir = groundPivot.InverseTransformDirection (gravityDir);
					slopeDir.y = 0;
					slopeDir = slopeDir.normalized;

					if (SlopeInfo.IsSlopeSteepOrUp(slopeAngle) && slopeDir != Vector3.zero) //on a steep slope
					{
						int direction = playerActionsController.IsMovingInFacingDir(lateralVelocity)? 1 : -1;
						if (Vector3.Angle (slopeDir, -wallNormal) <= wallAngleZeroAngle || Vector3.Angle (lateralVelocity, slopeDir) >= 90 || true) 
						{
							currentSpeed = speedLimit*deltaTime;
						}
					} 
					else 
					{
						currentSpeed = speedLimit*deltaTime;
					}



				}

				lateralVelocity = currentSpeed * lateralVelocity.normalized;
				collisionInfo.velocity = groundPivot.TransformDirection (lateralVelocity + vertical);
			} 


			if (!groundInfo.GetIsGrounded()) 
			{
				bool hitCeiling = CheckForCeiling (collisionInfo.velocity);

				if (hitCeiling) 
				{
					//max zero vertical speed if ceiling is flat enough
					float vertical = ExtVector3.MagnitudeInDirection(collisionInfo.velocity, -gravityDir);
					float newVertical = Mathf.Min(0, vertical);
					collisionInfo.velocity = collisionVelocity - (vertical + newVertical) * -gravityDir;
					leavingGround = false;
					if (cLeftGroundTimer != null)
						StopCoroutine (cLeftGroundTimer);
				}
			}


				
			if (groundInfo.GetIsGrounded()) 
			{
				collisionInfo.velocity = Vector3.ProjectOnPlane (collisionInfo.velocity, groundPivot.up);
				//playerActionsController.SetNewFacingDir (groundPivot.InverseTransformDirection (collisionInfo.velocity));
			}

		}

		groundInfo.wasGroundedBefore = groundInfo.GetIsGrounded(); //set this up for next update

		additionalVelocity = Vector3.zero;
		objectAliginingVelocity = Vector3.zero;
		velocity = collisionInfo.velocity / deltaTime;

		//Debug.Log(Vector3.ProjectOnPlane(velocity, groundPivot.up).magnitude);

		//playerActionsController.SetNewFacingDir ();

		//Debug.Log (ExtVector3.PrintFullVector3 (velocity));
		//Debug.Log(velocity.magnitude);
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
			collisionInfo.velocity = Vector3.zero;
		}


		collisionInfo.origin = transform.position;
		//Debug.Log (groundInfo.isOnEdge);
	}


	List<SphereCollisionInfo> collisionPointBuffer = new List<SphereCollisionInfo>();
	public void DoCollisionIteration (float deltaTime, Vector3 stepVelocity)
	{
		Vector3 transformUp = groundPivot.up;
		Vector3 previousOrigin = collisionInfo.origin;
		collisionInfo.origin += stepVelocity;
		collisionInfo.targetPosition = collisionInfo.origin;
		float negativeOffset = 0f;

		//needed to make sure we can detect lower convex slopes, so we move down into them to force the collision to depentrate from them and detect them in the grounding afterwards
		Vector3 moveVector = Vector3.zero;
		if (!playerActionsController.isHooked)  //this will need to changed to work with being hooked and staying on the ground
        {
			moveVector = SearchForLowerConvexGround(collisionInfo.origin, collisionLayers, deltaTime);
		}
		
		//moveVector = moveVector.normalized * Mathf.Clamp (moveVector.magnitude, 0, maxRadiusMove/60); //if move inside too far the collision will fail
		collisionInfo.origin += moveVector;
		//Debug.Log(ExtVector3.PrintFullVector3(moveVector));

		for (collisionInfo.temporaryAttempts = 0; collisionInfo.temporaryAttempts < collisionHandleInfo.maxCollisionCheckIterations; collisionInfo.temporaryAttempts++) 
		{
			Vector3 hitNormal = Vector3.zero;
			bool hasHit = false;
			bool depenetrated = false;

			//It is important for us to have a negativeOffset, otherwise our collision detection methods might keep telling us we are penetrated...
			//if(attempts > 0 && attempts < collisionHandleInfo.addNegativeOffsetUntilAttempt) negativeOffset += -smallOffset;
			
			if (collisionInfo.temporaryAttempts > 0 && negativeOffset > -safeCheckOffset) negativeOffset += -smallOffset;


			if (CheckOverlaps (currentCollider, collisionInfo.origin, transformUp, capsuleHeight + (negativeOffset * 2f), sphereRadius + negativeOffset + 0.00001f, ignoreColliders, collisionLayers)) {

				//Debug.Log ("hit something");
				List<SphereCollisionInfo> collisionPoints = SphereCollisionDetect.DetectCollisions (currentCollider, collisionInfo.origin, collisionInfo.origin, transform.up, capsuleHeight, sphereRadius, sphereRadius + 0.00001f, collisionLayers, ignoreColliders, collisionPointBuffer, transformUp, safeCheckOffset);

				//Debug.Log (collisionPoints.Count);

				if (collisionPoints.Count > 0) 
				{
					
					//Not tested, but might be a good idea to use this if it works...
					//if(collisionHandleInfo.cleanByIgnoreBehindPlane) SphereCollisionDetect.CleanByIgnoreBehindPlane(collisionPoints);

					#region Debug
					#if UNITY_EDITOR

					if (Input.GetKeyDown (KeyCode.P)) 
					{
						//DrawContactsDebug (collisionPoints, .5f, Color.magenta, Color.green);
					}

					#endif
					#endregion

					//look for moving platforms
					for (int k = 0; k < collisionPoints.Count; k++) 
					{
						Collider col = collisionPoints[k].collider;
						if (col != null) 
						{
							MovingPlatformController movingPlatform = col.gameObject.GetComponentInParent<MovingPlatformController> ();
							if (movingPlatform != null) 
							{
								collisionInfo.foundMovingPlatform = true;
								break;
							}
						}
					}


					//We do the main depenetration method
					DepenetrationInfo depenInfo = SphereCollisionDetect.Depenetrate (this, collisionPoints, velocity, collisionHandleInfo.maxDepenetrationIterations);
					Vector3 depenetration = Vector3.ClampMagnitude (depenInfo.totalDepenetration, maxRadiusMove); //We clamp to make sure we dont depenetrate too much into possibly unsafe areas **this myay be risky but unconfirmed**

					collisionInfo.origin += depenetration;
					collisionInfo.foundWalkableGroundNormal = (depenInfo.foundWalkableGroundNormal) ? true : collisionInfo.foundWalkableGroundNormal;
					collisionInfo.pointsInfo.AddRange (depenInfo.pointsInfo);
					//collisionInfo.pointsInfo = depenInfo.pointsInfo;  //directly setting rather than adding means we only keep the points of the last step velocity loop through (less to loop through later)

					hitNormal = (depenetration != Vector3.zero) ? depenetration.normalized : hitNormal;


					//Final check if we are safe, if not then we just move a little and hope for the best.
					if (FinalOverlapCheck (collisionInfo.origin, transformUp, capsuleHeight + ((negativeOffset - smallOffset) * 2f), sphereRadius + (negativeOffset - smallOffset), ignoreColliders, collisionLayers)) 
					{
						Debug.Log ("Still not safe yet");
						//collisionInfo.origin += (depenInfo.averageNormal * smallOffset);
						//collisionInfo.origin += hitNormal * smallOffset;

						//Debug.Log (ExtVector3.PrintFullVector3 (depenInfo.averageNormal));

						depenetrated = true;
					}
					else
                    {
						depenetrated = false;
                    }


					hasHit = true;
				}
			}


			if (hasHit) 
			{
				collisionInfo.targetPosition = collisionInfo.origin;

				if (depenetrated)
                {
					//this hasHit is only set to true for the main collision, as if we set it true after a grounding collision, because of the ground offset, it will get stuck in a continous loop, so instead
					//we just set the target position directly in the grounding collison too, and it will break and take that target position if it proceeds to find no colliding walls afterwards
					
					//go to next iteration
					
					collisionInfo.attempts++;
					//previousHitNormal = hitNormal;
				}
				else
                {
					break;
                }

			} 
			else 
			{
				break;
			}



		}

		//Debug.Log ("total loops: " + collisionInfo.temporaryAttempts);

		if (collisionInfo.temporaryAttempts >= collisionHandleInfo.maxCollisionCheckIterations) 
		{
			Debug.Log ("Player collision has failed!");
			collisionInfo.hasCollided = true;
			collisionInfo.collisionSuccessful = false;

			if (collisionInfo.foundMovingPlatform) 
			{
				playerActionsController.pic.Die (PlayerInteractionsController.DeathType.Crush); //when collision fails lets just assume we were crushed and kill the player
			}


		}
		else 
		{
			collisionInfo.hasCollided = (collisionInfo.attempts > 0);
			collisionInfo.collisionSuccessful = true;
			collisionInfo.safeMoveDirection = collisionInfo.targetPosition - transform.position;

		}

		MovePlayerAfterCollision (deltaTime);	
		playerActionsController.pic.CheckForInteractables (); //it is necessary to call this here or else we could pass through thin interactables without detecting them 

		return;
	}


	protected virtual void SetGroundPivot (Quaternion q) 
	{
		groundPivot.rotation = q;
	}


	void GetMovingPlatformVelocity (float deltaTime)
	{
		if (groundInfo.GetIsGrounded()) 
		{
			Collider movingPlatformCollider = groundInfo.groundCastInfo.collider;
			if (movingPlatformCollider != null) 
			{
				MovingPlatformController movingPlatform = movingPlatformCollider.gameObject.GetComponentInParent<MovingPlatformController> ();
				if (movingPlatform != null) 
				{
					//standing on a moving platform
					Vector3 platformVelocity = movingPlatform.velocity;
					//Debug.Log (platformVelocity/deltaTime);
					additionalVelocity += platformVelocity;
				}
			}
		}

	}


	void Grounding(bool didCollide, Vector3 previousOrigin, Vector3 origin, LayerMask layerMask, float deltaTime, Vector3 velocity)
	{
		bool hit = false;
		float newRadius = sphereRadius;

		bool _isGrounded = false;
		bool _isPartiallyGrounded = false;
		bool _wasGroundedBefore = groundInfo.wasGroundedBefore;
		//Debug.Log ("in grounding: " + _wasGroundedBefore);
		bool _isOnEdge = false;
		bool _isOnStep = false;
		bool _canJump = false;
		bool _isOnHardEdge = false;
		bool _isGoingTowardsEdge = false;
		bool _previouslyWall = false;
		Vector3 _groundNormal = Vector3.zero;
		Vector3 _groundPoint = Vector3.zero;
		Vector3 _edgeNormal = Vector3.zero;

		bool beginsGrounded = groundInfo.GetIsGrounded();
		groundCheckOffset = minGroundCheckOffset;

		GroundCastInfo hitInfo = new GroundCastInfo ();

		if (didCollide || groundInfo.GetIsGrounded()) 
		{
			hitInfo =  GroundCast(origin, origin, newRadius+0.015f, layerMask, velocity);

			if (hitInfo.hasHit) 
			{
				if (hitInfo.walkable) 
				{
					if (hitInfo.onEdge && hitInfo.edgeInfo.GetOnHardEdge ()) 
					{
						
						if (hitInfo.GetIsSteppable ()) 
						{
							//Debug.Log ("on a valid step!");

							hit = true;
							_isGrounded = true;
							_groundNormal = hitInfo.GetCalculatedGroundNormal ();
							_groundPoint = hitInfo.point;
							_isOnStep = true;
							isJumping = false;

							if (Input.GetKeyDown (KeyCode.O)) 
							{
								DrawGroundDebug (hitInfo.point, hitInfo.GetCalculatedGroundNormal(), 1, Color.cyan, Color.green);
							}

							SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));

						} 
						else 
						{
							//Debug.Log ("NOT on a valid step!");
						}
					} 
					else 
					{
						hit = true;
						_isGrounded = true;
						_groundNormal = hitInfo.GetCalculatedGroundNormal();
						_groundPoint = hitInfo.point;
						_isOnStep = false;
						isJumping = false;

						if (_wasGroundedBefore && Vector3.Angle(hitInfo.GetCalculatedGroundNormal(), groundPivot.up) > SlopeInfo.concaveSlopeLimit)
						{
							_previouslyWall = true;
						}
						


						if (Input.GetKeyDown (KeyCode.O)) 
						{
							DrawGroundDebug (hitInfo.point, hitInfo.GetCalculatedGroundNormal(), 1, Color.cyan, Color.green);
						}

						SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));
					}
				} 
				else if (hitInfo.partiallyWalkable)
                {
					hit = true;
					_isGrounded = true;
					_isPartiallyGrounded = true;
					_groundNormal = hitInfo.GetCalculatedGroundNormal();
					_groundPoint = hitInfo.point;
					_isOnStep = false;
					isJumping = false;
				}
				else
				{
					

					//Debug.Log ("non-walkable!");

					if (hitInfo.onEdge && hitInfo.edgeInfo.GetOnHardEdge()) 
					{
						bool jumpableStep = false;

						if (!groundInfo.GetIsGrounded() && !hitInfo.isValidStep) 
						{
							//do extra checking for when in the air and no ground is found below (implicit by being non-walkable) so that we can jump on these tricky non-ground edges
							float height = ExtVector3.MagnitudeInDirection (hitInfo.point - (hitInfo.detectionOrigin-(hitInfo.sphereRadius*groundPivot.up)), groundPivot.up, false);
							jumpableStep = height <= maxStepHeight;
						}

						_canJump = jumpableStep;
					}
				}
			}
			else 
			{
				//Debug.Log ("hasn't hit anything");
			}
		}
			
		if (hit) 
		{
			SetGrounded (_isGrounded);
			groundInfo.Set(_isPartiallyGrounded, _wasGroundedBefore, hitInfo, _isOnStep, _isGoingTowardsEdge, _previouslyWall);
			groundInfo.canJump = true;


			return;
		}

		SetGrounded (false);
		groundInfo.Set (false, _wasGroundedBefore, hitInfo, false, _isGoingTowardsEdge, false);
		groundInfo.canJump = _canJump;
		SetGroundPivot (Quaternion.FromToRotation (Vector3.up, -gravityDir));
		return;
	}



	Vector3 SearchForLowerConvexGround (Vector3 origin, LayerMask layerMask, float deltaTime)
	{
		Vector3 dirToMoveCheckDown = (groundInfo.groundCastInfo.isValidStep) ? groundInfo.groundCastInfo.edgeInfo.calculatedEdgeNormal : groundPivot.up;

		Vector3 moveVector = Vector3.zero;
		float newRadius = sphereRadius;
		bool beginsGrounded = groundInfo.GetIsGrounded();
		groundCheckOffset = minGroundCheckOffset;
		float numSteps = 10;
		float steppedGroundCheckOffset = (numSteps > 1)? ((maxGroundCheckOffset - minGroundCheckOffset) / (numSteps-1)) : 0;
		//float steppedGroundCheckOffset = (numSteps > 1)? ((maxGroundCheckOffset - minGroundCheckOffset) / (90)) : 0;

		GroundCastInfo hitInfo = new GroundCastInfo ();

		if (beginsGrounded) //of course we only do this if we're already on the ground
		{
			for (int i = 0; i < numSteps; i++) 
			{
				Vector3 newOrigin = origin - (groundPivot.up * groundCheckOffset);
				hitInfo =  GroundCastForLower (newOrigin, origin, newRadius, layerMask, velocity);

				if (hitInfo.hasHit) 
				{
					if (CanWalkOnSlope (hitInfo.GetCalculatedGroundNormal(), groundPivot.up)) 
					{
						//Debug.Log ("hit! i=" + i);

						if (hitInfo.GetIsSteppable() && (CanWalkOnConvexSlope (hitInfo.GetCalculatedGroundNormal (), groundPivot.up) || hitInfo.edgeInfo.GetOnHardEdge ())) 
						{
							moveVector = newOrigin - origin; //vector to move down is equal to the difference between start and end origin (how much we moved down to detect it)
							moveVector = moveVector.normalized * Mathf.Min (moveVector.magnitude, Mathf.Min(maxStepHeight, maxRadiusMove));

							//Debug.Log("found");
						} 
						else 
						{
							moveVector = Vector3.zero;
						}

						//Debug.Log (ExtVector3.PrintFullVector3 (moveVector));
							
						return moveVector;
					} 

				}
					
				groundCheckOffset += steppedGroundCheckOffset*i; //prepare for next iteration
				//groundCheckOffset = Mathf.Clamp (groundCheckOffset * 2, 0.001f, maxGroundCheckOffset/100); //prepare for next iteration
				continue;
			} 
		}
			
		return moveVector;
	}



	List<GroundingSphereCollisionInfo> groundContactsBuffer = new List<GroundingSphereCollisionInfo>();
	GroundCastInfo GroundCast(Vector3 newOrigin, Vector3 originalOrigin, float radius, LayerMask layerMask, Vector3 velocity)
	{
		groundContactsBuffer.Clear ();
		Vector3 transformUp = groundPivot.up;
		GroundCastInfo walkable = new GroundCastInfo();
		GroundCastInfo nonWalkable = new GroundCastInfo();
		Vector3 bottomHeightReference = originalOrigin;
		Vector3 bottomSphereOffset = newOrigin;

		if (currentCollider is SphereCollider) 
		{
			//bottomSphereOffset = origin - (transformUp * groundCheckOffset);
			//bottomHeightReference = origin;
			GroundingSphereCollisionDetect.DetectSphereCollisions(bottomSphereOffset, bottomSphereOffset, radius, layerMask, ignoreColliders, groundContactsBuffer, transform.up, 0, true, true);
		} 
		else if (currentCollider is CapsuleCollider) 
		{
			Vector3 topSphere = GetCapsulePoint(bottomSphereOffset, transformUp);
			Vector3 bottomSphere = GetCapsulePoint(bottomSphereOffset, -transformUp);
			//We use groundCheckOffset as a way to ensure we wont depenetrate ourselves too far off the ground to miss its detection next time.
			//bottomSphereOffset = bottomSphere - (transformUp * groundCheckOffset);

			//When we check to see if the hitpoint is below or above our bottomsphere, we want to take into account where we moved.
			//If we moved upwards, then just use our current bottomSphere, but if we moved downwards, then lets use our previous as the reference.
			//Vector3 previousBottomSphere = GetCapsulePoint(previousOrigin, -transformUp);
			//Vector3 bottomHeightReference = (ExtVector3.IsInDirection(origin - previousOrigin, transformUp)) ? bottomSphere : previousBottomSphere;
			//bottomHeightReference = bottomSphere;

			GroundingSphereCollisionDetect.DetectCapsuleCollisions(topSphere, bottomSphereOffset, bottomSphereOffset, radius, layerMask, ignoreColliders, groundContactsBuffer, transformUp, 0, true, true);
		}

		if (Input.GetKey(KeyCode.K)) {

			DrawContactsDebug (groundContactsBuffer, 2, Color.red, Color.green);
		}
			


		List<GroundCastInfo> walkableGroundPoints = new List<GroundCastInfo> ();
		List<GroundCastInfo> nonWalkableGroundPoints = new List<GroundCastInfo> ();

		//Debug.Log ("buffer: " + groundContactsBuffer.Count);

		//We search for the best ground.
		for (int i = 0; i < groundContactsBuffer.Count; i++) 
		{
			GroundingSphereCollisionInfo collisionPoint = groundContactsBuffer [i];

		
			//We make sure the hit is below our bottomSphere (note: using the original origion allows for perpendicular walls to be included as ground info, which we need for situations where a none-walkable slope forms a v shape with such a wall)
			//if (!ExtVector3.IsInDirection (collisionPoint.closestPointOnSurface - bottomSphereOffset, -transformUp, tinyOffset, false))
			//	continue;

			GroundingEdgeCollisionInfo normalsInfo = new GroundingEdgeCollisionInfo ();
			Vector3 normal = collisionPoint.realNormal;

			if (collisionPoint.isOnEdge) 
			{
				//Debug.Log ("on edge");
				normalsInfo = GroundingSphereCollisionDetect.DetectEdgeCollisions (collisionPoint.collider, collisionPoint.closestPointOnSurface, 0.0001f, collisionPoint.realNormal, collisionPoint.interpolatedNormal, layerMask, ignoreColliders, transformUp, gravityDir, velocity, 0);


				normal = normalsInfo.calculatedGroundNormal;
			}
				

			GroundCastInfo processedGround = new GroundCastInfo(newOrigin, sphereRadius, collisionPoint.closestPointOnSurface, normal, normalsInfo, collisionPoint.collider, collisionPoint.isOnEdge);

			if (processedGround.edgeInfo.GetOnHardEdge ()) 
			{
				processedGround.isValidStep = CheckIfEdgeIsSteppable (processedGround.point, bottomSphereOffset, processedGround.GetCalculatedGroundNormal());

				//Debug.DrawRay (processedGround.point, processedGround.GetCalculatedGroundNormal(), Color.blue);
				//Debug.DrawRay (processedGround.point, processedGround.edgeInfo.calculatedEdgeNormal, Color.cyan);
			}


			if (CanWalkToSlope (processedGround.GetCalculatedGroundNormal (), transformUp, groundInfo.GetIsGrounded(), true) && processedGround.GetIsSteppable()) 
			{
				processedGround.walkable = true;
				walkableGroundPoints.Add (processedGround);
			} 

			else 
			{
				processedGround.walkable = false;
				nonWalkableGroundPoints.Add (processedGround);
			}

		}



		//find the average of walkables (for concave slope handling)
		List<GroundCastInfo> postConcaveProcessingWalkableGroundPoints = new List <GroundCastInfo>();
		List<GroundCastInfo> hitPoints = new List<GroundCastInfo> ();
		Vector3 totalNormals = Vector3.zero;
		float smallestDiff = float.MinValue;
		Vector3 closestPoint = Vector3.zero;
		Vector3 closesPointNormal = Vector3.zero;

		/*for (int i = 0; i < walkableGroundPoints.Count; i++) 
		{
			if (walkableGroundPoints [i].edgeInfo.GetOnHardEdge()) 
			{
				postConcaveProcessingWalkableGroundPoints.Add (walkableGroundPoints [i]);
				continue;
			}
				

			Vector3 currentNormal = walkableGroundPoints [i].GetCalculatedGroundNormal ();

			totalNormals += currentNormal;
			hitPoints.Add (walkableGroundPoints [i]);

			////float distTohit = (newOrigin - walkableGroundPoints[i].point).magnitude;
			//float depenetrationDistance = Geometry.DepenetrateSphereFromPlaneInDirection(bottomSphereOffset, radius, transformUp, walkableGroundPoints[i].point, currentNormal).distance;
			//float angleToGravityDirDiff = Vector3.Angle (currentNormal, -gravityDir);

			//if (depenetrationDistance > smallestDiff) 
			//{
			//	smallestDiff = depenetrationDistance;
			//	closestPoint = walkableGroundPoints[i].point;
			//	closesPointNormal = currentNormal;
			//}
		}*/



		//only create a new one if we actually found non-edge walkables
		/*if (totalNormals != Vector3.zero) 
		{
			//Vector3 newNormal = closesPointNormal;
			Vector3 newNormal = totalNormals.normalized;
			GroundCastInfo newPoint = hitPoints[0];

			smallestDiff = float.MinValue;

			for (int i = 0; i < hitPoints.Count; i++) 
			{
				float depenetrationDistance = Geometry.DepenetrateSphereFromPlaneInDirection(bottomSphereOffset, radius, transformUp, hitPoints[i].point, newNormal).distance;

				if (depenetrationDistance > smallestDiff) 
				{
					smallestDiff = depenetrationDistance;
					newPoint = hitPoints [i];
				}
			}

			GroundCastInfo newWalkableGround = new GroundCastInfo(newOrigin, sphereRadius, newPoint.point, newNormal, newPoint.edgeInfo, newPoint.collider, newPoint.onEdge);
			newWalkableGround.walkable = true;
			postConcaveProcessingWalkableGroundPoints.Add (newWalkableGround);
		}*/

		//Debug.Log ("walkable points:" + )

		bool foundd = false;
		GroundCastInfo newWalkableInfo = new GroundCastInfo();

		for (int i = 0; i < walkableGroundPoints.Count; i++)
		{
			GroundCastInfo firstPoint = walkableGroundPoints[i];


			for (int j = 0; j < walkableGroundPoints.Count; j++)
			{
				GroundCastInfo secondPoint = walkableGroundPoints[j];


				if (secondPoint.GetInterpolatedNormal() == firstPoint.GetInterpolatedNormal())
				{
					continue;
				}

				if ((ExtVector3.Angle(firstPoint.GetInterpolatedNormal(), secondPoint.GetInterpolatedNormal()) < SlopeInfo.concaveSlopeLimit))
                {
					continue;
				}


				Vector3 firstFlattenedNormal = Vector3.ProjectOnPlane(firstPoint.GetInterpolatedNormal(), gravityDir).normalized;
				Vector3 secondFlattenedNormal = Vector3.ProjectOnPlane(secondPoint.GetInterpolatedNormal(), gravityDir).normalized;
				float angleBetween = Vector3.Angle(firstFlattenedNormal, secondFlattenedNormal);

				if (angleBetween >= 135)
				{
					foundd = true;

					newWalkableInfo = secondPoint;
					newWalkableInfo.walkable = true;
					newWalkableInfo.normal = -gravityDir;
					walkableGroundPoints.RemoveAt(i);
					walkableGroundPoints.Remove(secondPoint);
					walkableGroundPoints.Add(newWalkableInfo);

					break;
				}
			}

			if (foundd)
			{
				break;
			}
		}

		if (foundd)
		{
			Debug.Log("found!");
		}


		//List<GroundCastInfo> processedGroundPoints = postConcaveProcessingWalkableGroundPoints;
		List<GroundCastInfo> processedGroundPoints = walkableGroundPoints;
		processedGroundPoints.AddRange (nonWalkableGroundPoints);

		//Debug.Log ("walkable: " + walkableGroundPoints.Count + ", nonwalkable: " + nonWalkableGroundPoints.Count + ", total: " + postConcaveProcessingWalkableGroundPoints.Count);

		Vector3 walkableHighestPoint = float.MinValue * groundPivot.up;
		Vector3 nonWalkableHighestPoint = float.MinValue * groundPivot.up;
		float smallestAngleDiff = float.MaxValue;


		for (int i = 0; i < processedGroundPoints.Count; i++) {

			GroundCastInfo collisionInfo = processedGroundPoints [i];
			Vector3 hitPoint = collisionInfo.point;
			Vector3 normal = collisionInfo.GetCalculatedGroundNormal();



			//float depenetrationDistance = Geometry.DepenetrateSphereFromPlaneInDirection(bottomSphereOffset, radius, transformUp, hitPoint, normal).distance;

			if(collisionInfo.walkable && CanWalkToSlope(normal, transformUp, groundInfo.GetIsGrounded(), true) && collisionInfo.GetIsSteppable())
			{
				/*if(ExtVector3.MagnitudeInDirection ((collisionInfo.point-walkableHighestPoint), groundPivot.up, true) > 0)
				{
					walkableHighestPoint = collisionInfo.point;
					walkable = collisionInfo;

				}*/

				float angleDiff = Vector3.Angle(-gravityDir, normal);

				if (angleDiff < smallestAngleDiff)
                {
					walkableHighestPoint = collisionInfo.point;
					smallestAngleDiff = angleDiff;
					walkable = collisionInfo;
				}
			}
			else
			{ 

				if(ExtVector3.MagnitudeInDirection ((collisionInfo.point-nonWalkableHighestPoint), groundPivot.up, true) > 0)
				{
					nonWalkableHighestPoint = collisionInfo.point;
					nonWalkable = collisionInfo;

					#region Debug
					#if UNITY_EDITOR
					//DrawGroundDebug(nonWalkable.point, nonWalkable.normal, 1, Color.blue, Color.green);
					//Debug.Log ("set nonwalkable");
					#endif
					#endregion
				}
			}
		}


		if(walkable.hasHit)
		{
			return walkable;
		}



		List<GroundCastInfo> candidatePoints = new List<GroundCastInfo>();
		for (int i = 0; i < nonWalkableGroundPoints.Count; i++)
		{
			GroundCastInfo point = nonWalkableGroundPoints[i];

			//here we are flipping it upside down and checking if it is an overhang that the overhang angle isn't too great (will be upside-down-walkable if it is) 
			if (CanWalkOnSlope(point.GetInterpolatedNormal(), gravityDir))
			{
				continue;
			}


			candidatePoints.Add(point);
		}


		///Debug.Log("candidates:" + candidatePoints.Count);

		bool found = false;
		GroundCastInfo newInfo = new GroundCastInfo();

		for (int i = 0; i < candidatePoints.Count; i++)
        {
			GroundCastInfo firstPoint = candidatePoints[i];
			bool firstPointNotFacingUp = (ExtVector3.MagnitudeInDirection(firstPoint.GetInterpolatedNormal(), -gravityDir) <= 0);

			for (int j = 0; j < candidatePoints.Count; j++)
            {
				GroundCastInfo secondPoint = candidatePoints[j];

				if (secondPoint.GetInterpolatedNormal() == firstPoint.GetInterpolatedNormal())
                {
					continue;
                }

				bool secondPointNotFacingUp = (ExtVector3.MagnitudeInDirection(secondPoint.GetInterpolatedNormal(), -gravityDir) <= 0);
				if (firstPointNotFacingUp && secondPointNotFacingUp)
                {
					//both are either walls or ceilings, so we don't want to do it
					continue;
                }

				Vector3 firstFlattenedNormal = Vector3.ProjectOnPlane(firstPoint.GetInterpolatedNormal(), gravityDir).normalized;
				Vector3 secondFlattenedNormal = Vector3.ProjectOnPlane(secondPoint.GetInterpolatedNormal(), gravityDir).normalized;
				float angleBetween = Vector3.Angle(firstFlattenedNormal, secondFlattenedNormal);


				if (angleBetween >= 150)
                {
					if (firstPoint.onEdge || secondPoint.onEdge)
                    {
						//this is not allow it to work if the angle between edges is too wide and we have dropped down too far (max drop based on angle between)

						float maxInterpAngleAllowed = (angleBetween - 90);

						if (!Mathf.Approximately (angleBetween, 180))
                        {
							maxInterpAngleAllowed = Mathf.Min(maxInterpAngleAllowed, 40);
                        }

						if (firstPoint.onEdge && (ExtVector3.MagnitudeInDirection (firstPoint.GetInterpolatedNormal(), -gravityDir) >= 0))
                        {
							float angleToInterp = Vector3.Angle(firstPoint.GetInterpolatedNormal(), -gravityDir);

							if (angleToInterp > maxInterpAngleAllowed)
                            {
								continue;
                            }
                        }
						
						if (secondPoint.onEdge && (ExtVector3.MagnitudeInDirection(secondPoint.GetInterpolatedNormal(), -gravityDir) >= 0))
						{
							float angleToInterp = Vector3.Angle(secondPoint.GetInterpolatedNormal(), -gravityDir);

							if (angleToInterp > maxInterpAngleAllowed)
							{
								continue;
							}
						}

                    }

					found = true;

					newInfo = secondPoint;
					newInfo.partiallyWalkable = true;
					newInfo.normal = -gravityDir;

					break;
                }
			}

			if (found)
            {
				break;
            }
        }

		if (found)
        {
			Debug.Log("found!");
			return newInfo;
        }



		return nonWalkable;
	}


	GroundCastInfo GroundCastForLower (Vector3 newOrigin, Vector3 originalOrigin, float radius, LayerMask layerMask, Vector3 velocity)
	{
		groundContactsBuffer.Clear ();
		Vector3 transformUp = groundPivot.up;
		GroundCastInfo walkable = new GroundCastInfo();
		GroundCastInfo nonWalkable = new GroundCastInfo();
		//Vector3 bottomHeightReference = originalOrigin;
		Vector3 bottomSphereOffset = newOrigin;

		if (currentCollider is SphereCollider) 
		{
			bottomSphereOffset = newOrigin - (transformUp * 0.001f);
			//bottomHeightReference = origin;
			GroundingSphereCollisionDetect.DetectSphereCollisions(bottomSphereOffset, bottomSphereOffset, radius, layerMask, ignoreColliders, groundContactsBuffer, transform.up, 0, true, false);
		} 
		else if (currentCollider is CapsuleCollider) 
		{
			Vector3 topSphere = GetCapsulePoint(bottomSphereOffset, transformUp);
			Vector3 bottomSphere = GetCapsulePoint(bottomSphereOffset, -transformUp);
			//We use groundCheckOffset as a way to ensure we wont depenetrate ourselves too far off the ground to miss its detection next time.
			//bottomSphereOffset = bottomSphere - (transformUp * groundCheckOffset);

			//When we check to see if the hitpoint is below or above our bottomsphere, we want to take into account where we moved.
			//If we moved upwards, then just use our current bottomSphere, but if we moved downwards, then lets use our previous as the reference.
			//Vector3 previousBottomSphere = GetCapsulePoint(previousOrigin, -transformUp);
			//Vector3 bottomHeightReference = (ExtVector3.IsInDirection(origin - previousOrigin, transformUp)) ? bottomSphere : previousBottomSphere;
			//bottomHeightReference = bottomSphere;

			//SphereCollisionDetect.DetectCapsuleCollisions(topSphere, bottomSphereOffset, radius, layerMask, ignoreColliders, groundContactsBuffer, 0);
			GroundingSphereCollisionDetect.DetectCapsuleCollisions(topSphere, bottomSphereOffset, bottomSphereOffset, radius, layerMask, ignoreColliders, groundContactsBuffer, transformUp, 0, true, false);
		}

		if (Input.GetKey(KeyCode.K)) {

			DrawContactsDebug (groundContactsBuffer, 2, Color.red, Color.green);
		}



		List<GroundCastInfo> walkableGroundPoints = new List<GroundCastInfo> ();
		List<GroundCastInfo> nonWalkableGroundPoints = new List<GroundCastInfo> ();

		//Debug.Log ("buffer: " + groundContactsBuffer.Count);

		//We search for the best ground.
		for (int i = 0; i < groundContactsBuffer.Count; i++) 
		{
			GroundingSphereCollisionInfo collisionPoint = groundContactsBuffer [i];


			//We make sure the hit is below our bottomSphere (note: using the original origion allows for perpendicular walls to be included as ground info, which we need for situations where a none-walkable slope forms a v shape with such a wall)
			if (!ExtVector3.IsInDirection (collisionPoint.closestPointOnSurface - bottomSphereOffset, -transformUp, tinyOffset, false))
				continue;

			GroundingEdgeCollisionInfo normalsInfo = new GroundingEdgeCollisionInfo ();
			Vector3 normal = collisionPoint.realNormal;

			if (collisionPoint.isOnEdge) 
			{
				//Debug.Log ("on edge");
				normalsInfo = GroundingSphereCollisionDetect.DetectEdgeCollisions (collisionPoint.collider, collisionPoint.closestPointOnSurface, 0.0001f, collisionPoint.realNormal, collisionPoint.interpolatedNormal, layerMask, ignoreColliders, transformUp, gravityDir, velocity, 0);
				normal = normalsInfo.calculatedGroundNormal;
			}


			GroundCastInfo processedGround = new GroundCastInfo(newOrigin, sphereRadius, collisionPoint.closestPointOnSurface, normal, normalsInfo, collisionPoint.collider, collisionPoint.isOnEdge);

			if (processedGround.edgeInfo.GetOnHardEdge ()) 
			{
				//Debug.Log ("hard edge!");
				processedGround.isValidStep = CheckIfEdgeIsSteppable (processedGround.point, bottomSphereOffset, processedGround.GetCalculatedGroundNormal());

				//Debug.DrawRay (processedGround.point, processedGround.GetCalculatedGroundNormal(), Color.blue);
				//Debug.DrawRay (processedGround.point, processedGround.edgeInfo.calculatedEdgeNormal, Color.cyan);
			}


			if (CanWalkToSlope (processedGround.GetCalculatedGroundNormal (), transformUp, groundInfo.GetIsGrounded()) && processedGround.GetIsSteppable()) 
			{
				walkableGroundPoints.Add (processedGround);
			} 

			else 
			{
				nonWalkableGroundPoints.Add (processedGround);
			}

		}

		List<GroundCastInfo> processedGroundPoints = walkableGroundPoints;
		processedGroundPoints.AddRange (nonWalkableGroundPoints);

		//Debug.Log ("walkable: " + walkableGroundPoints.Count + ", nonwalkable: " + nonWalkableGroundPoints.Count + ", total: " + postConcaveProcessingWalkableGroundPoints.Count);

		Vector3 walkableHighestPoint = float.MinValue * groundPivot.up;
		Vector3 nonWalkableHighestPoint = float.MinValue * groundPivot.up;

		for (int i = 0; i < processedGroundPoints.Count; i++) {

			GroundCastInfo collisionInfo = processedGroundPoints [i];
			Vector3 hitPoint = collisionInfo.point;
			Vector3 normal = collisionInfo.GetCalculatedGroundNormal();



			float depenetrationDistance = Geometry.DepenetrateSphereFromPlaneInDirection(bottomSphereOffset, radius, transformUp, hitPoint, normal).distance;

			if(CanWalkToSlope(normal, transformUp, groundInfo.GetIsGrounded()) && collisionInfo.GetIsSteppable())
			{
				if(ExtVector3.MagnitudeInDirection ((collisionInfo.point-walkableHighestPoint), groundPivot.up, true) > 0)
				{
					walkableHighestPoint = collisionInfo.point;
					walkable = collisionInfo;


					#region Debug
					#if UNITY_EDITOR
					//DrawGroundDebug(walkable.point, walkable.normal, 1, Color.cyan, Color.green);
					//Debug.Log ("set walkable");
					#endif
					#endregion
				}
			}
			else 
			{

				if(ExtVector3.MagnitudeInDirection ((collisionInfo.point-nonWalkableHighestPoint), groundPivot.up, true) > 0)
				{
					nonWalkableHighestPoint = collisionInfo.point;
					nonWalkable = collisionInfo;

					#region Debug
					#if UNITY_EDITOR
					//DrawGroundDebug(nonWalkable.point, nonWalkable.normal, 1, Color.blue, Color.green);
					//Debug.Log ("set nonwalkable");
					#endif
					#endregion
				}
			}
		}

		if(walkable.hasHit)
		{
			return walkable;
		}
		return nonWalkable;
	}


	public bool SetWallsInfo (Vector3 v, bool useNonDepenetratedNormals)
	{
		//v is local

		Vector3 lateralVelocityDir = new Vector3 (v.x, 0, v.z).normalized;

		if (lateralVelocityDir == Vector3.zero) 
		{
			collisionInfo.minWallAngle = 1;
			collisionInfo.minWallNormal = Vector3.zero;
			collisionInfo.maxWallAngle = -1;
			collisionInfo.maxWallNormal = Vector3.zero;
			collisionInfo.averageWallNormal = Vector3.zero;

			return false;
		}

		float biggestPosAngle = -1;
		Vector3 normalOfBiggestPosAngle = Vector3.zero;
		float smallestNegAngle = 1;
		Vector3 normalOfSmallestNegAngle = Vector3.zero;


		for (int i = 0; i < collisionInfo.pointsInfo.Count; i++) 
		{
			//Debug.Log(collisionInfo.pointsInfo[i].wasDetected);

			//if (!useNonDepenetratedNormals && !collisionInfo.pointsInfo[i].wasDepenetrated)
			if (!collisionInfo.pointsInfo[i].wasDetected)
			{
				continue;
            }

			Vector3 normal = GetNormalToLimitSpeedBy (collisionInfo.pointsInfo [i]);

			if (CanWalkOnSlope(normal, groundPivot.up))
			{
				continue;
			}
			
			
			normal = groundPivot.InverseTransformDirection (normal);

			Vector3 flattenedNormal = new Vector3 (normal.x, 0, normal.z);

			if (flattenedNormal == Vector3.zero) {
				continue;
			}

			if (!ExtVector3.IsInDirection (lateralVelocityDir, -flattenedNormal)) {
				continue;
			}




			if (!ExtVector3.IsInDirection (v, -flattenedNormal)) {
				continue;
			}

			float angleToVelocity = Vector3.SignedAngle (lateralVelocityDir, -flattenedNormal, groundPivot.up);

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

		collisionInfo.minWallAngle = smallestNegAngle;
		collisionInfo.minWallNormal = normalOfSmallestNegAngle;
		collisionInfo.maxWallAngle = biggestPosAngle;
		collisionInfo.maxWallNormal = normalOfBiggestPosAngle;
		collisionInfo.averageWallNormal = averageNormal;

		return averageNormal != Vector3.zero; //return hit any wall
	}
		

	public Vector3 GetWallNormalInDir (Vector3 d)
	{
		//d is local

		Vector3 w = Vector3.zero;

		if (collisionInfo.minWallAngle != 1 && collisionInfo.maxWallAngle != -1) 
		{
			return w;
		} 
		else 
		{
			if (collisionInfo.minWallAngle != 1) 
			{
				w = collisionInfo.minWallNormal;
			} 
			else if (collisionInfo.maxWallAngle != -1) 
			{
				w = collisionInfo.maxWallNormal;
			}
		}

		return w;
	}


	public float GetAngleBetweenDirAndWall (Vector3 v)
	{
		//v is local

		float angle = 0;
		
		if (collisionInfo.minWallAngle != 1 && collisionInfo.maxWallAngle != -1) 
		{
			angle = 0;
		} 
		else 
		{
			if (collisionInfo.minWallAngle != 1) 
			{
				angle = Vector3.SignedAngle (-collisionInfo.minWallNormal, v.normalized, groundPivot.up);
			} 
			else if (collisionInfo.maxWallAngle != -1) 
			{
				angle = Vector3.SignedAngle (-collisionInfo.maxWallNormal, v.normalized, groundPivot.up);
			}
		}

		return angle;
	}

	public Vector3 LimitVelocityOnWalls (Vector3 v, bool zeroOnSmallAngles = true)
	{
		//v is local

		/*if (ExtVector3.IsInDirection (lateralVelocityDir, -averageNormal))
		{
			v = Vector3.ProjectOnPlane (v, averageNormal);
		}

		if (ExtVector3.IsInDirection (lateralVelocityDir, -normalOfBiggestPosAngle))
		{
			v = Vector3.ProjectOnPlane (v, normalOfBiggestPosAngle);
		}

		if (ExtVector3.IsInDirection (lateralVelocityDir, -normalOfSmallestNegAngle))
		{
			v = Vector3.ProjectOnPlane (v, normalOfSmallestNegAngle);
		}

		if (!ExtVector3.IsInDirection (lateralVelocityDir, v)) 
		{
			//set lateral to 0 if its going to go back on the original direction
			Vector3 vertical = v - Vector3.ProjectOnPlane (v, groundPivot.up);
			v = vertical;
		}*/


		float maxAngleStopTolerance = wallAngleZeroAngle;
		if (collisionInfo.minWallAngle != 1 && collisionInfo.maxWallAngle != -1) 
		{
			//if there is a wall on either side then we know we are entering a v shape, so just kill all lateral velocity
			Vector3 vertical = new Vector3 (0, v.y, 0);
			v = vertical;
		} 
		else 
		{
			if (collisionInfo.minWallAngle != 1) 
			{
				if (collisionInfo.minWallAngle >= -maxAngleStopTolerance) 
				{
					if (zeroOnSmallAngles) 
					{
						Vector3 vertical = new Vector3 (0, v.y, 0);
						v = vertical;
					} 
					else 
					{
						v = Vector3.ProjectOnPlane (v, collisionInfo.minWallNormal);
					}

				} 
				else 
				{
					v = Vector3.ProjectOnPlane (v, collisionInfo.minWallNormal);
				}
			} 
			else if (collisionInfo.maxWallAngle != -1) 
			{
				if (collisionInfo.maxWallAngle <= maxAngleStopTolerance) 
				{
					if (zeroOnSmallAngles) 
					{
						Vector3 vertical = new Vector3 (0, v.y, 0);
						v = vertical;
					} 
					else 
					{
						v = Vector3.ProjectOnPlane (v, collisionInfo.maxWallNormal);
					}

				} 
				else 
				{
					v = Vector3.ProjectOnPlane (v, collisionInfo.maxWallNormal);
				}
			}
		}

		//Debug.Log (averageNormal);

		return v;
	}


	public bool CheckIfEdgeIsSteppable (Vector3 edgePoint, Vector3 currentCentre, Vector3 upDir)
	{
		Vector3 lateralEdgeToCentreDir = Vector3.ProjectOnPlane ((currentCentre - edgePoint), upDir).normalized;
		float lateralDistance = GetStepCheckLateralDist (maxStepHeight);
		Vector3 castPoint = edgePoint + lateralEdgeToCentreDir * lateralDistance;

		RaycastHit hitInfo = new RaycastHit ();
		Debug.DrawRay (castPoint, -upDir * maxStepHeight, Color.green, Mathf.Infinity);
		if (Physics.Raycast (castPoint, -upDir, out hitInfo, maxStepHeight + 0.001f, collisionLayers)) 
		{
			if (CanWalkToSlope (hitInfo.normal, upDir, groundInfo.GetIsGrounded()))
			{
				//Debug.Log ("ground found below, this is a valid step!");
				return true;
			}
		}
		else
        {
			//do a second check which halves the max step height but brings the raycast half the distance closer

			float newMaxStepHeight = maxStepHeight * 0.5f;
			lateralDistance = GetStepCheckLateralDist(newMaxStepHeight);
			castPoint = edgePoint + lateralEdgeToCentreDir * lateralDistance;

			Debug.DrawRay(castPoint, -upDir * newMaxStepHeight, Color.green, Mathf.Infinity);
			if (Physics.Raycast(castPoint, -upDir, out hitInfo, newMaxStepHeight + 0.001f, collisionLayers))
			{
				if (CanWalkToSlope(hitInfo.normal, upDir, groundInfo.GetIsGrounded()))
				{
					//Debug.Log ("ground found below, this is a valid step!");
					return true;
				}
			}
		}

		//Debug.Log ("no ground found below within specified height, this is not a valid step!");
		return false;
	}

	float GetStepCheckLateralDist (float maxStepHeightCheck)
	{
		return (float) Math.Sqrt (sphereRadius*sphereRadius-(sphereRadius-maxStepHeightCheck)*(sphereRadius-maxStepHeightCheck));
	}



	protected Vector3 ApplyFriction(Vector3 velocity, float deltaTime)
	{
		//If our velocity is going upwards, such as a jump, we dont want to apply friction.
		//if(!ExtVector3.IsInDirection(velocity, transform.up, tinyOffset, false))
		//{
		float keepY = velocity.y;
		velocity.y = 0;
		velocity = velocity * (1f / (1f + (friction * deltaTime)));
		velocity.y = keepY;
		return velocity;
		//}

		//return velocity;
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
		return GroundingSphereCollisionDetect.DetectEdgeCollisions (col, point, 0.0001f, realNormal, interpolatedNormal, collisionLayers, ignoreColliders, groundPivot.up, gravityDir, velocity, 0);

	}
		


	//This tries to stops us if we are grounded and trying to walk up a slope thats angle is higher than our slope limit. This prevents jitter due to constant isGrounded to isNotGrounded when trying to walk up non walkable slopes.
	//We basically just create a wall stopping us from going up the slope.
	//Problem - This isnt perfect, for example it has some jitter issues and this methods success depends on how much we are moving (less = better).
	//This doesnt work well on low angled slopes. It kinda does if you check if you were previously grounded and use that normal, but it was very jittery. Since slope limits are usualy not so low it shouldnt be an issue.
	//Also, if we did use the previous groundNormal, we might have an issue if we were to jump and hit the non walkable slope and wanted to slide up, but the previous groundNormal might prevent that.
	//I have also seen a issue where if we are between 2 high angled slopes that we cant walk on, this will create a wall on both sides as it should and stop us, but our depenetration method wont know what to do and fail.
	/*void TryBlockAtSlopeLimit(List<SphereCollisionInfo> collisionPoints)
	{
		if(groundInfo.isGrounded && !isOnEdge)
		{						
			for(int j = 0; j < collisionPoints.Count; j++)
			{
				SphereCollisionInfo collisionPoint = collisionPoints[j];
				if(ExtVector3.IsInDirection(collisionPoint.normal, transform.up, tinyOffset, false) && !CanWalkOnSlope(collisionPoint.normal) && !collisionPoint.isOnEdge)
				{
					SweepInfo sweep = Geometry.SpherePositionBetween2Planes(collisionPoint.sphereRadius, collisionPoint.closestPointOnSurface, collisionPoint.normal, groundPoint, groundNormal, false);
					if(sweep.hasHit)
					{
						Vector3 depenetrateDirection = Vector3.ProjectOnPlane(collisionPoint.normal, groundNormal).normalized;

						//First we allign the intersectCenter with the detectionOrigin so that our depenetration method can use it properly.
						sweep.intersectCenter = sweep.intersectCenter + Vector3.Project(collisionPoint.detectionOrigin - sweep.intersectCenter, groundNormal);
						collisionPoints[j] = new SphereCollisionInfo(true, collisionPoint.collider, collisionPoint.detectionOrigin, collisionPoint.sphereRadius, sweep.intersectCenter - (depenetrateDirection * (collisionPoint.sphereRadius - smallOffset)), depenetrateDirection);
					}
				}
			}
		}
	}*/



	Vector3 ClampVelocity(Vector3 velocity)
	{
		/*if (Mathf.Abs (velocity.x) < 0.001f)
			velocity.x = 0;
		if (Mathf.Abs (velocity.y) < 0.001f)
			velocity.y = 0;
		if (Mathf.Abs (velocity.z) < 0.001f)
			velocity.z = 0;*/

		velocity = ClampVerticalVelocity(velocity, groundPivot.up, maxVerticalVelocity);
		velocity = ClampHorizontalVelocity(velocity, groundPivot.up, maxSpeed);
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


	public static bool CanWalkOnSlope(Vector3 normal, Vector3 comparedNormal)
	{
		if(normal == Vector3.zero) return false;
		return ExtVector3.Angle(normal, comparedNormal) < SlopeInfo.walkableSlopeLimit;
		//return true;
	}

	public bool CanWalkToSlope(Vector3 normal, Vector3 comparedNormal, bool isGrounded, bool forSpecial = false)
	{
		if(normal == Vector3.zero) return false;

		//this is for walkable slopes in a V shape where we should'nt think one of them is an unwalkable wall
		if ((ExtVector3.Angle(normal, -gravityDir) < SlopeInfo.concaveSlopeLimit) && CanWalkOnSlope (groundPivot.up, -gravityDir))
        {
			return true;
        }

		if (isGrounded) 
		{
			if (ExtVector3.Angle (normal, -gravityDir) < ExtVector3.Angle (comparedNormal, -gravityDir)) //going toward gravityUp?
			{
				//forSpecial is for when we are on a non-gravity walkable slope and going down to find a normally-gravity walkable slope, so that we can go back to normal ground
				//it is passed true from normal groundCast but nowhere else

				if (forSpecial)
                {
					if (CanWalkOnSlope(normal, comparedNormal))
					{
						return true;
					}
					else
					{
						return CanWalkOnSlope(normal, -gravityDir);
					}
				}
				else
                {
					return CanWalkOnSlope(normal, comparedNormal);
				}

			}

			return ExtVector3.Angle(normal, comparedNormal) < SlopeInfo.concaveSlopeLimit;
		} 
		else 
		{
			return CanWalkOnSlope (normal, comparedNormal);
		}
	}


	bool CanWalkOnConvexSlope(Vector3 normal, Vector3 comparedNormal)
	{
		if(normal == Vector3.zero) return false;
		return ExtVector3.Angle(normal, comparedNormal) < SlopeInfo.convexSlopeLimit;
		//return true;
	}


	Vector3 GetNormalToLimitSpeedBy (SphereCollisionDetect.CollisionPointInfo cpi)
	{
		Vector3 normal = cpi.depenetrationNormal;


		//if the normal is a sloped ceiling then treat it as a sideways wall when on the ground
		if (groundInfo.GetIsGrounded() && Vector3.Angle (normal, -groundPivot.up) < 90) 
		{
			normal = Vector3.ProjectOnPlane (normal, groundPivot.up); 
		}


		if (cpi.slopeTooSteep && !groundInfo.GetIsGrounded()) 
		{
			//normal = cpi.cp.interpolatedNormal; //the problem with this is it causes you to move up and therefore gain extra height on your jumps
		}

		return normal;
	}

	public bool CheckForCeiling (Vector3 velocity = new Vector3())
	{
		for (int i = 0; i < collisionInfo.pointsInfo.Count; i++) 
		{
			SphereCollisionDetect.CollisionPointInfo pointInfo = collisionInfo.pointsInfo [i];
			Vector3 normalToUse = GetNormalToLimitSpeedBy (pointInfo);

			if (velocity == Vector3.zero || ExtVector3.IsInDirection(velocity, -normalToUse, tinyOffset, true))
			{
				if (ExtVector3.IsInDirection (normalToUse, gravityDir)) 
				{
					//ceiling normal
					if (Vector3.Angle (normalToUse, gravityDir) < 45) 
					{
						return true;
					}

					//else do nothing
				}
			}
		}

		return false;
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
		public bool canJump;
		public bool isGoingTowardsEdge;
		public bool previouslyWall;
		public GroundCastInfo groundCastInfo;
		public GroundType groundType;

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



	}


	public void SetGrounded(bool newGrounded, bool partiallyGrounded = false) 
	{
		if (!newGrounded) 
		{
			if (groundInfo.GetIsGrounded()) 
			{
				//leaving ground
				groundInfo.canJump = false;

				if (cLeftGroundTimer != null)
					StopCoroutine (cLeftGroundTimer);
				cLeftGroundTimer = LeftGroundTimer ();
				StartCoroutine (cLeftGroundTimer);
			}

			groundInfo.isOnStep = false;
		}

		groundInfo.SetGrounded(newGrounded);
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
