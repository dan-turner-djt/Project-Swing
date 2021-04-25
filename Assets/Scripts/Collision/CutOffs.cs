using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutOffs : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}


	/*List<SphereCollisionInfo> collisionPointBuffer = new List<SphereCollisionInfo>();
	CollisionInfo CalcCollisVeloc (Vector3 targetVelocity) {

		CollisionInfo collisionInfo = new CollisionInfo();

		Vector3 origin = transform.position;
		Vector3 targetPosition = origin + targetVelocity;
		Vector3 previousHitNormal = Vector3.zero;
		hitNormals.Clear ();
		LayerMask mask = ~ignoreLayers;
		Vector3 transformUp = transform.up;


		//We cut our velocity up into steps so that we never move more than a certain amount of our radius per step.
		//This prevents tunneling and acts as a "Continuous Collision Detection", but is not as good as using a CapsuleCast.
		int steps = 1;
		Vector3 stepVelocity = targetVelocity;
		float distance = Vector3.Distance(origin, targetPosition);

		if(distance > maxRadiusMove)
		{
			steps = Mathf.CeilToInt(distance / maxRadiusMove);
			if(steps > collisionHandleInfo.maxVelocitySteps)
			{
				steps = collisionHandleInfo.maxVelocitySteps;

				#region Debug
				#if UNITY_EDITOR
				//if(infoDebug.printOverMaxVelocitySteps) Debug.LogWarning("PlayerRigidbody GetCollisionSafeVelocity velocity steps is larger than maxVelocitySteps. To avoid major lag we are limiting the amount of steps which means unsafe collision handling.", gameObject);
				#endif
				#endregion
			}

			stepVelocity /= steps;
		}

		//Debug.Log ("Steps: " + steps);


		//Start of main of loops
		int attempts = 0;
		for (int i = 0; i < steps; i++) 
		{
			Vector3 previousOrigin = origin;
			origin += stepVelocity;
			targetPosition = origin;
			float negativeOffset = 0;

			for (attempts = 0; attempts < collisionHandleInfo.maxCollisionCheckIterations; attempts++) 
			{
				Vector3 hitNormal = Vector3.zero;
				bool hasHit = false;

				//It is important for us to have a negativeOffset, otherwise our collision detection methods might keep telling us we are penetrated...
				if(attempts > 0 && attempts < collisionHandleInfo.addNegativeOffsetUntilAttempt) negativeOffset += -smallOffset;

				//Do grounding
				//Vector3 groundAndStepDepenetration = Grounding(previousOrigin, origin, mask);
				//if(groundAndStepDepenetration != Vector3.zero && groundAndStepDepenetration.sqrMagnitude > (negativeOffset).Squared())
				//{
					//hasHit = true;
					//hitNormal = groundNormal;
					//origin = origin + groundAndStepDepenetration;
				//}


				if (CheckOverlaps (currentCollider, origin, transformUp, capsuleHeight + (negativeOffset * 2f), sphereRadius + negativeOffset, ignoreColliders, mask)) {

					//Debug.Log ("hit something");
					List<SphereCollisionInfo> collisionPoints = SphereCollisionDetect.DetectCollisions(currentCollider, origin, transform.up, capsuleHeight, sphereRadius, mask, ignoreColliders, collisionPointBuffer, safeCheckOffset);

					//Debug.Log (collisionPoints.Count);

					if(collisionPoints.Count > 0)
					{
						//if(collisionHandleInfo.tryBlockAtSlopeLimit) TryBlockAtSlopeLimit(collisionPoints);

						//Not tested, but might be a good idea to use this if it works...
						//if(collisionHandleInfo.cleanByIgnoreBehindPlane) SphereCollisionDetect.CleanByIgnoreBehindPlane(collisionPoints);

						#region Debug
						#if UNITY_EDITOR

						if (Input.GetKeyDown (KeyCode.P)) {DrawContactsDebug(collisionPoints, .5f, Color.magenta, Color.green);}

						#endif
						#endregion

						//We do the main depenetration method
						Vector3 depenetration = SphereCollisionDetect.Depenetrate(collisionPoints, collisionHandleInfo.maxDepenetrationIterations);
						depenetration = Vector3.ClampMagnitude(depenetration, maxRadiusMove); //We clamp to make sure we dont depenetrate too much into possibly unsafe areas

						origin = origin + depenetration;

						//if (depenetration != Vector3.zero) 
						//{
							//hitNormal = depenetration.normalized;
							//hitNormals.Add (hitNormal);
						//}

						for (int j = 0; j < collisionPoints.Count; j++) 
						{
							hitNormals.Add (collisionPoints [j].normal);
						}

						hitNormal = (depenetration != Vector3.zero) ? depenetration.normalized : hitNormal;


						//Final check if we are safe, if not then we just move a little and hope for the best.
						if (FinalOverlapCheck (origin, transformUp, capsuleHeight + ((negativeOffset - smallOffset) * 2f), sphereRadius + negativeOffset - smallOffset, ignoreColliders, mask)) 
						{
							Debug.Log ("Still not safe yet");
							origin += (hitNormal * smallOffset);
						}


						hasHit = true;
					}
				} 


				if (hasHit) 
				{
					collisionInfo.attempts++;
					previousHitNormal = hitNormal;
					targetPosition = origin;
				} 
				else 
				{
					//lastSafePosition.position = origin;
					//lastSafePosition.upDirection = transform.up;
					//lastSafePosition.activeCollider = currentCollider;
					break;
				}


			}


			//Vector3 groundAndStepDepenetration = Grounding(previousOrigin, origin, mask);

			//Debug.Log ("Attempts: " + attempts);


			if(attempts >= collisionHandleInfo.maxCollisionCheckIterations)
			{
				//Failed to find a safe spot, breaking out early.
				Debug.Log ("Failed to find a safe spot, breaking out early");
				//origin = lastSafePosition.position;
				break;
			}
		}


		if(attempts < collisionHandleInfo.maxCollisionCheckIterations || collisionHandleInfo.depenetrateEvenIfUnsafe)
		{
			collisionInfo.hasCollided = (collisionInfo.attempts > 0);
			collisionInfo.safeMoveDirection = targetPosition - transform.position;

			//We handle redirecting our velocity. First we just default it to the targetVelocity.
			collisionInfo.velocity = targetVelocity;
			Vector3 currentVelocity = targetVelocity;

			if (!ExtVector3.IsInDirection (targetVelocity, previousHitNormal, tinyOffset, false)) 
			{
				//collisionInfo.velocity = Vector3.ProjectOnPlane (targetVelocity, previousHitNormal);
			}



			for (int i = 0; i < hitNormals.Count; i++) {

				//If we are already moving in a direction that is not colliding with the normal, we dont redirect the velocity.
				if(!ExtVector3.IsInDirection(targetVelocity, previousHitNormal, tinyOffset, false))
					//{
					///If we are on an edge then we dont care if we cant walk on the slope since our grounding will count the edge as a ground and friction will slow us down.
				//if((!isOnEdge && !CanWalkOnSlope(previousHitNormal)) || GoingOverEdge(targetVelocity))
				//{
					//collisionInfo.velocity = Vector3.ProjectOnPlane(targetVelocity, previousHitNormal);
				//}
				//else if(isGrounded)
				//{
					//We flatten our velocity. This helps us move up and down slopes, but also has a bad side effect of not having us fly off slopes correctly.
					collisionInfo.velocity = Vector3.ProjectOnPlane(targetVelocity, transformUp);
				//}

				if (Vector3.Dot (currentVelocity.normalized, -hitNormals [i]) > 0f) 
				{
					collisionInfo.velocity = Vector3.ProjectOnPlane(collisionInfo.velocity, hitNormals[i]);
				}

				//collisionInfo.velocity = Vector3.ProjectOnPlane(collisionInfo.velocity, hitNormals[i]);
				//}
			}

			if (isGrounded) {

				collisionInfo.velocity.y = Mathf.Max (0, collisionInfo.velocity.y);
			}


		}

		if (attempts >= collisionHandleInfo.maxCollisionCheckIterations) 
		{
			Debug.Log ("Player collision has failed!");
			collisionInfo.hasCollided = true;
		}


		return collisionInfo;
	}*/




	//Old walk

	//velocity = localVelocity;

	/*if (inputDir != Vector3.zero) {
			//float speed = (isGrounded) ? walkSpeed * 10f : airSpeed * 10f;

			//AddRelativeForce((inputDirection * speed) * deltaTime, ForceMode.Impulse); //We set it as ForceMode.Impulse since we are multiplying it with deltaTime ourselves within the subUpdater loop


			if (Mathf.Abs(currentSpeed) == 0) 
			{
				facingDir = (inputDir).normalized;
				isSkidding = false;
			}
			else if (!isSkidding)
			{
				float angleDiff = Vector3.Angle (facingDir, inputDir);

				//if (angleDiff < skidAngle) 
				//{
					facingDir = (facingDir + turnSpeed * inputDir * deltaTime).normalized;
				//} 
				//else 
				//{
					//isSkidding = true;
				//}

			}

			float angleDif = Vector3.Angle (facingDir, inputDir);
			float r;
			float newAcceleration = acceleration;

			if (angleDif < skidAngle && !isSkidding) 
			{
				angleDif = 90 - angleDif;
				r = (angleDif / 45) - 1f;

				//lateralVelocity += acceleration * facingDir * deltaTime;
				//currentSpeed = lateralVelocity.magnitude; //this needs to take into account speed in the opposite dir to facingdir
				currentSpeed += newAcceleration * deltaTime;
				currentSpeed = Mathf.Min (currentSpeed, topSpeed);
				lateralVelocity = facingDir * currentSpeed;
				//lateralVelocity = lateralVelocity.normalized * currentSpeed;
				float keepY = velocity.y;
				velocity = transform.TransformDirection(lateralVelocity);
				velocity.y = keepY;

			} 
			else 
			{  
				isSkidding = true;
				//skid
				//r = -1;
				//newAcceleration = r * friction;

				//currentSpeed = lateralVelocity.magnitude;
				currentSpeed = Mathf.Max (0, currentSpeed - friction * deltaTime);
				lateralVelocity = facingDir * currentSpeed;
				float keepY = velocity.y;
				velocity = transform.TransformDirection (lateralVelocity);
				velocity.y = keepY;

			}

			//newAcceleration = acceleration * r;

			//currentSpeed = lateralVelocity.magnitude;
			//currentSpeed += newAcceleration * deltaTime;
			//currentSpeed = Mathf.Min (currentSpeed, topSpeed);
			//lateralVelocity = facingDir * currentSpeed;
			//lateralVelocity += inputDir * acceleration * deltaTime;

			//float keepY = velocity.y;

			//currentSpeed = lateralVelocity.magnitude;
			//lateralVelocity += inputDir * turnSpeed * deltaTime;
			//lateralVelocity = currentSpeed * lateralVelocity.normalized;  //overall speed remains same as before so that it changes direction only

			//currentSpeed = lateralVelocity.magnitude;
			//currentSpeed = Mathf.Min (currentSpeed, maxWalkSpeed);
			//lateralVelocity = lateralVelocity.normalized * currentSpeed;
			//velocity = lateralVelocity;
			//velocity.y = keepY;

		} 

		else {

			float keepY = velocity.y;
			//currentSpeed = lateralVelocity.magnitude;

			if (currentSpeed >= 0) {

				currentSpeed = Mathf.Max (currentSpeed - ((isGrounded) ? deceleration : 0) * deltaTime, 0);
			} 

			else {

				currentSpeed = Mathf.Min (currentSpeed + ((isGrounded) ? deceleration : 0) * deltaTime, 0);
			}


			lateralVelocity = facingDir * currentSpeed;
			velocity = transform.TransformDirection(lateralVelocity);
			velocity.y = keepY;

			//velocity -= velocity.normalized * friction * deltaTime;
		}*/

	//Debug.Log (currentSpeed);


	//By Damizean

	// We assume input is already in the Player's local frame...
	// If there is some input...

	/*if (inputMag != 0.0f) 
		{

			// Fetch velocity in the Player's local frame, decompose into lateral and vertical
			// motion, and decompose lateral motion further into normal and tangential components.
			float keepY = velocity.y;

			var localVelocity = transform.InverseTransformDirection (velocity*deltaTime);
			var lateralVelocity = new Vector3 (localVelocity.x, 0.0f, localVelocity.z);
			var verticalVelocity = new Vector3 (0.0f, localVelocity.y, 0.0f);

			var normalSpeed = Vector3.Dot (lateralVelocity, inputDir);
			var normalVelocity = inputDir * normalSpeed;
			var tangentVelocity = lateralVelocity - normalVelocity;

			// Note: normalSpeed is the magnitude of normalVelocity, with the added
			// bonus that it's signed. If positive, the speed goes towards the same
			// direction than the input :)

			if (normalSpeed < topSpeed) {
				// Accelerate towards the input direction.
				normalSpeed += acceleration * deltaTime * inputMag;
				normalSpeed = Mathf.Min (normalSpeed, topSpeed);
				

				// Rebuild back the normal velocity with the correct modulus.
				if (normalSpeed >= 0f) 
				{
					normalVelocity = inputDir * normalSpeed;
				} 
				else 
				{
					// (Reverse the inpit of inputdirection (on x and z, here)
					normalVelocity = inputDir * normalSpeed;
				}

			}

			// Additionally, we can apply some drag on the tangent directions for
			// tighter control.

			float curvePosTang = (velocity.sqrMagnitude / maxSpeed) / maxSpeed;

			tangentVelocity = Vector3.MoveTowards(tangentVelocity, Vector3.zero, (TangentialDrag * TangDragOverSpeed.Evaluate(curvePosTang)) * deltaTime * inputMag);


			// Compose local velocity back and compute velocity back into the Global frame.
			// You probably want to delay doing this to the end of the physics processing,
			// as transformations can incur into numerical damping of the velocities.
			// The last step is included only for the sake of completeness.

			localVelocity = normalVelocity;
			//localVelocity = normalVelocity + tangentVelocity + verticalVelocity;
			velocity = transform.TransformDirection (localVelocity)/deltaTime;
			velocity.y = keepY;

			//Export nescessary variables

			b_normalSpeed = normalSpeed;
			b_normalVelocity = normalVelocity;
			b_tangentVelocity = tangentVelocity;

			//DEBUG VARIABLES

			Debui.inputDirection = inputDirection;
			Debui.inputMagnitude = inputMagnitude;
			Debui.velocity = rigidbody.velocity;
			Debui.localVelocity = localVelocity;
			Debui.normalSpeed = normalSpeed;
			Debui.normalVelocity = normalVelocity;
			Debui.tangentVelocity = tangentVelocity;

		} 
		else 
		{
			float keepY;

			if (isGrounded) 
			{
				keepY = velocity.y;
				velocity = velocity / deceleration;
				velocity.y = keepY;
			}
		}*/


	/*Vector3 Grounding(Vector3 previousOrigin, Vector3 origin, LayerMask layerMask, Vector3 stepVelocity, float deltaTime)
	{
		bool hit = false;
		float radius = sphereRadius;
		float maxIterations = 1;
		Vector3 depenetration = Vector3.zero;

		Vector3 lateralVelocity = groundPivot.InverseTransformDirection (velocity);
		lateralVelocity.y = 0;

		Vector3 lateralStepVelocity = groundPivot.InverseTransformDirection (stepVelocity/deltaTime);
		lateralStepVelocity.y = 0;

		canJump = false;
		bool _isGrounded = false;
		bool _wasGroundedBefore = groundInfo.isGrounded;
		bool _isOnEdge = false;
		bool _isOnStep = false;
		bool _isOnHardEdge = false;
		bool _isGoingTowardsEdge = false;
		Vector3 _groundNormal = Vector3.zero;
		Vector3 _groundPoint = Vector3.zero;
		Vector3 _edgeNormal = Vector3.zero;
		float _edgeAngle = 0;

		for (int i = 0; i < maxIterations; i++) 
		{
			//the first hit is for handling flat (non edge detected) ground and concave slopes only
			groundCheckOffset = airGroundCheckOffset;
			GroundCastInfo firstHit =  GroundCast(previousOrigin, origin, radius, layerMask, velocity, 1);
			if (firstHit.hasHit && firstHit.onEdge) {

				if (CanWalkOnSlope (firstHit.realNormal, groundPivot.up)) {

					//Debug.Log ("First: onEdge, " + firstHit.edgeAngle + ", edgeNormal: " + firstHit.edgeNormal + ", realNormal: " + firstHit.realNormal + ", interpolatedNormal: " + firstHit.interpolatedNormal);

					bool wrapEdge = ShouldWrapEdge(firstHit, velocity);
					if (!wrapEdge) 
					{
						//hard edge that shouldnt be wrapped
						//Debug.Log ("first hit found a hard edge");
						//Debug.Log ("first not wrap edge");

						if (CanStep(firstHit, origin)) {

							//Debug.Log ("first on step");

							hit = true;
							isJumping = false;
							_isGrounded = true;
							_isOnEdge = true;
							_isOnStep = true;
							_groundNormal = firstHit.realNormal;
							_groundPoint = firstHit.point;
							_edgeNormal = firstHit.edgeNormal;
							_edgeAngle = firstHit.edgeAngle;

							SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));
							depenetration = DepenetrateFromGround (origin, radius, firstHit.point, groundPivot.up);

							continue;
						}

						//checking movement direction to hard edge
						if (GetGoingTowardsEdge (origin, firstHit.point, lateralStepVelocity)) 
						{
							//hit = true;
							//isJumping = false;
							//_isGrounded = true;
							//_isOnEdge = true;
							//_isOnStep = false;
							//_isOnHardEdge = true;
							//_isGoingTowardsEdge = true;
							//_groundNormal = -gravityDir;
							//_groundPoint = firstHit.point;
							//_edgeNormal = firstHit.edgeNormal;
							//_edgeAngle = firstHit.edgeAngle;

							//groundPivot.rotation = Quaternion.FromToRotation (Vector3.up, _groundNormal);
							//depenetration = DepenetrateFromGround (origin, radius, firstHit.point, groundPivot.up);

							_isOnEdge = true;
							_isOnHardEdge = true;
							_isGoingTowardsEdge = true;
							_groundPoint = firstHit.point;
							_edgeNormal = firstHit.edgeNormal;
							_edgeAngle = firstHit.edgeAngle;

							Debug.Log ("going towards hard egde");

							continue;
						} 
						else 
						{	
							//hit = true;
							//isJumping = false;
							//_isGrounded = true;
							//_isOnEdge = true;
							//_isOnStep = false;
							//_isOnHardEdge = true;
							//_isGoingTowardsEdge = false;
							//_groundNormal = -gravityDir;
							//_groundPoint = firstHit.point;
							//_edgeNormal = firstHit.edgeNormal;
							//_edgeAngle = firstHit.edgeAngle;

							//groundPivot.rotation = Quaternion.FromToRotation (Vector3.up, _groundNormal);
							//depenetration = DepenetrateFromGround (origin, radius, firstHit.point, groundPivot.up);
							//Vector3 localDepen = groundPivot.InverseTransformDirection(depenetration);
							//localDepen.y = Mathf.Max (0, localDepen.y);
							//depenetration = groundPivot.TransformDirection (localDepen);

							//continue;

							_isOnEdge = true;
							_isOnHardEdge = true;
							_isGoingTowardsEdge = false;
							_groundPoint = firstHit.point;
							_edgeNormal = firstHit.edgeNormal;
							_edgeAngle = firstHit.edgeAngle;


							Debug.Log ("going away from hard egde");
						}

						continue;
					}


					_isOnEdge = true;
					_edgeNormal = firstHit.edgeNormal;
					_edgeAngle = firstHit.edgeAngle;

					hit = true;
					_isGrounded = true;
					_groundNormal = firstHit.normal;
					_groundPoint = firstHit.point;

					isJumping = false;

					if (Input.GetKeyDown (KeyCode.K)) 
					{
						//DrawGroundDebug (secondHit.point, secondHit.normal, 1, Color.cyan, Color.green);
					}

					SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));
					depenetration = DepenetrateFromGround (origin, radius, firstHit.point, groundPivot.up);

					continue;
				}



			}

			if (firstHit.hasHit && !firstHit.onEdge) 
			{
				if (CanWalkOnSlope (firstHit.realNormal, groundPivot.up)) 
				{
					hit = true;
					_isGrounded = true;
					_groundNormal = firstHit.realNormal;
					_groundPoint = firstHit.point;

					isJumping = false;

					if (Input.GetKeyDown (KeyCode.K)) 
					{
						//DrawGroundDebug (firstHit.point, firstHit.normal, 1, Color.cyan, Color.green);
					}

					SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));
					depenetration = DepenetrateFromGround (origin, radius, firstHit.point, groundPivot.up);

					continue;
				}
			} 

			if (groundInfo.isGrounded) 
			{
				//the second hit is for handling convex edges and steps
				groundCheckOffset = (groundInfo.isGrounded)?groundedGroundCheckOffset : airGroundCheckOffset;
				GroundCastInfo secondHit = GroundCast(previousOrigin, origin, radius, layerMask, velocity, 2);
				if (secondHit.hasHit) 
				{
					if (CanWalkOnSlope (secondHit.realNormal, groundPivot.up)) 
					{
						//DrawGroundDebug (secondHit.point, secondHit.normal, 1, Color.green, Color.green);

						hit = true;
						_isOnEdge = secondHit.onEdge;
						if (_isOnEdge) 
						{
							//Debug.Log ("Second: onEdge, " + secondHit.edgeAngle + ", edgeNormal: " + secondHit.edgeNormal + ", realNormal: " + secondHit.realNormal + ", interpolatedNormal: " + secondHit.interpolatedNormal);

							bool wrapEdge = ShouldWrapEdge(secondHit, velocity);
							if (!wrapEdge)
							{
								//hard edge that cant be wrapped
								//Debug.Log ("second not wrap edge");

								if (CanStep (secondHit, origin)) {

									//Debug.Log ("second on step");

									isJumping = false;
									_isGrounded = true;
									_isOnEdge = true;
									_isOnStep = true;
									_groundNormal = secondHit.realNormal;
									_groundPoint = secondHit.point;
									_edgeNormal = secondHit.edgeNormal;
									_edgeAngle = secondHit.edgeAngle;

									SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));
									depenetration = DepenetrateFromGround (origin, radius, secondHit.point, groundPivot.up);

									continue;
								}

								//any other hard edge should have be dealt with by the first hit already, and it shouldnt detect hard edges that are significantly lower than the bottom anyway

								continue;
							}

							_isOnEdge = true;
							_edgeNormal = secondHit.edgeNormal;
							_edgeAngle = secondHit.edgeAngle;
						}

						hit = true;
						_isGrounded = true;
						_groundNormal = secondHit.normal;
						_groundPoint = secondHit.point;

						isJumping = false;

						if (Input.GetKeyDown (KeyCode.K)) 
						{
							//DrawGroundDebug (secondHit.point, secondHit.normal, 1, Color.cyan, Color.green);
						}

						SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));
						depenetration = DepenetrateFromGround (origin, radius, secondHit.point, groundPivot.up);

						continue;
					}
				} 

			} 

			break;
		} 

		if (_isGrounded || _isOnHardEdge) 
		{
			canJump = true;
		}

		if (hit) 
		{
			groundInfo.Set(_isGrounded, _wasGroundedBefore, _isOnEdge, _isOnStep, _isOnHardEdge, _isGoingTowardsEdge, _groundNormal, _groundPoint, _edgeNormal, _edgeAngle);
			return depenetration;
		}

		groundInfo.Set (false, _wasGroundedBefore, _isOnEdge, false, _isOnHardEdge, _isGoingTowardsEdge, Vector3.zero, _groundPoint, _edgeNormal, _edgeAngle);
		SetGroundPivot (Quaternion.FromToRotation (Vector3.up, -gravityDir));
		return Vector3.zero;
	}*/




	/*Vector3 DepenetrateFromGround (Vector3 newCentre, Vector3 originalCentre, float radius, Vector3 hitPoint, Vector3 transformUp, float offsetUsed, bool onEdge, int num = 0, bool allowSuckingDown = true) 
	{
		//return Vector3.zero;
		//Debug.Log ("trying to depen");

		Vector3 initialTranslation = newCentre - originalCentre;

		Vector3 depenetration = Vector3.zero;
		float distanceToHit = 0;
		float height = 0;

		if (onEdge) 
		{
			Vector3 groundPivotOriginalPos = groundPivot.position;
			groundPivot.position = newCentre;
			Vector3 localCentre = groundPivot.InverseTransformPoint (newCentre);
			Vector3 localHitPoint = groundPivot.InverseTransformPoint (hitPoint);



			distanceToHit = localCentre.y - localHitPoint.y;

			float lateralDist = Mathf.Abs ((new Vector3 (localCentre.x, 0, localCentre.z) - new Vector3 (localHitPoint.x, 0, localHitPoint.z)).magnitude);

			groundPivot.position = groundPivotOriginalPos;

			float discriminant = radius*radius - lateralDist*lateralDist;
			if (discriminant >= 0) 
			{
				height = Mathf.Sqrt (discriminant);
			} 
			else 
			{
				Debug.Log ("Negative discriminant, ground depenetration failed");
				return depenetration;
			}
		} 

		else 
		{
			distanceToHit = (newCentre - hitPoint).magnitude;
			height = radius;
		}


		float verticalDepenetration = (height - distanceToHit); //doing it this way and using the initial translation at the end is the correct way, but (see above)


		if (!allowSuckingDown) 
		{
			if (verticalDepenetration < 0) 
			{
				verticalDepenetration = 0;
			}
		}


		if (verticalDepenetration < -0.000001f && num == 0 && onEdge)  //num is so we cant get stuck in an endless loop
		{
			//we need to make sure the depentration doesnt move us into another lower ground plane and make us get stuck in a collision loop (eg certain step cases)

			RaycastHit hit = new RaycastHit ();
			if (Physics.SphereCast (newCentre, Mathf.Abs (verticalDepenetration), -transformUp, out hit, radius)) 
			{
				Debug.Log ("unsafe depenetration!");

				depenetration = DepenetrateFromGround (newCentre, newCentre, radius, hit.point, transformUp, groundCheckOffset, true, 1);
				return depenetration;
			}
		}


		depenetration = verticalDepenetration * transformUp;
		depenetration += initialTranslation;

		return depenetration;
	}*/


	/*Vector3 Grounding(Vector3 previousOrigin, Vector3 origin, LayerMask layerMask, Vector3 stepVelocity, float deltaTime)
	{
		bool hit = false;
		float radius = sphereRadius+groundOffset;
		Vector3 depenetration = Vector3.zero;

		Vector3 lateralVelocity = groundPivot.InverseTransformDirection (velocity);
		lateralVelocity.y = 0;

		Vector3 lateralStepVelocity = groundPivot.InverseTransformDirection (stepVelocity/deltaTime);
		lateralStepVelocity.y = 0;

		canJump = false;
		bool _isGrounded = false;
		bool _wasGroundedBefore = groundInfo.isGrounded;
		bool _isOnEdge = false;
		bool _isOnStep = false;
		bool _isOnHardEdge = false;
		bool _isGoingTowardsEdge = false;
		Vector3 _groundNormal = Vector3.zero;
		Vector3 _groundPoint = Vector3.zero;
		Vector3 _edgeNormal = Vector3.zero;

		bool beginsGrounded = groundInfo.isGrounded;
		groundCheckOffset = minGroundCheckOffset;
		//groundCheckOffset = 0;
		float numSteps = 3;
		float steppedGroundCheckOffset = (numSteps > 1)? ((maxGroundCheckOffset - minGroundCheckOffset) / (numSteps-1)) : 0;

		GroundCastInfo hitInfo = new GroundCastInfo ();

		for (int i = 0; i < numSteps; i++) 
		{
			//the first hit is for handling flat (non edge detected) ground and concave slopes only
			Vector3 newOrigin = origin - (groundPivot.up * groundCheckOffset);
			hitInfo =  GroundCast(newOrigin, origin, sphereRadius+0.01f, layerMask, velocity, 1);

			if (hitInfo.hasHit) 
			{
				if (CanWalkOnSlope (hitInfo.GetCalculatedGroundNormal(), groundPivot.up)) 
				{
					if (hitInfo.onEdge && hitInfo.edgeInfo.GetOnHardEdge ()) 
					{
						//hard edges
						Debug.Log ("hard edge!");

						hit = true;
						_isGrounded = true;
						_groundNormal = -gravityDir;
						//_groundNormal = hitInfo.GetCalculatedGroundNormal();
						_groundPoint = hitInfo.point;

						isJumping = false;

						SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));
						depenetration = DepenetrateFromGround (newOrigin, origin, radius, hitInfo.point, groundPivot.up, groundCheckOffset, true);

						//if (ExtVector3.MagnitudeInDirection (depenetration, groundPivot.up, true) < 0) 
						//{
						//	depenetration = Vector3.zero;
						//	hit = false;
						//}

						break;
					}


					hit = true;
					_isGrounded = true;
					_groundNormal = hitInfo.GetCalculatedGroundNormal();
					_groundPoint = hitInfo.point;

					isJumping = false;


					if (Input.GetKeyDown (KeyCode.O)) {
						DrawGroundDebug (hitInfo.point, hitInfo.GetCalculatedGroundNormal(), 1, Color.cyan, Color.green);
					}

					SetGroundPivot (Quaternion.FromToRotation (Vector3.up, _groundNormal));
					depenetration = DepenetrateFromGround (newOrigin, origin, radius, hitInfo.point, groundPivot.up, groundCheckOffset, hitInfo.onEdge);

					break;
				} 
				else 
				{
					Debug.Log ("non-walkable!");
				}
			}


			//check to proceed to and prepare for next iteration (sucking only)
			if (beginsGrounded) 
			{
				groundCheckOffset += steppedGroundCheckOffset;
				//continue;
				break;
			}

			break;
		} 



		if (hit) 
		{
			if (_isGrounded || _isOnHardEdge) 
			{
				canJump = true;
			}

			groundInfo.Set(_isGrounded, _wasGroundedBefore, hitInfo, _isOnStep, _isGoingTowardsEdge);
			return depenetration;
		}

		groundInfo.Set (false, _wasGroundedBefore, hitInfo, false, _isGoingTowardsEdge);
		SetGroundPivot (Quaternion.FromToRotation (Vector3.up, -gravityDir));
		return Vector3.zero;
	}*/

	/*bool ShouldWrapEdge (GroundCastInfo hit, Vector3 stepVelocity) {

		bool wrapEdge = true;
		//if cant walk naturally around convex slope 
		//if (!(hit.edgeAngle <= convexSlopeLimit && hit.edgeAngle != 0)) 
		{

			Vector3 lateralEdgeNormal = groundPivot.InverseTransformDirection (hit.edgeNormal);
			lateralEdgeNormal.y = 0;
			lateralEdgeNormal = groundPivot.TransformDirection (lateralEdgeNormal);

			//if handling for special edge is true
			if (CanWalkOnSlope (hit.realNormal, -gravityDir) && CanWalkOnSlope (hit.edgeNormal, -gravityDir)) 
			{
				//if speed too great to wrap special edge
				if (Mathf.Abs (ExtVector3.MagnitudeInDirection (stepVelocity, lateralEdgeNormal, false)) < wallSpeedThreshold) 
				{
					wrapEdge = true;
				} 
				else 
				{
					wrapEdge = false;
				}

				//Debug.Log ("speed mag in dir:" + ExtVector3.MagnitudeInDirection (stepVelocity, lateralEdgeNormal, false));
			} 
			else if (CanWalkOnSlope (hit.realNormal, -gravityDir) && !CanWalkOnSlope (hit.edgeNormal, -gravityDir)) 
			{
				float angleBetweenEdgeAndGravUp = ExtVector3.Angle (hit.edgeNormal, -gravityDir);
				if (ExtVector3.Angle (hit.edgeNormal, hit.interpolatedNormal) > angleBetweenEdgeAndGravUp && Mathf.Abs (ExtVector3.MagnitudeInDirection (stepVelocity, lateralEdgeNormal, false)) < wallSpeedThreshold) 
				{
					wrapEdge = true;
				} 
				else 
				{
					wrapEdge = false;
				}
			} 
			else 
			{
				wrapEdge = false;
			}

		} 

		return wrapEdge;
	}*/

	/*bool CanStep (GroundCastInfo hit, Vector3 origin) {

		Vector3 hitToOriginDir = origin - hit.point;
		hitToOriginDir = groundPivot.InverseTransformDirection (hitToOriginDir);
		hitToOriginDir.y = 0;
		hitToOriginDir = groundPivot.TransformDirection (hitToOriginDir);
		Vector3 castOrigin = hit.point + 0.1f * hitToOriginDir;

		RaycastHit rayHit = new RaycastHit ();

		if (Physics.Raycast (castOrigin, -groundPivot.up, out rayHit, sphereRadius*maxStepHeight)) {

			//if (rayHit.normal != hit.edgeNormal && CanWalkOnSlope (rayHit.normal, groundPivot.up) && ExtVector3.Angle (hit.realNormal, rayHit.normal) < 15f) 
			//{
			return true;
			//}
		}

		return false;
	}*/

	/*bool GetGoingTowardsEdge (Vector3 origin, Vector3 hitPoint, Vector3 lateralVelocity) {

		Vector3 originToHit = hitPoint - origin;
		originToHit = groundPivot.InverseTransformDirection (originToHit);
		originToHit.y = 0;

		if (ExtVector3.Angle (originToHit, lateralVelocity) >= 90 || lateralVelocity.magnitude == 0) 
		{
			//going away from edge
			//Debug.Log ("going away from edge");
			return false;
		} 
		else 
		{
			//going towards edge
			//Debug.Log ("going towards edge");
			return true;
		}
	}*/


