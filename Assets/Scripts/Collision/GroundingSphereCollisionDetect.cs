using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GroundingSphereCollisionDetect {


	public static List<GroundingSphereCollisionInfo> DetectCollisions(Collider collider, Vector3 detectionOrigin, Vector3 realOrigin, Vector3 directionUp, float height, float radius, int mask, IList<Component> ignoreColliders, List<GroundingSphereCollisionInfo> resultBuffer, Vector3 transformUp, float checkOffset = 0, bool multipleContactsPerCollider = true)
	{
		if (collider is SphereCollider) 
		{
			return DetectSphereCollisions (detectionOrigin, realOrigin, radius, mask, ignoreColliders, resultBuffer, transformUp, checkOffset, true);
		} 
		else if (collider is CapsuleCollider) 
		{
			CapsuleShape points = new CapsuleShape (detectionOrigin, directionUp, height, radius, checkOffset);
			return DetectCapsuleCollisions (points.top, points.bottom, realOrigin, radius, mask, ignoreColliders, resultBuffer, transformUp, checkOffset, true);
		}

		return new List<GroundingSphereCollisionInfo> ();
	}


	static List<Collider> colliderBufferSphere = new List<Collider>();
	static List<ContactInfo> contactsBufferSphere = new List<ContactInfo>();
	public static List<GroundingSphereCollisionInfo> DetectSphereCollisions(Vector3 detectionOrigin, Vector3 realOrigin, float radius, int mask, IList<Component> ignoreColliders, List<GroundingSphereCollisionInfo> resultBuffer, Vector3 transformUp, float checkOffset = 0, bool multipleContactsPerCollider = true, bool forGroundCast = false)
	{
		resultBuffer.Clear();
		colliderBufferSphere.Clear();

		ExtPhysics.OverlapSphere(detectionOrigin, radius, ignoreColliders, colliderBufferSphere, mask);
		//Debug.Log (colliderBufferSphere.Count);
		if(colliderBufferSphere.Count == 0) return resultBuffer;

		for(int i = 0; i < colliderBufferSphere.Count; i++)
		{
			contactsBufferSphere = ExtCollider.ClosestPointsOnSurface(colliderBufferSphere[i], detectionOrigin, radius, contactsBufferSphere, multipleContactsPerCollider, true);
			//Debug.Log (contactsBufferSphere.Count);

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
				resultBuffer.Add(new GroundingSphereCollisionInfo(true, colliderBufferSphere[i], sphereDetectionOriginInSphere, realOrigin, radius, contactsBufferSphere[j].point, contactsBufferSphere[j].normal, transformUp));
			}
		}


		return resultBuffer;
	}



	static List<Collider> colliderBufferCapsule = new List<Collider>();
	static List<ContactInfo> contactsBufferCapsule = new List<ContactInfo>();
	public static List<GroundingSphereCollisionInfo> DetectCapsuleCollisions(Vector3 segment0, Vector3 segment1, Vector3 realOrigin, float radius, int mask, IList<Component> ignoreColliders, List<GroundingSphereCollisionInfo> resultBuffer, Vector3 transformUp, float checkOffset = 0, bool multipleContactsPerCollider = true, bool forGroundCast = false)
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
				resultBuffer.Add(new GroundingSphereCollisionInfo(true, colliderBufferCapsule[i], sphereDetectionOriginInCapsule, realOrigin, radius, contactsBufferCapsule[j].point, contactsBufferCapsule[j].normal, transformUp));
			}
		}

		return resultBuffer;
	}


	public static Vector3 Depenetrate(List<GroundingSphereCollisionInfo> collisionPoints, int maxIterations = 1)
	{
		if(collisionPoints.Count > 0 && maxIterations > 0)
		{
			Vector3 depenetrationVelocity = Vector3.zero;
			Vector3 totalDepenetrationVelocity = Vector3.zero;

			//Since with each iteration we are using old collision data, higher maxIterations does not mean more accuracy. You will need to tune it to your liking.
			for(int i = 0; i < maxIterations; i++)
			{
				for(int j = 0; j < collisionPoints.Count; j++)
				{
					GroundingSphereCollisionInfo cp = collisionPoints[j];

					Vector3 detectOriginOffset = totalDepenetrationVelocity + depenetrationVelocity + cp.detectionOrigin;

					//We check if we are already depenetrated.
					if(ExtVector3.MagnitudeInDirection(detectOriginOffset - cp.closestPointOnSurface, cp.interpolatedNormal, false) > cp.sphereRadius) continue;

					//We take into account how much we already depenetrated.
					Vector3 collisionVelocityOffset = Vector3.Project(detectOriginOffset - cp.closestPointOnSurface, cp.GetCollisionVelocity());

					float collisionMagnitude = GroundingSphereCollisionInfo.GetCollisionMagnitudeInDirection(collisionVelocityOffset, cp.interpolatedNormal, cp.sphereRadius) + .0001f;

					depenetrationVelocity += GroundingSphereCollisionInfo.GetDepenetrationVelocity(cp.interpolatedNormal, collisionMagnitude);
				}

				if(depenetrationVelocity == Vector3.zero) break;

				totalDepenetrationVelocity += depenetrationVelocity;
				depenetrationVelocity = Vector3.zero;
			}

			return totalDepenetrationVelocity;
		}

		return Vector3.zero;
	}


	//I think this works fine with our capsule detection, but doesnt really work with spheres shaping a capsule. The reason for this is
	//when having spheres shape a capsule, its possible for a sphere to detect a hit and set the interpolated normal in a way that blocks all other hits behind it, however,
	//when this sphere depenetrates, since it isnt taking into account that we wanted to treat it like a capsule, it wont depenetrate enough for the hits behind it to be resolved.
	//However, since our capsule DetectCollisions handles placing the spheres properly to form a capsule, it should work with that.
	//This method is pretty similar to the "CleanUp" method in our meshbsptree.
	static List<MPlane> ignoreBehindPlanes = new List<MPlane>();
	public static List<GroundingSphereCollisionInfo> CleanByIgnoreBehindPlane(List<GroundingSphereCollisionInfo> collisionPoints)
	{
		if(collisionPoints.Count > 1)
		{
			ignoreBehindPlanes.Clear();

			//Taking advantage of C# built in QuickSort algorithm
			collisionPoints.Sort(GroundingSphereCollisionInfo.SphereCollisionComparerDescend.defaultComparer);

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


	static List<ContactInfo> normalsBufferSphere = new List<ContactInfo>();
	public static GroundingEdgeCollisionInfo DetectEdgeCollisions(Collider collider, Vector3 detectionOrigin, float radius, Vector3 detectedGroundNormal, Vector3 interpolatedNormal, int mask, IList<Component> ignoreColliders, Vector3 transformUp, Vector3 gravDir, Vector3 velocity, float checkOffset = 0, bool multipleContactsPerCollider = true)
	{
		GroundingEdgeCollisionInfo edgeInfo = new GroundingEdgeCollisionInfo();
		normalsBufferSphere = ExtCollider.CollectNormalsOnCollider(collider, detectionOrigin, radius + checkOffset, normalsBufferSphere, multipleContactsPerCollider);

		if (collider is MeshCollider) 
		{
			edgeInfo = ProcessEdgeInfo ((MeshCollider)collider, detectionOrigin, radius, normalsBufferSphere, detectedGroundNormal, interpolatedNormal, transformUp, gravDir, velocity);
		} 
		else if (collider is BoxCollider) 
		{
			edgeInfo = ProcessEdgeInfo ((BoxCollider)collider, detectionOrigin, radius, normalsBufferSphere, detectedGroundNormal, interpolatedNormal, transformUp, gravDir, velocity);
		} 



		/*for(int j = 0; j < normalsBufferSphere.Count; j++)
		{
			//We calculate sphereDetectionOriginInCapsule for our depenetration method since we need to know where the spheres detection origin would be within the capsule.
			Vector3 sphereDetectionOriginInSphere = detectionOrigin;

			//We store just the radius, not radius + checkOffset, so that our depenetration method has the correct radius to depenetrate with.

			Debug.Log ("edgeNormal" + j + ": " + normalsBufferSphere [j]);
		}*/




		return edgeInfo;
	}


	public static GroundingEdgeCollisionInfo ProcessEdgeInfo (BoxCollider collider, Vector3 centre, float radius, List<ContactInfo> foundNormals, Vector3 detectedGroundNormal, Vector3 interpolatedNormal, Vector3 transformUp, Vector3 gravDir, Vector3 velocity) 
	{
		List<ContactInfo> possibleNormals = new List<ContactInfo> ();

		Vector3 closestGroundNormal = detectedGroundNormal;

		//first find which normal is closest to the current transformUp and make that the new ground normal
		/*float smallestAngleBetween = Mathf.Infinity;
		Vector3 closestGroundNormal = Vector3.zero;

		for (int i = 0; i < foundNormals.Count; i++) 
		{
			if (ExtVector3.IsInDirection (interpolatedNormal, foundNormals [i].normal, .001f, true)) 
			{
				float angleBetween = Vector3.Angle (transformUp.normalized, foundNormals [i].normal.normalized);
				if (angleBetween < smallestAngleBetween) 
				{
					smallestAngleBetween = angleBetween;
					closestGroundNormal = foundNormals [i].normal;
				}
			}
		}

		//Debug.Log (closestGroundNormal);*/

		//Vector3 flattenedInterpolatedNormal = Vector3.ProjectOnPlane (interpolatedNormal, closestGroundNormal);

		for (int i = 0; i < foundNormals.Count; i++) 
		{
			//obviously the ground normal is not an edge normal
			if (foundNormals [i].normal != closestGroundNormal) 
			{
				if (ExtVector3.IsInDirection (interpolatedNormal, foundNormals[i].normal, .001f, true))
				{
					possibleNormals.Add (foundNormals [i]);
				}
					
			} 
		}

		//Debug.Log ("possibleEdgeNormals.Count=" + possibleNormals.Count);

		ContactInfo detectedGroundPair = new ContactInfo (centre, closestGroundNormal);
		return new GroundingEdgeCollisionInfo (collider, centre, radius, detectedGroundPair, interpolatedNormal, possibleNormals, transformUp, gravDir, velocity);
	}


	public static GroundingEdgeCollisionInfo ProcessEdgeInfo (MeshCollider collider, Vector3 centre, float radius, List<ContactInfo> foundNormals, Vector3 detectedGroundNormal, Vector3 interpolatedNormal, Vector3 transformUp, Vector3 gravDir, Vector3 velocity) 
	{
		//Debug.Log ("total edges count: " + foundNormals.Count);

		List<ContactInfo> possibleEdgeNormals = new List<ContactInfo> ();

		for (int i = 0; i < foundNormals.Count; i++) 
		{
			//obviously the ground normal is not an edge normal
			if (foundNormals [i].normal != detectedGroundNormal) 
			{
				
				bool significantSimilarFound = false;

				//only add if not the same as existing added normal
				for (int j = 0; j < possibleEdgeNormals.Count; j++) 
				{
					//check if significantly different
					if (Vector3.Angle (foundNormals [i].normal, possibleEdgeNormals [j].normal) < 0.01f) 
					{
						significantSimilarFound = true;
					}
				}


				if (significantSimilarFound == false) 
				{
					possibleEdgeNormals.Add (foundNormals [i]);
				}
			} 
		}

		//Debug.Log ("possible edges count: " + possibleEdgeNormals.Count);

		ContactInfo detectedGroundPair = new ContactInfo (centre, detectedGroundNormal);
		return new GroundingEdgeCollisionInfo (collider, centre, radius, detectedGroundPair, interpolatedNormal, possibleEdgeNormals, transformUp, gravDir, velocity);
	}
}

