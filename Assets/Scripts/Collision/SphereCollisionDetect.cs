using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SphereCollisionDetect {

	public static List<SphereCollisionInfo> DetectCollisions(Collider collider, Vector3 detectionOrigin, Vector3 realOrigin, Vector3 directionUp, float height, float radius, float castRadius, int mask, IList<Component> ignoreColliders, List<SphereCollisionInfo> resultBuffer, Vector3 transformUp, float checkOffset = 0, bool multipleContactsPerCollider = true)
	{
		if (collider is SphereCollider) 
		{
			return DetectSphereCollisions (detectionOrigin, realOrigin, radius, castRadius, mask, ignoreColliders, resultBuffer, transformUp, checkOffset, multipleContactsPerCollider);
		} 
		else if (collider is CapsuleCollider) 
		{
			CapsuleShape points = new CapsuleShape (detectionOrigin, directionUp, height, radius, checkOffset);
			return DetectCapsuleCollisions (points.top, points.bottom, realOrigin, radius, mask, ignoreColliders, resultBuffer, transformUp, checkOffset, multipleContactsPerCollider);
		}

		return new List<SphereCollisionInfo> ();
	}


	static List<Collider> colliderBufferSphere = new List<Collider>();
	static List<ContactInfo> contactsBufferSphere = new List<ContactInfo>();
	public static List<SphereCollisionInfo> DetectSphereCollisions(Vector3 detectionOrigin, Vector3 realOrigin, float radius, float castRadius, int mask, IList<Component> ignoreColliders, List<SphereCollisionInfo> resultBuffer, Vector3 transformUp, float checkOffset = 0, bool multipleContactsPerCollider = true)
	{
		resultBuffer.Clear();
		colliderBufferSphere.Clear();

		ExtPhysics.OverlapSphere(detectionOrigin, castRadius, ignoreColliders, colliderBufferSphere, mask);
		//Debug.Log (colliderBufferSphere.Count);
		if(colliderBufferSphere.Count == 0) return resultBuffer;

		for(int i = 0; i < colliderBufferSphere.Count; i++)
		{
			contactsBufferSphere = ExtCollider.ClosestPointsOnSurface(colliderBufferSphere[i], detectionOrigin, radius + checkOffset, contactsBufferSphere, multipleContactsPerCollider);

			for(int j = 0; j < contactsBufferSphere.Count; j++)
			{
				//We calculate sphereDetectionOriginInCapsule for our depenetration method since we need to know where the spheres detection origin would be within the capsule.
				Vector3 sphereDetectionOriginInSphere = detectionOrigin;
				/*if((colliderBufferCapsule[i] is CapsuleCollider || colliderBufferCapsule[i] is SphereCollider))
				{
					//sphereDetectionOriginInSphere = Geometry.ClosestPointsOnSegmentToLine(centre, centre, contactsBufferCapsule[j].point, contactsBufferCapsule[j].normal).first;
				}
				else
				{
					//sphereDetectionOriginInSphere = Geometry.ClosestPointOnLineSegmentToPoint(contactsBufferCapsule[j].point, centre, centre);
				}*/

				//We store just the radius, not radius + checkOffset, so that our depenetration method has the correct radius to depenetrate with.
				resultBuffer.Add(new SphereCollisionInfo(true, colliderBufferSphere[i], sphereDetectionOriginInSphere, realOrigin, radius, contactsBufferSphere[j].point, contactsBufferSphere[j].normal, transformUp));
			}
		}

		return resultBuffer;
	}



	static List<Collider> colliderBufferCapsule = new List<Collider>();
	static List<ContactInfo> contactsBufferCapsule = new List<ContactInfo>();
	public static List<SphereCollisionInfo> DetectCapsuleCollisions(Vector3 segment0, Vector3 segment1, Vector3 realOrigin, float radius, int mask, IList<Component> ignoreColliders, List<SphereCollisionInfo> resultBuffer, Vector3 transformUp, float checkOffset = 0, bool multipleContactsPerCollider = true)
	{
		resultBuffer.Clear();
		colliderBufferCapsule.Clear();

		ExtPhysics.OverlapCapsule(segment0, segment1, radius + checkOffset, ignoreColliders, colliderBufferCapsule, mask);
		//Debug.Log (colliderBufferCapsule.Count);
		if(colliderBufferCapsule.Count == 0) return resultBuffer;

		for(int i = 0; i < colliderBufferCapsule.Count; i++)
		{
			contactsBufferCapsule = ExtCollider.ClosestPointsOnSurface (colliderBufferCapsule[i], segment0, segment1, radius + checkOffset, contactsBufferCapsule, multipleContactsPerCollider);

			for(int j = 0; j < contactsBufferCapsule.Count; j++)
			{
				//We calculate sphereDetectionOriginInCapsule for our depenetration method since we need to know where the spheres detection origin would be within the capsule.
				Vector3 sphereDetectionOriginInCapsule = Vector3.zero;
				if((colliderBufferCapsule[i] is CapsuleCollider || colliderBufferCapsule[i] is SphereCollider) && !ExtVector3.IsParallel(segment1 - segment0, contactsBufferCapsule[j].normal))
				{
					sphereDetectionOriginInCapsule = Geometry.ClosestPointsOnSegmentToLine(segment0, segment1, contactsBufferCapsule[j].point, contactsBufferCapsule[j].normal).first;
				}
				else
				{
					sphereDetectionOriginInCapsule = Geometry.ClosestPointOnLineSegmentToPoint(contactsBufferCapsule[j].point, segment0, segment1);
				}

				//We store just the radius, not radius + checkOffset, so that our depenetration method has the correct radius to depenetrate with.
				resultBuffer.Add(new SphereCollisionInfo(true, colliderBufferCapsule[i], sphereDetectionOriginInCapsule, realOrigin, radius, contactsBufferCapsule[j].point, contactsBufferCapsule[j].normal, transformUp));
			}
		}

		return resultBuffer;
	}


	public static PlayerPhysicsController.DepenetrationInfo Depenetrate (PlayerPhysicsController ppc, List<SphereCollisionInfo> collisionPoints, Vector3 velocity, int maxIterations = 4)
	{
		PlayerPhysicsController.DepenetrationInfo depenInfo = new PlayerPhysicsController.DepenetrationInfo ();
		depenInfo.Initialize ();

		if (collisionPoints.Count > 0 && maxIterations > 0) 
		{
			Vector3 depenetrationVelocity = Vector3.zero;
			float steepStopLimit = maxIterations / 2;   //the purpose of this is to try and stop it get stucking in a collision breaking loop if there is a case where it must move up in order to become safe

			bool foundWalkableGroundNormal = false;
			Vector3 totalNormal = Vector3.zero;
			List<CollisionPointInfo> collisionPointsInfo = new List <CollisionPointInfo> ();
			for (int i = 0; i < collisionPoints.Count; i++) 
			{
				CollisionPointInfo cpi = new CollisionPointInfo ();
				cpi.cp = collisionPoints [i];

				totalNormal += cpi.cp.interpolatedNormal;

				if (cpi.cp.isOnEdge) 
				{
					cpi.normalsInfo = ppc.GetEdgeInfo (cpi.cp.collider, cpi.cp.closestPointOnSurface, cpi.cp.realNormal, cpi.cp.interpolatedNormal, velocity);
				}

				

				collisionPointsInfo.Add (cpi);
			}
			depenInfo.pointsInfo = collisionPointsInfo;
			depenInfo.averageNormal = totalNormal.normalized;

			int counter = 0;

			//Since with each iteration we are using old collision data, higher maxIterations does not mean more accuracy. You will need to tune it to your liking.
			for (int i = 0; i < maxIterations; i++) 
			{
				counter++;

				bool depenetrated = false;
				for (int j = 0; j < depenInfo.pointsInfo.Count; j++) 
				{
					CollisionPointInfo cpi = depenInfo.pointsInfo [j];
					SphereCollisionInfo cp = cpi.cp;
					Vector3 detectOriginOffset = depenInfo.totalDepenetration + depenetrationVelocity + cp.detectionOrigin;

					cpi.SetInfo (i, steepStopLimit, ppc, detectOriginOffset); //sets stuff in the struct
					Vector3 depenetrationNormal = cpi.GetDepenetrationNormal();
					depenInfo.pointsInfo [j] = cpi;
				
					Vector3 depenetration = (Geometry.DepenetrateSphereFromPlaneInDirection (detectOriginOffset, cp.sphereRadius, depenetrationNormal, cp.closestPointOnSurface, cp.interpolatedNormal).distance) * depenetrationNormal;
					//if (ExtVector3.MagnitudeInDirection (depenetration, depenetrationNormal, false) < -0.0001f) continue;

					cpi.wasDetected = true;
					depenInfo.pointsInfo[j] = cpi;

					if (ExtVector3.MagnitudeInDirection(depenetration, depenetrationNormal, false) <= 0) continue;

					cpi.wasDepenetrated = true;
					depenInfo.pointsInfo[j] = cpi;

					//to work with our extra grounding, walkableGroundNormal is changed to any kind of up-facing "ground" normal
					if (!foundWalkableGroundNormal && ExtVector3.MagnitudeInDirection (cpi.cp.interpolatedNormal, -ppc.gravityDir) > 0)
					{
						foundWalkableGroundNormal = true;
					}


					depenetrationVelocity += depenetration + 0.00001f * depenetrationNormal;
					//depenetrationVelocity += depenetration;
					depenetrated = true;
				}
					

				if(!depenetrated) break;

				depenInfo.totalDepenetration += depenetrationVelocity;
				depenetrationVelocity = Vector3.zero;
			}

			depenInfo.foundWalkableGroundNormal = foundWalkableGroundNormal;

			//Debug.Log ("depenetration loops: " + counter);
		}

		return depenInfo;
	}



	//I think this works fine with our capsule detection, but doesnt really work with spheres shaping a capsule. The reason for this is
	//when having spheres shape a capsule, its possible for a sphere to detect a hit and set the interpolated normal in a way that blocks all other hits behind it, however,
	//when this sphere depenetrates, since it isnt taking into account that we wanted to treat it like a capsule, it wont depenetrate enough for the hits behind it to be resolved.
	//However, since our capsule DetectCollisions handles placing the spheres properly to form a capsule, it should work with that.
	//This method is pretty similar to the "CleanUp" method in our meshbsptree.
	static List<MPlane> ignoreBehindPlanes = new List<MPlane>();
	public static List<SphereCollisionInfo> CleanByIgnoreBehindPlane(List<SphereCollisionInfo> collisionPoints)
	{
		if(collisionPoints.Count > 1)
		{
			ignoreBehindPlanes.Clear();

			//Taking advantage of C# built in QuickSort algorithm
			collisionPoints.Sort(SphereCollisionInfo.SphereCollisionComparerDescend.defaultComparer);

			for(int i = collisionPoints.Count - 1; i >= 0; i--)
			{
				if(!MPlane.IsBehindPlanes(collisionPoints[i].closestPointOnSurface, ignoreBehindPlanes, -.0001f))
				{
					ignoreBehindPlanes.Add(new MPlane(collisionPoints[i].interpolatedNormal, collisionPoints[i].closestPointOnSurface, false));
				}
				else
				{
					collisionPoints.RemoveAt(i);
				}
			}
		}

		return collisionPoints;
	}




	public struct CollisionPointInfo
	{
		public SphereCollisionInfo cp;
		public Vector3 depenetrationNormal;
		public bool slopeTooSteep;
		public bool invalidStep;
		public GroundingEdgeCollisionInfo normalsInfo;
		public Vector3 depenetrationInNormalDir;
		public bool wasDepenetrated;
		public bool wasDetected;

		public void SetInfo (int i, float steepSlopeLimit, PlayerPhysicsController ppc, Vector3 originOffset)
		{
			this.depenetrationNormal = cp.interpolatedNormal;

			if (i < steepSlopeLimit) 
			{
				float angleToUp = Vector3.Angle (ppc.groundPivot.up, cp.interpolatedNormal); //to make sure its pointing up and not a ceiling kind of edge

				if (cp.isOnEdge) 
				{
					if (angleToUp <= 90 && normalsInfo.GetOnHardEdge()) 
					{
						bool validGroundedStep = ppc.CheckIfEdgeIsSteppable (cp.closestPointOnSurface, originOffset, ppc.groundPivot.up);

						if (!validGroundedStep) 
						{
							//do extra checking for when in the air and no ground is found below (we still want to slide up over edges of a low enough height)
							float height = ExtVector3.MagnitudeInDirection (cp.closestPointOnSurface - (cp.detectionOrigin-(cp.sphereRadius*ppc.groundPivot.up)), ppc.groundPivot.up, false);
							validGroundedStep = height <= ppc.maxStepHeight*0.9f;
						}

						if (!validGroundedStep) 
						{
							this.invalidStep = true;
							this.depenetrationNormal = Vector3.ProjectOnPlane (cp.interpolatedNormal, ppc.groundPivot.up).normalized;
						}
					}
				} 
				else 
				{
					if ((angleToUp < 90) && !ppc.CanWalkToSlope (cp.normal, ppc.groundPivot.up, ppc.groundInfo.GetIsGrounded()) && ppc.groundInfo.GetIsGrounded()) 
					{
						this.slopeTooSteep = true;
						this.depenetrationNormal = Vector3.ProjectOnPlane (cp.interpolatedNormal, ppc.groundPivot.up).normalized;

						//Debug.Log("cant");
					} 
				}
			} 
		}


		public Vector3 GetDepenetrationNormal ()
		{
			return depenetrationNormal;
		}


	}
}