/*List<GroundingSphereCollisionInfo> groundContactsBuffer = new List<GroundingSphereCollisionInfo>();
GroundCastInfo GroundCast(Vector3 newOrigin, Vector3 originalOrigin, float radius, LayerMask layerMask, Vector3 velocity, int num)
{
	Vector3 transformUp = groundPivot.up;
	GroundCastInfo walkable = new GroundCastInfo();
	GroundCastInfo nonWalkable = new GroundCastInfo();
	GroundCastInfo averagedWalkable = new GroundCastInfo();
	Vector3 bottomHeightReference = originalOrigin;
	Vector3 bottomSphereOffset = newOrigin;

	if (currentCollider is SphereCollider) 
	{
		//bottomSphereOffset = origin - (transformUp * groundCheckOffset);
		//bottomHeightReference = origin;
		GroundingSphereCollisionDetect.DetectSphereCollisions(bottomSphereOffset, bottomSphereOffset, radius, layerMask, ignoreColliders, groundContactsBuffer, transform.up, 0);
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
		GroundingSphereCollisionDetect.DetectCapsuleCollisions(topSphere, bottomSphereOffset, bottomSphereOffset, radius, layerMask, ignoreColliders, groundContactsBuffer, transformUp, 0);
	}

	if (Input.GetKey(KeyCode.K)) {

		DrawContactsDebug (groundContactsBuffer, 2, Color.red, Color.green);
	}



	List<GroundCastInfo> walkableGroundPoints = new List<GroundCastInfo> ();
	List<GroundCastInfo> nonWalkableGroundPoints = new List<GroundCastInfo> ();

	//We search for the best ground.
	for (int i = 0; i < groundContactsBuffer.Count; i++) 
	{
		GroundingSphereCollisionInfo collisionPoint = groundContactsBuffer [i];


		//We make sure the hit is below our bottomSphere (note: using the original origion allows for perpendicular walls to be included as ground info, which we need for situations where a none-walkable slope forms a v shape with such a wall)
		if (!ExtVector3.IsInDirection (collisionPoint.closestPointOnSurface - bottomSphereOffset, -transformUp, tinyOffset, false))
			continue;

		GroundingEdgeCollisionInfo normalsInfo = new GroundingEdgeCollisionInfo ();
		Vector3 normal = collisionPoint.realNormal;

		if (collisionPoint.isOnEdge) {
			//Debug.Log ("on edge");
			normalsInfo = GroundingSphereCollisionDetect.DetectEdgeCollisions (collisionPoint.collider, collisionPoint.closestPointOnSurface, 0.0001f, collisionPoint.realNormal, collisionPoint.interpolatedNormal, layerMask, ignoreColliders, transformUp, 0);


			normal = normalsInfo.calculatedGroundNormal;
		}


		GroundCastInfo processedGround = new GroundCastInfo(newOrigin, collisionPoint.closestPointOnSurface, normal, normalsInfo, collisionPoint.collider, collisionPoint.isOnEdge);

		if (CanWalkOnSlope (processedGround.GetCalculatedGroundNormal (), transformUp)) 
		{
			walkableGroundPoints.Add (processedGround);
		} 

		else 
		{
			nonWalkableGroundPoints.Add (processedGround);
		}

	}



	//find the average of walkables (for concave slope handling)
	List<GroundCastInfo> postConcaveProcessingWalkableGroundPoints = new List <GroundCastInfo>();
	List<Vector3> hitPoints = new List<Vector3> ();
	Vector3 totalNormals = Vector3.zero;
	float smallestDiff = float.MinValue;
	Vector3 closestPoint = Vector3.zero;
	Vector3 closesPointNormal = Vector3.zero;

	for (int i = 0; i < walkableGroundPoints.Count; i++) 
	{
		if (walkableGroundPoints [i].edgeInfo.GetOnHardEdge()) 
		{
			//postConcaveProcessingWalkableGroundPoints.Add (walkableGroundPoints [i]);
			//continue;
		}


		Vector3 currentNormal = walkableGroundPoints [i].GetCalculatedGroundNormal ();

		totalNormals += currentNormal;
		hitPoints.Add (walkableGroundPoints [i].point);

		////float distTohit = (newOrigin - walkableGroundPoints[i].point).magnitude;
		//	float depenetrationDistance = Geometry.DepenetrateSphereFromPlaneInDirection(bottomSphereOffset, radius, transformUp, walkableGroundPoints[i].point, currentNormal).distance;
		//	float angleToGravityDirDiff = Vector3.Angle (currentNormal, -gravityDir);
		//
		//	if (depenetrationDistance > smallestDiff) 
		//	{
		//		smallestDiff = depenetrationDistance;
		//		closestPoint = walkableGroundPoints[i].point;
		//		closesPointNormal = currentNormal;
		//	}
	}

	//only create a new one if we actually found non-edge walkables
	if (totalNormals != Vector3.zero) 
	{
		//Vector3 newNormal = closesPointNormal;
		Vector3 newNormal = totalNormals.normalized;
		Vector3 newPoint = closestPoint;

		smallestDiff = float.MinValue;

		for (int i = 0; i < hitPoints.Count; i++) 
		{
			float depenetrationDistance = Geometry.DepenetrateSphereFromPlaneInDirection(bottomSphereOffset, radius, transformUp, hitPoints[i], newNormal).distance;

			if (depenetrationDistance > smallestDiff) 
			{
				smallestDiff = depenetrationDistance;
				newPoint = hitPoints [i];
			}
		}

		GroundCastInfo newWalkableGround = new GroundCastInfo(newOrigin, newPoint, newNormal, new GroundingEdgeCollisionInfo (), null, false);
		postConcaveProcessingWalkableGroundPoints.Add (newWalkableGround);
	}




	List<GroundCastInfo> processedGroundPoints = postConcaveProcessingWalkableGroundPoints;
	//List<GroundCastInfo> processedGroundPoints = walkableGroundPoints;
	processedGroundPoints.Concat (nonWalkableGroundPoints);

	Vector3 walkableHighestPoint = float.MinValue * groundPivot.up;
	Vector3 nonWalkableHighestPoint = float.MinValue * groundPivot.up;


	for (int i = 0; i < processedGroundPoints.Count; i++) {

		GroundCastInfo collisionInfo = processedGroundPoints [i];
		Vector3 hitPoint = collisionInfo.point;
		Vector3 normal = collisionInfo.normal;



		float depenetrationDistance = Geometry.DepenetrateSphereFromPlaneInDirection(bottomSphereOffset, radius, transformUp, hitPoint, normal).distance;

		if(CanWalkOnSlope(normal, transformUp))
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
			//We try to see if we are on a platform like a V shape. If we are, then we want to count that as grounded.
			//Vector3 averageNormal = (normal + nonWalkable.normal).normalized;
			//if(CanWalkOnSlope(averageNormal, groundPivot.up) && Vector3.Dot(averageNormal, transformUp) > Vector3.Dot(averagedWalkable.normal, transformUp) + tinyOffset)
			//	{
			//		SweepInfo sweep = Geometry.SpherePositionBetween2Planes(radius, nonWalkable.point, nonWalkable.normal, hitPoint, normal, false);
			//		if(!sweep.hasHit || sweep.distance < averagedWalkable.depenetrationDistance) continue;
			//
			//		//Our grounding does not handle depenetrating us from averageNormals, we are mainly just passing the averageNormal so we can be considered grounded.
			//		//Our GetCollisionSafeVelocity will handle depenetrating us. This means we dont have much controll over how we want to handle average normals.
			//		//So for average normals we will just slide off edges.
			//		averagedWalkable.Set(sweep.intersectPoint, averageNormal, realNormal, interpolatedNormal, collisionPoint.collider, false, sweep.distance);
			//
			//		#region Debug
			//		#if UNITY_EDITOR
			//		//DrawGroundDebug(averagedWalkable.point, averagedWalkable.normal, 1, Color.yellow, Color.green);
			//		//Debug.Log ("set averaged walkable");
			//		#endif
			//		#endregion
			//	}

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
	if(averagedWalkable.hasHit)
	{
		return averagedWalkable;
	}
	return nonWalkable;
}*/


/*Vector3 ConstrainInterpolatedNormal(Vector3 interpolatedNormal, Vector3 realNormal, Vector3 edgeNormal, float edgeAngle) 
{
	float angleBetween = ExtVector3.Angle (realNormal, interpolatedNormal);
	if (angleBetween > edgeAngle) {
		//interpolatedNormal = (edgeNormal+realNormal*0.2f).normalized;
	}

	angleBetween = ExtVector3.Angle (edgeNormal, interpolatedNormal);
	if (angleBetween > edgeAngle) {
		//this can cause a problem
		//interpolatedNormal = (realNormal+edgeNormal*0.2f).normalized;

	}

	return interpolatedNormal;
}*/





}
