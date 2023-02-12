using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActionsController : PlayerPhysicsController {

	//coroutines
	[HideInInspector]
	public IEnumerator cIncreaseJumpHeight;
	public IEnumerator cUseGroundBooster;
	public IEnumerator cUseAirBooster;
	public IEnumerator cUseSpring;


	GeneralInput input;
	PlayerGraphicsRotations pgr;
	PlayerGraphicsController pgc;

	public PlayerInput playerInput;
	public LayerMask swingPointsMask;
	public Transform tailEnd;
	LineRenderer lineRenderer;

	float normalAcceleration = 12;
	float normalDeceleration = 7;
	float iceAcceleration = 8;
	float iceDeceleration = 3;
	float airDeceleration = 10;
	float normalSkidDeceleration = 14;
	float iceSkidDeceleration = 8;
	float airBackwardsDeceleration = 16;
	float slopeAdditionalDeceleration = 12;
	float slopeAdditionalAcceleration = 20;
	float walkSpeed = 2;
	float walkInputTolerance = 0.8f;
	float topSpeed = 12;
	float jumpPower = 12;
	float gravityStrength = 36;
	float maxGravitySpeed = 40;
	float groundTurnSpeed = 12;
	float airTurnSpeed = 14;
	float iceTurnSpeed = 5;
	float aligningSpeed = 6;
	float skidAngle = 130;

	public bool isSkidding;

	public CurrentSwingPointInfo swingPointInfo = new CurrentSwingPointInfo ();

	public bool isHooked = false;
	public bool isHooking = false;
	bool isUnhooking = false;
	int swingingFacingDir; //1 if facing swing point forward, -1 if not
	float swingPointCheckDist = 8;
	float swingPointMaxFindDist = 10;
	float swingPointMaxFindAngle = 85;
	float hookingSpeed = 30;
	float currentTailLength;
	float minTailLength = 1.2f;
	float maxTailLength = 3.1f;
	float maxSwingSpeed = 30;

	public bool dying;
	public bool currentlyControllable;

	public bool currentlyUsingBooster;
	public bool currentlyUsingAirBooster;
	public bool currentlyUsingSpring;

	void Awake () 
	{
		base.DoAwake ();
		input = sc.GetComponent<GeneralInput> ();
		pgr = GetComponent <PlayerGraphicsRotations> ();
		pgc = GetComponent<PlayerGraphicsController>();
		playerInput = GetComponent <PlayerInput> ();

	}

	void Start () 
	{
		base.DoStart ();
		sc.AddPlayerToList (gameObject);

		pgr.DoStart();
		pgc.DoStart();

		//find out where the players spawn position should be (eg checkpoint?)
		Vector3 receivedStartPos = sc.GetPlayerSpawnPos ();
		if (receivedStartPos != Vector3.zero) 
		{
			transform.position = receivedStartPos;
		}


		currentlyControllable = true;
		lineRenderer = GetComponent<LineRenderer> ();

	}
		


	public override void DoUpdate () 
	{
		base.DoUpdate ();
		pic.DoUpdate ();


		playerInput.glideAxis = false;

		if (sc.gameController.debugMode)
        {
			if (input.GetButtonDown(GeneralInput.AxesNames.DebugPlace))
			{
				sc.debugInfoManager.CreateManualDebugWithCollision(sc.gameController.debugMode, transform.position, groundInfo.up, groundPivot.rotation, groundInfo.GetIsGrounded());
			}
		}

		if (sc.debugModeType == ActionSceneController.DebugModeType.Player) 
		{
			DebugMove (playerInput.GetRawInput().normalized);
		}

	}

	public override void DoFinalUpdate ()
	{
		UpdateGraphicsObjects (Time.deltaTime);

		// This has to be reset here rather than in the next jump method check, as that can run multiple times per frame whereas the graphics only run once
		justStartedJumping = false;

	}

	protected override void DoPlayerInputUpdate(float deltaTime)
    {
		playerInput.DoUpdate();
	}

	protected override void PreCollisionControl (float deltaTime)
	{
		base.PostCollisionControl(deltaTime);

		if (Input.GetAxis ("Grab") > 0) 
		{
			//get key down
			if (!playerInput.grabAxis) 
			{
				if (!isUnhooking)
				{
					Transform found = FindClosestSwingPoint ();

					if (found != null) 
					{
						HookSwingPoint (found);
					} 
				} 

				playerInput.grabAxis = true;
			}

			else 
			{
				if (isHooked) 
				{
					//update swing point position
					tailEnd.position = swingPointInfo.transform.position;
					//do swing physics
					//SwingPhysics (deltaTime);
				} 
			}


		} 
		else 
		{
			//get key up
			if (playerInput.grabAxis) 
			{
				//if (isHooked) 
				//{
					UnhookSwingPoint ();
				//} 

				playerInput.grabAxis = false;
			} 

		}




		/*if ((Input.GetKeyDown (KeyCode.LeftShift) && !playerInput.glideAxis) || (Input.GetAxis ("Jump") > 0 && !playerInput.jumpAxis)) 
		{
			playerInput.glideAxis = true;

			if (moveMode == MoveMode.Default) 
			{
				moveMode = MoveMode.Gliding;
				velocity.y = 0;
			} 
			else if (moveMode == MoveMode.Gliding) 
			{
				moveMode = MoveMode.Default;
			}
		}*/ 

		if (groundInfo.GetIsGrounded())
			moveMode = MoveMode.Default;


		if (moveMode == MoveMode.Default) 
		{
			if (isHooked) 
			{
				
				//Walk (deltaTime);
				//Gravity (deltaTime);
				SwingPhysics (deltaTime);
			} 
			else 
			{
				if (currentlyUsingBooster || currentlyUsingAirBooster || currentlyUsingSpring) 
				{
					
				}
				else 
				{
					Walk (deltaTime);
				}


				if (currentlyUsingSpring || currentlyUsingAirBooster) 
				{
					
				} 
				else 
				{
					Gravity (deltaTime);
				}

				Jump (deltaTime);

			}

		} 

			

	}


	protected override void PostCollisionControl (float deltaTime) 
	{
		UpdateTailHooking (deltaTime);

		if (!dying) 
		{
			pic.DealWithInteractables (deltaTime);
		}

		//Debug.Log (ExtVector3.PrintFullVector3 (new Vector3 (lateralVelocity.x, 0, lateralVelocity.z).normalized));
		//Debug.Log (ExtVector3.PrintFullVector3 (new Vector3 (facingDir.x, 0, facingDir.z).normalized));
	}

	protected override void SetGroundPivot (Quaternion q)
	{
		if (!isHooked) 
		{
			groundPivot.rotation = q;
		}
	}


	void UpdateGraphicsObjects (float deltaTime) 
	{
		pgr.DoUpdate (deltaTime, groundInfo.up, groundInfo.GetIsGrounded(), isHooked, facingDir);
		pgc.DoUpdate (deltaTime, sc.gamePaused, velocity, gravityDir, groundInfo.up, groundInfo.GetIsGrounded(), playerInput.GetRawInput(), isHooked, isJumping, justStartedJumping);

		if (tailEnd.position == transform.position) 
		{
			lineRenderer.positionCount = 0;
		} 
		else 
		{
			lineRenderer.positionCount = 2;
			lineRenderer.SetPosition (0, transform.position);
			lineRenderer.SetPosition (1, tailEnd.position);
		}

	}


	Transform FindClosestSwingPoint () 
	{
		Collider[] foundColliders = Physics.OverlapSphere (transform.position, swingPointCheckDist, swingPointsMask);
		List<Transform> validSwingPoints = new List<Transform> ();

		if (foundColliders.Length > 0) 
		{
			float closestAngle = 100000;
			Transform closestSwingPoint = null;

			for (int i = 0; i < foundColliders.Length; i++) 
			{
				Vector3 hitPos = foundColliders [i].transform.position;
				Vector3 originToHit = hitPos - transform.position;

				if (originToHit.magnitude > swingPointMaxFindDist) 
				{
					continue;
				}

				float angleBetween = Vector3.Angle (facingDir, originToHit);
				if (angleBetween > swingPointMaxFindAngle) 
				{
					continue;
				}

				validSwingPoints.Add (foundColliders [i].transform);

				if (angleBetween < closestAngle) 
				{
					closestAngle = angleBetween;
					closestSwingPoint = foundColliders [i].transform;
				}
			}



			//Debug.Log ("found " + validSwingPoints.Count);
			return closestSwingPoint;
		}

		return null;
	}

	void HookSwingPoint (Transform found) 
	{
		isHooking = true;
		swingPointInfo.transform = found;
		swingPointInfo.directionType = found.GetComponent<SwingPointInfo> ().directionType;
	}

	void UnhookSwingPoint ()
	{
		isUnhooking = true;
		isHooked = false;
		swingPointInfo.transform = null;

		//Debug.Log (velocity);
	}


	void SwingPhysics (float deltaTime) 
	{
		Vector3 swingAxis = Vector3.zero;
		Vector3 moveInp = playerInput.GetRawInput();

		bool tailLengthened = ChangeTailLength (deltaTime);

		if (swingPointInfo.directionType == SwingPointInfo.DirectionType.FixedDirection)
		{
			swingAxis = swingPointInfo.transform.right;
			//moveInp = (groundPivot.InverseTransformDirection (moveInp)).normalized;
			moveInp = (Vector3.ProjectOnPlane(moveInp, swingAxis)).normalized;
			//moveInp = (Vector3.ProjectOnPlane (moveInp, groundPivot.up)).normalized;
		}


		Vector3 playerToSwingPoint = swingPointInfo.transform.position - transform.position;

		if (swingAxis != Vector3.zero)
        {
			Vector3 direction = Vector3.ProjectOnPlane(playerToSwingPoint.normalized, swingAxis).normalized;
			
			groundPivot.rotation = Quaternion.FromToRotation(Vector3.up, direction);
		}
		else
        {
			groundPivot.rotation = Quaternion.FromToRotation(Vector3.up, (playerToSwingPoint.normalized));
		}
		

		

		Vector3 lateralVelocity = groundPivot.InverseTransformDirection (velocity);

		if (moveInp != Vector3.zero) 
		{
			lateralVelocity += moveInp * normalAcceleration*0.7f * deltaTime;
		} 
		else 
		{
			//velocity = Vector3.Lerp (velocity, Vector3.zero, deceleration * deltaTime);
			//if (velocity.magnitude < 0.1f)
			//velocity = Vector3.zero;

			float deceleration = 12;

			if (ExtVector3.MagnitudeInDirection (velocity, gravityDir) <= 0)
            {
				lateralVelocity = (lateralVelocity.magnitude - deceleration * deltaTime) * lateralVelocity.normalized;
			}

			
		}

		lateralVelocity = Mathf.Min (maxSwingSpeed, lateralVelocity.magnitude) * lateralVelocity.normalized;
		velocity = groundPivot.TransformDirection (lateralVelocity);

		velocity = Vector3.ProjectOnPlane (velocity, swingAxis);
		
		Gravity (deltaTime);

		Vector3 newPos = transform.position;
		Vector3 oldPos = newPos;

		//constrain to axis

		if (swingAxis != Vector3.zero) 
		{
			float axisDisplacment = ExtVector3.MagnitudeInDirection (newPos - swingPointInfo.transform.position, swingAxis, false);

			oldPos = newPos;
			//newPos = Vector3.Lerp (newPos, newPos + axisDisplacment * -swingAxis, 10 * deltaTime);
			newPos = ExtVector3.CustomControlledLerpPosition(newPos, newPos + axisDisplacment * -swingAxis, 10, 0, 11, deltaTime);
			//newPos = Vector3.MoveTowards (newPos, newPos + axisDisplacment * -swingAxis, 5 * deltaTime);
			//transform.position = newPos;
			additionalVelocity += newPos - oldPos;
			
		}


		//constrain to tail length

		playerToSwingPoint = swingPointInfo.transform.position - newPos;

		if (playerToSwingPoint.magnitude > maxTailLength && !(playerToSwingPoint.magnitude - currentTailLength < 0.01f)) 
		{
			/*if (Mathf.Abs (playerToSwingPoint.magnitude - maxTailLength) < 0.2f) 
			{
				oldPos = newPos;
				newPos = swingPointInfo.transform.position + currentTailLength * -playerToSwingPoint.normalized;
				additionalVelocity += newPos - oldPos;
			} 
			else 
			{*/
			oldPos = newPos;
			//newPos = Vector3.Lerp (newPos, newPos + (playerToSwingPoint - currentTailLength * playerToSwingPoint.normalized), 15 * deltaTime);
			newPos = ExtVector3.CustomControlledLerpPosition(newPos, newPos + (playerToSwingPoint - currentTailLength * playerToSwingPoint.normalized), 15, 0, 15, deltaTime);
			additionalVelocity += newPos - oldPos;
			//}

		} 
		//else if (playerToSwingPoint.magnitude > currentTailLength || tailLengthened)
		else if (playerToSwingPoint.magnitude != currentTailLength)
		{
			playerToSwingPoint = swingPointInfo.transform.position - newPos;
			oldPos = newPos;
			newPos = swingPointInfo.transform.position + currentTailLength * -playerToSwingPoint.normalized;

			//transform.position = newPos;
			additionalVelocity += newPos - oldPos;
		}




		playerToSwingPoint = swingPointInfo.transform.position - newPos;
		//float axisDisplacement = ExtVector3.MagnitudeInDirection (playerToSwingPoint, swingAxis, false);

		//if (Mathf.Abs (axisDisplacement) > 0) 
		//{
			//Vector3 newPos = transform.position + swingAxis * 12 * Mathf.Sign (axisDisplacement) * deltaTime;
			//Vector3 newPos = transform.position + swingAxis * 12 * deltaTime;
			//newPos = ((transform.position - newPos).magnitude > Mathf.Abs (axisDisplacement)) ? transform.position + axisDisplacement * swingAxis : newPos;
			//transform.position = newPos;
		//}

		playerToSwingPoint = swingPointInfo.transform.position - newPos;

		if (swingAxis != Vector3.zero)
		{
			Vector3 direction = Vector3.ProjectOnPlane(playerToSwingPoint.normalized, swingAxis).normalized;
			
			groundPivot.rotation = Quaternion.FromToRotation(Vector3.up, direction);
		}
		else
		{
			groundPivot.rotation = Quaternion.FromToRotation(Vector3.up, (playerToSwingPoint.normalized));
		}


		float magInDir = ExtVector3.MagnitudeInDirection(velocity, -playerToSwingPoint.normalized);
		if (magInDir > 0 && playerToSwingPoint.magnitude >= currentTailLength)
        {
			velocity = Vector3.ProjectOnPlane(velocity, groundInfo.up);
		}
		velocity = Vector3.ProjectOnPlane(velocity, groundInfo.up);

		//set facingDir
		if (swingAxis != Vector3.zero)
        {
			//Vector3 globalFacingDir = groundPivot.TransformDirection(facingDir).normalized;
			//globalFacingDir = Vector3.ProjectOnPlane(globalFacingDir, swingAxis).normalized;

			//Vector3 globalFacingDir = swingPointInfo.transform.forward;
			//facingDir = groundPivot.InverseTransformDirection(globalFacingDir).normalized * swingingFacingDir;
			facingDir = swingPointInfo.transform.forward * swingingFacingDir;

			//Debug.Log(facingDir);
        }
	}



	void UpdateTailHooking (float deltaTime)
	{
		if (isHooked) 
		{
			//update hooked point position
			tailEnd.transform.position = swingPointInfo.transform.position;
		} 
		else if (isHooking && swingPointInfo.transform != null) 
		{

			//keep hooking
			tailEnd.transform.position = Vector3.Lerp (tailEnd.transform.position, swingPointInfo.transform.position, hookingSpeed * deltaTime);

			if ((swingPointInfo.transform.position - tailEnd.transform.position).magnitude < 1f) 
			{
				tailEnd.transform.position = swingPointInfo.transform.position;
				isHooking = false;
				isHooked = true;
				isJumping = false;
				currentTailLength = Mathf.Min ((swingPointInfo.transform.position - transform.position).magnitude, maxTailLength);

				//set info for facingDir
				if (swingPointInfo.directionType == SwingPointInfo.DirectionType.FixedDirection)
                {
					Vector3 swingAxis = swingPointInfo.transform.right;
					Vector3 globalFacingDir = groundPivot.TransformDirection(facingDir).normalized;

					if (globalFacingDir == swingAxis || globalFacingDir == -swingAxis)
                    {
						globalFacingDir = swingPointInfo.transform.forward;
						facingDir = groundPivot.InverseTransformDirection(globalFacingDir);
                    }
					else
                    {
						globalFacingDir = Vector3.ProjectOnPlane(globalFacingDir, swingAxis);
						facingDir = groundPivot.InverseTransformDirection(globalFacingDir);
					}

					swingingFacingDir = (int)Mathf.Sign(ExtVector3.MagnitudeInDirection(globalFacingDir, swingPointInfo.transform.forward));
                }
			}

		} 
		else if (isUnhooking) 
		{
			//keep unhooking
			tailEnd.transform.position = Vector3.Lerp (tailEnd.transform.position, transform.position, hookingSpeed * deltaTime);

			if ((transform.position - tailEnd.transform.position).magnitude < 1f) 
			{
				tailEnd.transform.position = transform.position;
				isUnhooking = false;
			}
		} 
		else 
		{
			//update default position
			tailEnd.position = transform.position;
		}
	}


	bool ChangeTailLength (float deltaTime) 
	{
		float startingTailLength = currentTailLength;

		float inp = Input.GetAxisRaw ("ChangeTailLength");

		if (inp != 0) 
		{
			currentTailLength += -Mathf.Sign(inp) * 3 * deltaTime;
		}

		currentTailLength = Mathf.Min (currentTailLength, maxTailLength);
		currentTailLength = Mathf.Max (currentTailLength, minTailLength);

		if (currentTailLength > startingTailLength)
        {
			return true; //tail has been stretch
        }

		return false;
	}

	void Walk (float deltaTime) 
	{
		bool hittingWall = (hitNormals.Count > 0) ? true : false;
		
		Vector3 moveInput = playerInput.GetRawInput();
		float inputMag = playerInput.GetInputMag();
		Vector3 rawInputDir = moveInput.normalized;
		//Vector3 smoothedInputDir = playerInput.GetSmoothedInput().normalized;


		//Debug.Log ("--IN WALK-- x: " + lateralVelocity.x + ", z:" + lateralVelocity.z);


		if (groundInfo.GetIsGrounded()) 
		{
			velocity = GroundMovement (deltaTime, rawInputDir, inputMag, velocity);

		} 
		else 
		{
			velocity = AirMovement (deltaTime, rawInputDir, velocity);
		}


		//Debug.Log("Speed: " + velocity.magnitude);
	}


	Vector3 GroundMovement (float deltaTime, Vector3 rawInputDir, float inputMag, Vector3 passedVelocity)
	{
		Vector3 localVelocity = ExtVector3.InverseTransformDirection(groundInfo.up, passedVelocity);
		Vector3 lateralVelocity =  new Vector3 (localVelocity.x, 0, localVelocity.z);
		Vector3 savedVerticalVelocity = passedVelocity - lateralVelocity;


		// Set slope info
		float slopeAngle = 0;
		SlopeInfo.SlopeType slopeType = SlopeInfo.SlopeType.None;
		Vector3 slopeDir = Vector3.zero;
		float downSlopeMod = 0;

		if (!ignoreSlopePhysics) 
		{
			slopeAngle = Vector3.Angle (-gravityDir, groundInfo.up);
			slopeType = SlopeInfo.GetSlopeType (slopeAngle);
			slopeDir = ExtVector3.InverseTransformDirection(groundInfo.up, gravityDir);
			slopeDir.y = 0;
			slopeDir = slopeDir.normalized;

			downSlopeMod = 1 - Mathf.Max (0, ExtVector3.MagnitudeInDirection (groundInfo.up, -gravityDir));  //angles greater than 90 return 1
		}

		if (SlopeInfo.IsSlopeSteepOrUp(slopeAngle) && slopeDir != Vector3.zero && inputMag <= walkInputTolerance)
        {
			// We are walking and still inputting to walk, but we're on a steep slope, so make the input 0 because we can't walk on a steep slope
			rawInputDir = Vector3.zero;
        }


		float acceleration = (groundInfo.groundType == GroundType.Ice) ? iceAcceleration : normalAcceleration;
		float deceleration = (groundInfo.groundType == GroundType.Ice) ? iceDeceleration : normalDeceleration;



		// Check to see if should skid first
		if (rawInputDir != Vector3.zero && (ExtVector3.Angle (rawInputDir, lateralVelocity.normalized) > skidAngle) && (ExtVector3.Angle (rawInputDir, facingDir) > skidAngle)) 
		{
			//if (lateralVelocity.magnitude >= 6f)  is this necessary?
			isSkidding = true;
		}

		bool isSkiddingDownSlope = false;

		// Check to see if we should stop skidding (and stop if so)
		if (isSkidding) 
		{
			float skidDeceleration = (groundInfo.groundType == GroundType.Ice)? iceSkidDeceleration : normalSkidDeceleration;
			skidDeceleration = skidDeceleration + slopeAdditionalDeceleration * downSlopeMod * ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, -slopeDir);

			if (SlopeInfo.IsSlopeSteepOrUp(slopeAngle) && slopeDir != Vector3.zero) //if on steep slope
			{
				if (ExtVector3.MagnitudeInDirection (lateralVelocity, slopeDir) <= 0.00001f) 
				{
					// Only stop skidding if going up or sideways to the slope
					lateralVelocity = ExtVector3.CustomLerpVector (lateralVelocity, Vector3.zero, skidDeceleration, deltaTime, 0.3f, false);

					if (Mathf.Approximately (lateralVelocity.magnitude, 0)) 
					{
						lateralVelocity = Vector3.zero;
						isSkidding = false;
					} 
				}
				else 
				{
					isSkiddingDownSlope = true;
				}
			}
			else 
			{
				lateralVelocity = ExtVector3.CustomLerpVector (lateralVelocity, Vector3.zero, skidDeceleration, deltaTime, 0.3f, false);

				if (Mathf.Approximately (lateralVelocity.magnitude, 0)) 
				{
					lateralVelocity = Vector3.zero;
					isSkidding = false;
				}
			}
		} 


		if (rawInputDir != Vector3.zero && !isSkiddingDownSlope) 
		{
			if (!isSkidding) 
			{
				float vMod = 1;
				float speedLimit = topSpeed;


				// Turn around straightaway if now inputting in direction moving backwards in
				if (!Mathf.Approximately (lateralVelocity.magnitude, 0) && !IsMovingInFacingDir (lateralVelocity)) 
				{
					float angle = Vector3.Angle (facingDir, rawInputDir);

					if (angle >= 110f && Vector3.Angle (facingDir, -lateralVelocity.normalized) < 0.01f) // Give a little bit of leeway for 90 degrees
					{  
						facingDir = lateralVelocity.normalized;
					}
				}


				// Stuff to do if speed is 0
				if (Mathf.Approximately (lateralVelocity.magnitude, 0)) 
				{
					if (SlopeInfo.IsSlopeSteepOrUp(slopeAngle) && slopeDir != Vector3.zero) // On a steep slope
					{
						lateralVelocity += 0.001f * facingDir;
					} 
					else 
					{
						lateralVelocity += 0.001f * rawInputDir.normalized;
						facingDir = rawInputDir;
					}
				} 


				float baseInputTurnSpeed = groundTurnSpeed;
				float inputTurnSpeed = (lateralVelocity.magnitude < 1f) ? 50 : baseInputTurnSpeed * 0.8f;

				Vector3 targetDir = rawInputDir;

				// Limit the turning when going backwards down the slope
				if (SlopeInfo.IsSlopeSteepOrUp(slopeAngle) && slopeDir != Vector3.zero)  // On a steep slope
				{ 
					if (!IsMovingInFacingDir (lateralVelocity)) 
					{
						float angle = Vector3.SignedAngle (targetDir, -slopeDir, Vector3.up);
						if (Mathf.Abs (angle) > 30) 
						{
							targetDir = (Quaternion.AngleAxis (30*-Mathf.Sign(angle), Vector3.up) * -slopeDir).normalized;
						}
					}
				}


				// Turn facing dir
				int dir = (IsMovingInFacingDir (lateralVelocity)) ? 1 : -1;
				//facingDir = ExtVector3.CustomMoveTowardsAngleFromVector (facingDir, targetDir, inputTurnSpeed, deltaTime, Vector3.up, lateralVelocity).normalized;
				//facingDir = ExtVector3.CustomLerpAngleFromVector(facingDir, targetDir, inputTurnSpeed, deltaTime, Vector3.up, 1f).normalized;
				facingDir = TurnDirection(facingDir, targetDir, inputTurnSpeed, deltaTime, Vector3.up).normalized;

				// Turn velocity dir
				float newTurnSpeed = (groundInfo.GetIsGrounded() && groundInfo.groundType == GroundType.Ice) ? iceTurnSpeed : inputTurnSpeed;
				Vector3 originalLateralVelocity = lateralVelocity;
				dir = (IsMovingInFacingDir (lateralVelocity)) ? 1 : -1;
				Vector3 trueLateralVelocityDir = lateralVelocity.normalized * dir;
				//lateralVelocity = ExtVector3.CustomMoveTowardsAngleFromVector (trueLateralVelocityDir, targetDir, newTurnSpeed, deltaTime, Vector3.up, lateralVelocity).normalized;
				//lateralVelocity = ExtVector3.CustomLerpAngleFromVector(trueLateralVelocityDir, targetDir, newTurnSpeed, deltaTime, Vector3.up, 1f).normalized;
				lateralVelocity = TurnDirection(trueLateralVelocityDir, targetDir, newTurnSpeed, deltaTime, Vector3.up).normalized;
				float magInDir = ExtVector3.MagnitudeInDirection (originalLateralVelocity, lateralVelocity.normalized, false);
				lateralVelocity = lateralVelocity.normalized * originalLateralVelocity.magnitude * dir;

				vMod = (Vector3.Lerp (lateralVelocity.normalized, rawInputDir, ((groundInfo.groundType == GroundType.Ice)? iceTurnSpeed : baseInputTurnSpeed * 0.6f) * deltaTime)).magnitude;

				int direction = IsMovingInFacingDir(lateralVelocity)? 1 : -1;
				VelocityAgainstWallsNormalsInfo info = SetWallsInfo(false, facingDir * direction, collisionInfo.wallPoints, groundInfo.up, gravityDir);
				bool hitWall = info.infoSet;

				// Deal with walls
				if (hitWall) 
				{
					vMod = 1;
					Vector3 vBefore = lateralVelocity;
					Vector3 wallNormal = GetWallNormalInDir (info).normalized;

					if (wallNormal == Vector3.zero) 
					{
						// V-shape
						lateralVelocity = Vector3.zero;
						speedLimit = 0;

					} 
					else 
					{
						lateralVelocity = vBefore.magnitude * Vector3.ProjectOnPlane (lateralVelocity, wallNormal).normalized;

						float mod = 1 - ExtVector3.MagnitudeInDirection (facingDir * direction, -wallNormal, true);

						if (Vector3.Angle (facingDir * direction, -wallNormal) <= GetWallAngleZero()) 
						{
							mod = 0;
						}

						speedLimit = speedLimit * Mathf.Pow (mod, 1.2f);
						acceleration = acceleration * mod;
					}

					if (SlopeInfo.IsSlopeSteepOrUp(slopeAngle) && slopeDir != Vector3.zero) // On steep slope
					{
						if (Vector3.Angle(slopeDir, -wallNormal) > GetWallAngleZero() && Vector3.Angle(lateralVelocity.normalized, slopeDir) < 90)
                        {
							// No speed limit in this situation
							speedLimit = topSpeed;
							vMod = 1;
							acceleration = (groundInfo.groundType == GroundType.Ice) ? iceAcceleration : normalAcceleration;
						}
					}

				}


				if (!Mathf.Approximately (lateralVelocity.magnitude, 0)) 
				{
					// Accelerate

					dir = (IsMovingInFacingDir (lateralVelocity)) ? 1 : -1;
					acceleration = acceleration * ((dir == -1) ? 1.6f : 1);  // Increase it a little for trying to regain forward direction 

					if (dir == -1 && ExtVector3.MagnitudeInDirection (lateralVelocity, slopeDir) > 0.0001f ) // If facing forwards but moving backwards down slope 
					{
						float rawInputAgainstSlope = Vector3.Angle (rawInputDir, -slopeDir);
						if (rawInputAgainstSlope > 45 && SlopeInfo.IsSlopeSteepOrUp(slopeAngle) && slopeDir != Vector3.zero) // and slope is steep and angle is this
						{
							// Cannot regain speed forwards unless you input up the slope, so make it accelerate down the slope instead
							acceleration = -(acceleration + slopeAdditionalAcceleration * downSlopeMod * ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, slopeDir)) * 0.8f;
							vMod = 1;
						} 
						else 
						{
							// This is to keep it consistent to that trying to regain forward direction when going backwards is the same for any backwards angle
							acceleration = acceleration + slopeAdditionalAcceleration * downSlopeMod * -1;
						}
					} 
					else 
					{
						acceleration = acceleration + slopeAdditionalAcceleration * downSlopeMod * ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, slopeDir);
					}



					float currentSpeed = lateralVelocity.magnitude * dir;
					float newSpeed = currentSpeed;

					bool belowWalkSpeed = (Mathf.Abs(currentSpeed) < walkSpeed || Mathf.Approximately (Mathf.Abs(currentSpeed), walkSpeed));

					// Accelerate and speed limit stuff
					if (dir == 1) 
					{
						if (currentSpeed < speedLimit) 
						{
							newSpeed = Mathf.Min(currentSpeed + acceleration * deltaTime, speedLimit);
						} 
						else 
						{
							if (speedLimit == topSpeed)
                            {
								speedLimit = GetMaxSpeed();
                            }

							if (currentSpeed > speedLimit)
                            {
								if (groundInfo.groundType == GroundType.Ice)
								{
									newSpeed = ExtMathf.CustomLerpFloat(newSpeed, speedLimit, 5, deltaTime, 0.2f);
								}
								else
								{
									newSpeed = speedLimit;
								}
							}
							
						}
					} 
					else 
					{
						if (speedLimit < topSpeed) // If a limit exists
						{
							if (groundInfo.groundType == GroundType.Ice) 
							{
								newSpeed = ExtMathf.CustomLerpFloat (newSpeed, speedLimit, 5, deltaTime, 0.2f);
							} 
							else 
							{
								if (currentSpeed < -speedLimit) 
								{
									newSpeed = -speedLimit;
								} 
								else 
								{
									newSpeed = (currentSpeed + acceleration * deltaTime);
								}
							}

						} 
						else if (currentSpeed < speedLimit)
						{
							newSpeed = Mathf.Min (currentSpeed + acceleration * deltaTime, speedLimit);
						}
					}

					if (belowWalkSpeed && (inputMag <= walkInputTolerance))
                    {
						// We should only walk, unless we were already running then it doesn't matter
						newSpeed = Mathf.Min(newSpeed, walkSpeed);
                    }



					// Apply the turning speed mod
					float minSpeedAfterMod = 4;
					if (Mathf.Abs (newSpeed) > minSpeedAfterMod) 
					{
						newSpeed = newSpeed * Mathf.Pow (vMod, 1.6f);

						// Stops the speed going below a minimum speed after applying
						if (Mathf.Abs (newSpeed) < minSpeedAfterMod) 
						{
							newSpeed = minSpeedAfterMod * dir;
						}
					}

					lateralVelocity = newSpeed * lateralVelocity.normalized * dir; //direction cannot change
				}
			}	
		} 
		else 
		{
			// No input

			float turnToSlopeSpeed = 8;

			if (SlopeInfo.IsSlopeSteepOrUp(slopeAngle)) 
			{
				if (slopeDir != Vector3.zero) // Could be a perfectly flat ceiling
				{
					float velocityToSlopeDir = Vector3.Angle (lateralVelocity.normalized, slopeDir);

					if (velocityToSlopeDir <= 110) 
					{
						// Speed is down slope or slightly up it

						float facingToSlopeDir = Vector3.Angle (facingDir, slopeDir);

						if (facingToSlopeDir > 90.0001f) 
						{
							// Facing up slope

							int dir = -1;

							if (velocityToSlopeDir < 90) 
							{
								// Going down or sideways
								facingDir = ExtVector3.CustomLerpAngleFromVector (facingDir, -slopeDir, turnToSlopeSpeed, deltaTime, Vector3.up, 1f).normalized;
							} 
							else 
							{
								// Going slightly up
								facingDir = ExtVector3.CustomLerpAngleFromVector (facingDir, slopeDir, turnToSlopeSpeed, deltaTime, Vector3.up, 1f).normalized;
								dir = 1;
							}



							if (Mathf.Approximately (lateralVelocity.magnitude, 0))
							{
								lateralVelocity = 0.001f * -facingDir.normalized;
							}

							lateralVelocity = ExtVector3.CustomLerpAngleFromVector (lateralVelocity.normalized, slopeDir, turnToSlopeSpeed, deltaTime, Vector3.up, 1f).normalized * lateralVelocity.magnitude;

							acceleration = acceleration + slopeAdditionalAcceleration * downSlopeMod * ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, slopeDir);
							lateralVelocity += acceleration * deltaTime * facingDir * dir;
						} 
						else 
						{
							// Facing sideways or down slope

							if (IsMovingInFacingDir (lateralVelocity)) 
							{
								facingDir = ExtVector3.CustomLerpAngleFromVector (facingDir, slopeDir, turnToSlopeSpeed, deltaTime, Vector3.up, 1f).normalized;
							} 
							else 
							{
								facingDir = ExtVector3.CustomLerpAngleFromVector (facingDir, slopeDir, turnToSlopeSpeed, deltaTime, Vector3.up, 1f).normalized; //minus slopeDir??
							}


							if (Mathf.Approximately (lateralVelocity.magnitude, 0))
							{
								lateralVelocity = 0.001f * facingDir.normalized;
							}

							lateralVelocity = ExtVector3.CustomLerpAngleFromVector (lateralVelocity.normalized, slopeDir, turnToSlopeSpeed, deltaTime, Vector3.up, 1f).normalized * lateralVelocity.magnitude;

							acceleration = acceleration + slopeAdditionalAcceleration * downSlopeMod * ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, slopeDir);
							lateralVelocity += acceleration * deltaTime * facingDir;
						}

						int direction = IsMovingInFacingDir(lateralVelocity)? 1 : -1;
						VelocityAgainstWallsNormalsInfo info = SetWallsInfo(false, facingDir * direction, collisionInfo.wallPoints, groundInfo.up, gravityDir);
						bool hitWall = info.infoSet;
						float speedLimit = topSpeed;

						if (hitWall) 
						{
							Vector3 vBefore = lateralVelocity;
							Vector3 wallNormal = GetWallNormalInDir (info).normalized;

							if (wallNormal == Vector3.zero) 
							{
								// V-shape
								lateralVelocity = Vector3.zero;
								speedLimit = 0;

							}
							else 
							{
								lateralVelocity = vBefore.magnitude * Vector3.ProjectOnPlane (lateralVelocity, wallNormal).normalized;

								float mod = 1 - ExtVector3.MagnitudeInDirection (facingDir * direction, -wallNormal, true);

								if (Vector3.Angle (facingDir * direction, -wallNormal) <= GetWallAngleZero()) 
								{
									mod = 0;
								}

								speedLimit = speedLimit * Mathf.Pow (mod, 1.2f);
							}

							float speed = lateralVelocity.magnitude;
							if (speed > speedLimit && Vector3.Angle (slopeDir, -wallNormal) <= GetWallAngleZero() && speedLimit != topSpeed) 
							{
								speed = speedLimit;
							}
							lateralVelocity = lateralVelocity.normalized * speed;

						}

					}
					else 
					{
						// Going up the slope

						// Decelerate quicker
						deceleration = deceleration + slopeAdditionalDeceleration * downSlopeMod * ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, -slopeDir);
						lateralVelocity = ExtVector3.CustomLerpVector (lateralVelocity, Vector3.zero, deceleration, deltaTime, 0.4f, false);
					}
						
				}
				else 
				{
					// Decelerate normally on ceiling
					deceleration = deceleration + slopeAdditionalDeceleration * downSlopeMod * ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, -slopeDir);
					lateralVelocity = ExtVector3.CustomLerpVector (lateralVelocity, Vector3.zero, deceleration, deltaTime, 0.2f, false);
				}
			}
			else 
			{
				// Decelerate normally
				deceleration = deceleration + slopeAdditionalDeceleration * downSlopeMod * ExtVector3.MagnitudeInDirection (lateralVelocity.normalized, -slopeDir);
				lateralVelocity = ExtVector3.CustomLerpVector (lateralVelocity, Vector3.zero, deceleration, deltaTime, 0.2f, false);
			}
		}



		// Fall off wall if speed too low
		if (slopeType == SlopeInfo.SlopeType.SuperSteep) 
		{
			float minStickSpeed = CollisionController.GetFallOffSlopeSpeedThreshold();
			if ((slopeDir == Vector3.zero && lateralVelocity.magnitude < minStickSpeed) || (ExtVector3.MagnitudeInDirection (lateralVelocity, -slopeDir, true) >= -0.0001f && lateralVelocity.magnitude < minStickSpeed)) 
			{
				SetGrounded (false);

				localVelocity = ExtVector3.InverseTransformDirection(groundInfo.up, passedVelocity);
				lateralVelocity =  new Vector3 (localVelocity.x, 0, localVelocity.z);
			}

		}


		/*if (lateralVelocity.magnitude >= topSpeed) 
		{
			lateralVelocity = topSpeed * lateralVelocity.normalized;
		}*/

		localVelocity = new Vector3 (lateralVelocity.x, localVelocity.y, lateralVelocity.z);
		passedVelocity = ExtVector3.TransformDirection(groundInfo.up, localVelocity);

		return passedVelocity;
	}
		
		

	Vector3 AirMovement (float deltaTime, Vector3 rawInputDir, Vector3 passedVelocity)
	{
		Vector3 localVelocity = ExtVector3.InverseTransformDirection(groundInfo.up, passedVelocity);
		Vector3 lateralVelocity =  new Vector3 (localVelocity.x, 0, localVelocity.z);
		Vector3 savedVerticalVelocity = passedVelocity - lateralVelocity;

		float acceleration = normalAcceleration;

		if (rawInputDir != Vector3.zero) 
		{
			float baseInputTurnSpd = airTurnSpeed;
			float inputTurnSpd;


			// Turn around straightaway if now inputting in direction moving backwards in
			if (!Mathf.Approximately(lateralVelocity.magnitude, 0) && !IsMovingInFacingDir(lateralVelocity))
			{
				float angle = Vector3.Angle(facingDir, rawInputDir);

				if (angle > 90f) 
				{
					facingDir = lateralVelocity.normalized;
				}
			}

			// Stuff to do if speed is 0
			if (Mathf.Approximately(lateralVelocity.magnitude, 0))
			{
				lateralVelocity += 0.001f * rawInputDir.normalized;
				facingDir = rawInputDir;
			}

			// Decelerating backwards is like skidding but without input locking
			bool deceleratingBackwards = IsMovingInFacingDir(lateralVelocity) && (Vector3.Angle(facingDir.normalized, rawInputDir) > skidAngle) && (Vector3.Angle(lateralVelocity.normalized, rawInputDir) > skidAngle);

			float vMod = 1;

			if (!deceleratingBackwards)
            {
				// Turn facing dir
				inputTurnSpd = baseInputTurnSpd;
				facingDir = ExtVector3.CustomLerpAngleFromVector(facingDir, rawInputDir.normalized, inputTurnSpd, deltaTime, Vector3.up, 1f).normalized;

				// Turn velocity dir
				if (ExtVector3.MagnitudeInDirection(lateralVelocity, facingDir) < 0 && ExtVector3.MagnitudeInDirection(rawInputDir, facingDir) > 0)
				{
					Vector3 newV = ExtVector3.CustomLerpVector(lateralVelocity.normalized, rawInputDir.normalized, inputTurnSpd, deltaTime, 0.1f, false);
					lateralVelocity = newV * lateralVelocity.magnitude;

					vMod = (Vector3.Lerp(lateralVelocity.normalized, rawInputDir, 12 * deltaTime)).magnitude;
				}
				else
				{
					Vector3 newV = ExtVector3.CustomLerpAngleFromVector(lateralVelocity.normalized, rawInputDir.normalized, inputTurnSpd, deltaTime, Vector3.up, 1f);
					lateralVelocity = newV.normalized * lateralVelocity.magnitude;

					vMod = (Vector3.Lerp(lateralVelocity.normalized, rawInputDir, 12 * deltaTime)).magnitude;
				}
			}

			

			// Deal with walls
			VelocityAgainstWallsNormalsInfo info = SetWallsInfo(false, facingDir, collisionInfo.wallPoints, groundInfo.up, gravityDir);
			bool hitWall = info.infoSet;
			float speedLimit = topSpeed;

			if (hitWall) 
			{
				vMod = 1;
				Vector3 vBefore = lateralVelocity;
				Vector3 wallNormal = GetWallNormalInDir (info).normalized;

				if (wallNormal == Vector3.zero) 
				{
					// V-shape
					lateralVelocity = Vector3.zero;
					speedLimit = 0;

				} 
				else 
				{
					lateralVelocity = vBefore.magnitude * Vector3.ProjectOnPlane (lateralVelocity, wallNormal).normalized;

					float mod = 1 - ExtVector3.MagnitudeInDirection (facingDir, -wallNormal, true);

					if (Vector3.Angle (facingDir, -wallNormal) <= GetWallAngleZero()) 
					{
						mod = 0;
					}

					speedLimit = speedLimit * Mathf.Pow (mod, 1.2f);
					acceleration = acceleration * mod;
				}
			}


			if (!Mathf.Approximately (lateralVelocity.magnitude, 0)) 
			{
				if (!deceleratingBackwards)
                {
					// Accelerate
					int dir = (IsMovingInFacingDir(lateralVelocity)) ? 1 : -1;
					acceleration = acceleration * ((dir == -1) ? 1 : 1);  // Can be modified for regaining the forward direction

					float currentSpeed = lateralVelocity.magnitude * dir;
					float newSpeed = currentSpeed;

					// Accelerate and speed limit stuff
					if (dir == 1)
					{
						if (currentSpeed < speedLimit)
						{
							newSpeed = Mathf.Min(currentSpeed + acceleration * deltaTime, speedLimit);
						}
						else
						{
							if (speedLimit == topSpeed)
							{
								speedLimit = GetMaxSpeed();
							}

							// Stop speed exceeding speed limit
							if (currentSpeed > speedLimit)
							{
								newSpeed = speedLimit;
							}

						}
					}
					else
					{
						if (Mathf.Abs(currentSpeed) < speedLimit)
						{
							newSpeed = Mathf.Max(currentSpeed + acceleration * deltaTime, -speedLimit);
						}
						else
						{
							if (speedLimit == topSpeed)
							{
								speedLimit = GetMaxSpeed();
							}

							// Stop speed exceeding speed limit
							if (currentSpeed < -speedLimit)
							{
								newSpeed = -speedLimit;
							}
						}
					}

					// Apply turning speed mod
					float minSpeedAfterMod = 0;
					if (Mathf.Abs(newSpeed) > minSpeedAfterMod && dir == 1)
					{
						newSpeed = newSpeed * Mathf.Pow(vMod, 1.6f);

						// Stops speed going below a minimum speed after applying
						if (Mathf.Abs(newSpeed) < minSpeedAfterMod)
						{
							newSpeed = minSpeedAfterMod * dir;
						}
					}

					lateralVelocity = newSpeed * lateralVelocity.normalized * dir;
				}
				else
                {
					// Decelerate to 0
					lateralVelocity = ExtVector3.CustomLerpVector(lateralVelocity, Vector3.zero, airBackwardsDeceleration, deltaTime, 0.3f, false);
				}		
			}

		} 
		else 
		{
			//Decelerate

			if (!leavingGround || true) //this is done so that if we are trying to walk off a platform very slowly we will keep enough speed to move over the edge before the gravity gets too high
			{
				//lateralVelocity = ExtVector3.CustomLerpVector (lateralVelocity, Vector3.zero, airDeceleration, deltaTime, 0.2f, false);
				float speed = lateralVelocity.magnitude;
				speed = Mathf.Max (speed - airDeceleration * deltaTime, 0);
				lateralVelocity = speed * lateralVelocity.normalized;
			}

			//if there is no input turn towards the velocity dir
			//int direction = (IsMovingInFacingDir (lateralVelocity))? 1 : -1;
			//facingDir = ExtVector3.CustomLerpAngleFromVector (facingDir, lateralVelocity.normalized * direction, 10, deltaTime, Vector3.up, 1f).normalized;
		}


		/*if (lateralVelocity.magnitude >= topSpeed)
		{
			lateralVelocity = topSpeed * lateralVelocity.normalized;
		}*/

		localVelocity = new Vector3 (lateralVelocity.x, localVelocity.y, lateralVelocity.z);
		passedVelocity = ExtVector3.TransformDirection(groundInfo.up, localVelocity);

		return passedVelocity;
	}



	public void SetNewFacingDir (Vector3 lateralVelocity = new Vector3())
	{
		/*if (lateralVelocity == Vector3.zero) 
		{
			Vector3 localVelocity = groundPivot.InverseTransformDirection (velocity);
			lateralVelocity =  new Vector3 (localVelocity.x, 0, localVelocity.z);

			//lateralVelocity = Vector3.ProjectOnPlane (velocity, groundPivot.up);
		}*/


		if (lateralVelocity != Vector3.zero) 
		{
			if (groundInfo.GetIsGrounded()) 
			{
				if (groundInfo.groundType == GroundType.Normal || true)
				{
					if (IsMovingInFacingDir (lateralVelocity)) 
					{
						facingDir = lateralVelocity.normalized;
						//Debug.Log (lateralVelocity);
					} 
					else 
					{
						facingDir = -lateralVelocity.normalized;
					}

					//progressiveInput = facingDir;
				}

			} 
			else 
			{
				if (IsMovingInFacingDir (lateralVelocity)) 
				{
					facingDir = lateralVelocity.normalized;
					//Debug.Log (lateralVelocity);
				} 
				else 
				{
					facingDir = -lateralVelocity.normalized;
				}
			}
		}
			
	}


	public bool IsMovingInFacingDir (Vector3 lateralVelocity)
	{
		float velocityMagInFacingDir = ExtVector3.MagnitudeInDirection (lateralVelocity, facingDir, true);
		return (velocityMagInFacingDir >= 0) ? true : false;
	}


	void Jump (float deltaTime) 
	{
		if(groundInfo.canJump && input.GetButtonDown(GeneralInput.AxesNames.Jump) && !isJumping)
		{
			isJumping = true;
			justStartedJumping = true;
			groundInfo.canJump = false;
			currentlyUsingBooster = false;
			if (cUseGroundBooster != null)
				StopCoroutine (cUseGroundBooster);

			Vector3 prevUp = groundInfo.up;

			Vector3 velocityToUseForCheck = velocity;
			//0 the velocity in the vertical direction is moving downwards so that a jump still gives the full height it should
			if (ExtVector3.IsInDirection (velocity, gravityDir)) 
			{
				velocity = Vector3.ProjectOnPlane (velocity, gravityDir);
			}


			if (groundInfo.GetIsGrounded()) 
			{
				float slopeAngle = 0;
				Vector3 slopeDir = groundInfo.up;
				SlopeInfo.SlopeType slopeType = SlopeInfo.SlopeType.None;

				if (!ignoreSlopePhysics) 
				{
					slopeAngle = Vector3.Angle (-gravityDir, groundInfo.up);
					slopeType = SlopeInfo.GetSlopeType (slopeAngle);
					slopeDir = ExtVector3.InverseTransformDirection(groundInfo.up, gravityDir);
					slopeDir.y = 0;
					slopeDir = slopeDir.normalized;

				}

				Vector3 localVelocity = ExtVector3.InverseTransformDirection(groundInfo.up, velocityToUseForCheck);
				Vector3 lateral = new Vector3 (localVelocity.x, 0, localVelocity.z);

				if (groundInfo.GetIsOnStaircase())
                {
					// Do a normal jump with no momentum altering
					velocity += -gravityDir * jumpPower;
                }
				else if (slopeAngle == 0) 
				{
					//flat ground
					velocity += -gravityDir * jumpPower;
				}
				else if (slopeType == SlopeInfo.SlopeType.Shallow) 
				{
					if (ExtVector3.MagnitudeInDirection (lateral.normalized, -slopeDir) > -0.0001f) 
					{
						velocity += -gravityDir * jumpPower;
					} 
					else 
					{
						velocity += groundInfo.up * jumpPower;
					}
				}
				else if (slopeAngle <= 90.01f) 
				{
					if (ExtVector3.MagnitudeInDirection (lateral.normalized, -slopeDir) > -0.0001f) 
					{
						float velocityAgainstGravityDir = ExtVector3.MagnitudeInDirection (velocity, -gravityDir, false);

						float maxAdditionalVelocity = 2.5f;
						velocityAgainstGravityDir = Mathf.Min (maxAdditionalVelocity, velocityAgainstGravityDir);
						velocityAgainstGravityDir = (1 - ExtVector3.MagnitudeInDirection (groundInfo.up, -gravityDir)) * velocityAgainstGravityDir;

						velocity += -gravityDir * jumpPower;


						//0 velocity in slope direction
						Vector3 flattenedSlopeDir = Vector3.ProjectOnPlane(ExtVector3.TransformDirection (groundInfo.up, slopeDir), gravityDir);
						velocity = Vector3.ProjectOnPlane (velocity, flattenedSlopeDir);

						//set height to jumppower + additional velocity from speed
						velocity = Vector3.ProjectOnPlane (velocity, gravityDir);
						velocity += -gravityDir * (jumpPower + velocityAgainstGravityDir);
					} 
					else 
					{
						
						velocity += groundInfo.up * jumpPower;
					}
				}
				else if (SlopeInfo.IsSlopeSteepOrUp (slopeAngle) && slopeDir == Vector3.zero)
				{
					//ceiling
					velocity += groundInfo.up * jumpPower;
				}
				else 
				{
					//slope greater than 90 and not ceiling
					if (ExtVector3.MagnitudeInDirection (lateral.normalized, -slopeDir) > -0.0001f) 
					{
						//0 velocity in slope direction
						Vector3 flattenedSlopeDir = Vector3.ProjectOnPlane(ExtVector3.TransformDirection (groundInfo.up, slopeDir), gravityDir);
						velocity = Vector3.ProjectOnPlane (velocity, flattenedSlopeDir);

						velocity += groundInfo.up * jumpPower;



					} 
					else 
					{
						Vector3 flattenedSlopeDir = Vector3.ProjectOnPlane (slopeDir, gravityDir);
						velocity = Vector3.ProjectOnPlane (velocity, flattenedSlopeDir);

						velocity += groundInfo.up * jumpPower;

					}
				}
			}
			else 
			{
				//not on ground but can jump
				velocity += -gravityDir * jumpPower;
			}

			SetGrounded(false);

			Vector3 dirToAdd = -gravityDir;

			Vector3 lateralVelocity = ExtVector3.InverseTransformDirection(groundInfo.up, velocity);
			lateralVelocity.y = 0;
			//SetNewFacingDir (lateralVelocity);

			cIncreaseJumpHeight = IncreaseJumpHeight (dirToAdd);
			StartCoroutine (cIncreaseJumpHeight);
		}
	}


	IEnumerator IncreaseJumpHeight (Vector3 dirToAdd)
	{
		float jumpingTimer = 0;
		float jumpForce = 20; //increasing this value increases the distance gap between heights

		while (isJumping && input.GetPressed(GeneralInput.AxesNames.Jump) && jumpingTimer <= 0.6f && ExtVector3.IsInDirection (velocity, -gravityDir)) 
		{
			while (sc.gamePaused || sc.gameController.frameByFrame) 
			{
				yield return new WaitForEndOfFrame ();
			}

			velocity += dirToAdd * jumpForce * Time.deltaTime;
			jumpingTimer += Time.deltaTime;

			yield return new WaitForEndOfFrame ();  //remember this means me we use Time.deltaTime and not our other calculated deltaTime
		}
			
		isJumping = false;
		jumpForce = 0;
		jumpingTimer = 0;
	}

	void Gravity (float deltaTime) 
	{
		if (!groundInfo.GetIsGrounded()) 
		{
			//add gravity
			//we limit the fall speed in such a way that if we are already over the fall speed limit it is not reset but we cannot accelerate via gravity above it

			float velocityMagInGravDir = ExtVector3.MagnitudeInDirection (velocity, gravityDir, false);

			if (velocityMagInGravDir < maxGravitySpeed) 
			{
				velocity += gravityStrength * gravityDir * deltaTime;

				velocityMagInGravDir = ExtVector3.MagnitudeInDirection (velocity, gravityDir, false);
				if (velocityMagInGravDir > maxGravitySpeed)
				{
					Vector3 lateral = velocity - velocityMagInGravDir * gravityDir;
					velocity = maxGravitySpeed * gravityDir + lateral;
				}
			}

		}

		//velocity += gravityStrength * -groundPivot.up * deltaTime;
	}

	void Glide (float deltaTime) 
	{

	}




	public IEnumerator UseGroundBooster (GameObject booster)
	{
		currentlyUsingBooster = true;
		BoosterController boosterController = booster.GetComponent<BoosterController> ();

		float boostTimer = 0;

		while (boostTimer < boosterController.useTime) 
		{
			while (sc.gamePaused || sc.gameController.frameByFrame) 
			{
				yield return new WaitForEndOfFrame ();
			}

			Vector3 boosterVelocity = boosterController.GetVelocity ();
			Vector3 boosterDir = Vector3.ProjectOnPlane (boosterVelocity.normalized, groundInfo.up);
			Vector3 vertical = velocity - Vector3.ProjectOnPlane (velocity, groundInfo.up);
			velocity = boosterDir * boosterVelocity.magnitude;
			facingDir = ExtVector3.InverseTransformDirection(groundInfo.up, velocity).normalized;
			velocity += vertical;

			boostTimer += Time.deltaTime;

			yield return new WaitForEndOfFrame ();
		}

		currentlyUsingBooster = false;
	}


	public IEnumerator UseAirBooster (GameObject booster)
	{
		currentlyUsingAirBooster = true;
		AirBoosterController boosterController = booster.GetComponent<AirBoosterController> ();

		SetGrounded(false);
		if (cIncreaseJumpHeight != null)
			StopCoroutine (cIncreaseJumpHeight);

		float boostTimer = 0;

		while (boostTimer < boosterController.useTime) 
		{
			while (sc.gamePaused || sc.gameController.frameByFrame) 
			{
				yield return new WaitForEndOfFrame ();
			}

			Vector3 boosterVelocity = boosterController.GetVelocity ();
			Vector3 boosterDir = boosterVelocity.normalized;
			velocity = boosterDir * boosterVelocity.magnitude;
			Vector3 local = ExtVector3.InverseTransformDirection(groundInfo.up, velocity);
			local.y = 0;

			if (Vector3.Angle (-gravityDir, boosterDir.normalized) > 0.01f && Vector3.Angle (gravityDir, boosterDir.normalized) > 0.01f) //this is just so it doesnt change the facing direction to the wrong way when the direction is perfectly vertical 
			{
				facingDir = local.normalized;
			}


			//align with centre
			Vector3 closestPointOnLine = Geometry.ClosestPointOnLineToPoint (transform.position, booster.transform.position, boosterController.GetVelocity().normalized);
			Vector3 perpendicular = closestPointOnLine - transform.position;
			float perpendicularDistance = perpendicular.magnitude;
			float moveDistance = Mathf.Min (perpendicularDistance * aligningSpeed * Time.deltaTime, perpendicularDistance);
			Vector3 toMove = perpendicular.normalized * moveDistance;
			objectAliginingVelocity = toMove;


			boostTimer += Time.deltaTime;

			yield return new WaitForEndOfFrame ();
		}

		currentlyUsingAirBooster = false;
	}


	public IEnumerator UseSpring (GameObject spring)
	{
		
		currentlyUsingSpring = true;
		SpringController springController = spring.GetComponent<SpringController> ();

		SetGrounded(false);
		if (cIncreaseJumpHeight != null)
			StopCoroutine (cIncreaseJumpHeight);

		float boostTimer = 0;

		while (boostTimer < springController.useTime) 
		{
			while (sc.gamePaused || sc.gameController.frameByFrame) 
			{
				yield return new WaitForEndOfFrame ();
			}

			Vector3 boosterVelocity = springController.GetVelocity ();
			Vector3 boosterDir = boosterVelocity.normalized;
			velocity = boosterDir * boosterVelocity.magnitude;
			Vector3 local = ExtVector3.InverseTransformDirection(groundInfo.up, velocity);
			local.y = 0;

			if (Vector3.Angle (-gravityDir, boosterDir.normalized) > 0.01f && Vector3.Angle (gravityDir, boosterDir.normalized) > 0.01f) //this is just so it doesnt change the facing direction to the wrong way when the direction is perfectly vertical 
			{
				facingDir = local.normalized;
			}


			//align with centre
			Vector3 closestPointOnLine = Geometry.ClosestPointOnLineToPoint (transform.position, spring.transform.position, springController.GetVelocity().normalized);
			Vector3 perpendicular = closestPointOnLine - transform.position;
			float perpendicularDistance = perpendicular.magnitude;
			float moveDistance = Mathf.Min (perpendicularDistance * aligningSpeed * Time.deltaTime, perpendicularDistance);
			Vector3 toMove = perpendicular.normalized * moveDistance;
			objectAliginingVelocity = toMove;

			boostTimer += Time.deltaTime;

			yield return new WaitForEndOfFrame ();
		}

		currentlyUsingSpring = false;

	}
		


	void FollowPathLauncherPath (float deltaTime)
	{
		/*//velocity = currentlyUsingSpring.GetVelocity ();
		velocity = Vector3.zero;
		additionalVelocity = Vector3.zero;

		springPathDistanceTravelled += currentlyUsingSpring.power * deltaTime;


		Vector3 newPos = currentlyUsingSpring.GetNextTargetPosition (springPathDistanceTravelled);
		Vector3 toMove = newPos - transform.position;
		additionalVelocity += toMove;

		bool atEndOfPath = currentlyUsingSpring.CheckIfAtEndOfPath (newPos);

		if (atEndOfPath) 
		{
			velocity = currentlyUsingSpring.GetVelocity ();
			currentlyUsingSpring = null;
			springPathDistanceTravelled = 0;

		}*/
	}


	Vector3 TurnDirection (Vector3 frm, Vector3 to, float turnSpd, float deltaTime, Vector3 axis)
    {
		Vector3 lerpRes = ExtVector3.CustomLerpAngleFromVector(frm, to, turnSpd, deltaTime, axis, 0);
		Vector3 moveTowardsRes = ExtVector3.CustomMoveTowardsAngleFromVector(frm, to, turnSpd, deltaTime, axis);

		float lerpAngle = Vector3.Angle(lerpRes, to);
		float moveTowardsAngle = Vector3.Angle(moveTowardsRes, to);

		if (moveTowardsAngle < lerpAngle)
        {
			return moveTowardsRes;
        }

		return lerpRes;
    }


	public override void PauseAnimator()
	{
		pgc.PauseAnimator();
	}

	public override void UnPauseAnimator()
	{
		pgc.UnPauseAnimator();
	}




	void DebugMove(Vector3 inputDir) 
	{
		currentlyUsingBooster = false;
		currentlyUsingSpring = false;

		Vector3 toMove = Vector3.zero;
		velocity = Vector3.zero; //reset the normal velocity to 0 so we don't keep moving using the pre-debug vector after
		float moveSpeed = 8;

		if(inputDir != Vector3.zero)
		{
			inputDir = transform.TransformDirection (inputDir);
			toMove += inputDir * moveSpeed;
		}

		if (input.GetRawInput(GeneralInput.AxesNames.DebugMoveVertical) > 0) {toMove += moveSpeed * Vector3.up;}
		else if (input.GetRawInput(GeneralInput.AxesNames.DebugMoveVertical) < 0) {toMove += moveSpeed * -Vector3.up;}

		transform.position += toMove * Time.deltaTime;
	}


	public struct CurrentSwingPointInfo 
	{
		public Transform transform;
		public SwingPointInfo.DirectionType directionType;
	}


	public struct LateralVelocityTurnReturnPair
	{
		public Vector3 lateralVelocity;
		public float accelMod;

	}
}
