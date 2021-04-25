using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ExtCollider {


	#region Sphere
	public static List<ContactInfo> ClosestPointsOnSurface (Collider collider, Vector3 centre, float radius, List<ContactInfo> resultsBuffer, bool multipleContacts = true, bool forGroundCast = false)
	{
		resultsBuffer.Clear();

		if(collider is MeshCollider)
		{
			MeshAABBTree meshTree = collider.GetComponent<MeshAABBTree>();
			if(meshTree != null)
			{
				if(multipleContacts)
				{
					meshTree.ClosestPointsOnSurface(centre, radius, resultsBuffer, forGroundCast);
				}
			}
		}
		else if(collider is BoxCollider) resultsBuffer.Add(ClosestPointOnSurface((BoxCollider)collider, centre));
		else if(collider is SphereCollider) resultsBuffer.Add(ClosestPointOnSurface((SphereCollider)collider, centre));
		else if(collider is CapsuleCollider) resultsBuffer.Add(ClosestPointOnSurface((CapsuleCollider)collider, centre));

		return resultsBuffer;
	}


	public static List<ContactInfo> CollectNormalsOnCollider (Collider collider, Vector3 centre, float radius, List<ContactInfo> resultsBuffer, bool multipleContacts = true)
	{
		resultsBuffer.Clear();

		if (collider is MeshCollider) 
		{
			MeshAABBTree meshTree = collider.GetComponent<MeshAABBTree> ();
			if (meshTree != null) 
			{
				if (multipleContacts) 
				{
					meshTree.CollectNormalsOnCollider (centre, radius, resultsBuffer);
				}
			}
		} 
		else if (collider is BoxCollider) 
		{
			CollectNormalsOnCollider ((BoxCollider)collider, centre, radius, resultsBuffer);
		}
		//sphere and capsule cant have edges

		return resultsBuffer;
	}


	public static ContactInfo ClosestPointOnSurface(SphereCollider collider, Vector3 centre)
	{
		Vector3 localCentre = ToLocal (collider, collider.center, centre);

		ContactInfo contact = new ContactInfo();
		contact.normal = localCentre - Vector3.zero;
		contact.normal = CheckAndSetNormal(contact.normal, Vector3.zero, localCentre);
		contact.point = Vector3.zero + (contact.normal * collider.radius);

		return ToGlobal(collider, collider.center, contact);
	}


	public static ContactInfo ClosestPointOnSurface(CapsuleCollider collider, Vector3 centre)
	{
		CapsuleShape points = CapsuleShape.CapsuleColliderLocalPoints(collider);

		Vector3 localCentre = ToLocal(collider, collider.center, centre);

		Vector3 closest = Geometry.ClosestPointOnLineSegmentToPoint(localCentre, points.top, points.bottom);

		ContactInfo contact = new ContactInfo();
		contact.normal = localCentre - closest;
		contact.normal = CheckAndSetNormal(contact.normal, Vector3.zero, localCentre * .5f);
		contact.point = closest + (contact.normal * collider.radius);

		return ToGlobal(collider, collider.center, contact);
	}



	public static ContactInfo ClosestPointOnSurface(BoxCollider collider, Vector3 centre)
	{
		Vector3 localCentre = ToLocal(collider, collider.center, centre);
		Vector3 extents = collider.size;
		Vector3 halfExtents = extents * .5f;

		//We try to choose the best 3 faces on the box to do our rectangle tests.
		//Is it safe to use the lineCenter as the reference point?
		Vector3 xAxis = new Vector3(Mathf.Sign(localCentre.x), 0, 0);
		Vector3 yAxis = new Vector3(0, Mathf.Sign(localCentre.y), 0);
		Vector3 zAxis = new Vector3(0, 0, Mathf.Sign(localCentre.z));

		Rect3D xRect = new Rect3D(Vector3.Scale(xAxis, halfExtents), Vector3.forward, Vector3.up, extents.z, extents.y);
		Rect3D yRect = new Rect3D(Vector3.Scale(yAxis, halfExtents), Vector3.right, Vector3.forward, extents.x, extents.z);
		Rect3D zRect = new Rect3D(Vector3.Scale(zAxis, halfExtents), Vector3.right, Vector3.up, extents.x, extents.y);

		IntersectPoints xIntersect = Geometry.ClosestPointOnRectangleToPoint(localCentre, xRect, true);
		float xDistance = (xIntersect.second - xIntersect.first).sqrMagnitude;
		IntersectPoints yIntersect = Geometry.ClosestPointOnRectangleToPoint(localCentre, yRect, true);
		float yDistance = (yIntersect.second - yIntersect.first).sqrMagnitude;
		IntersectPoints zIntersect = Geometry.ClosestPointOnRectangleToPoint(localCentre, zRect, true);
		float zDistance = (zIntersect.second - zIntersect.first).sqrMagnitude;

		IntersectPoints closestIntersect = new IntersectPoints();
		Vector3 closestNormal = Vector3.zero;
		if(xDistance <= yDistance && xDistance <= zDistance)
		{
			closestIntersect = xIntersect;
			closestNormal = xAxis;
		}
		else if(yDistance <= xDistance && yDistance <= zDistance)
		{
			closestIntersect = yIntersect;
			closestNormal = yAxis;
		}
		else
		{
			closestIntersect = zIntersect;
			closestNormal = zAxis;
		}

		//Two intersect distances might be the same, so we need to choose the best normal
		//Must compare with ExtMathf.Approximately since float precision can cause errors, especially when dealing with different scales.
		if(ExtMathf.Approximately(xDistance, yDistance, .0001f) || ExtMathf.Approximately(xDistance, zDistance, .0001f) || ExtMathf.Approximately(yDistance, zDistance, .0001f))
		{
			//We need to scale by the colliders scale for reasons I am not too sure of. Has to do with if the collider is scaled weird,
			//in local space it is just a uniform box which throws off our direction calculation below. Not sure if we should use local or lossy scale.
			Vector3 closestDirection = Vector3.Scale(closestIntersect.first - closestIntersect.second, collider.transform.localScale);

			float xDot = Vector3.Dot(closestDirection, xAxis);
			float yDot = Vector3.Dot(closestDirection, yAxis);
			float zDot = Vector3.Dot(closestDirection, zAxis);

			if(xDot >= yDot && xDot >= zDot)
			{
				closestNormal = xAxis;
			}
			else if(yDot >= xDot && yDot >= zDot)
			{
				closestNormal = yAxis;
			}
			else
			{
				closestNormal = zAxis;
			}
		}

		return ToGlobal(collider, collider.center, new ContactInfo(closestIntersect.second, closestNormal));
	}


	public static List<ContactInfo> CollectNormalsOnCollider(BoxCollider collider, Vector3 centre, float radius, List<ContactInfo> resultsBuffer)
	{
		resultsBuffer.Clear();

		Vector3 localCentre = ToLocal(collider, collider.center, centre);
		Vector3 extents = collider.size;
		Vector3 halfExtents = extents * .5f;

		//We try to choose the best 3 faces on the box to do our rectangle tests.
		//Is it safe to use the lineCenter as the reference point?
		Vector3 xAxis = new Vector3(Mathf.Sign(localCentre.x), 0, 0);
		Vector3 yAxis = new Vector3(0, Mathf.Sign(localCentre.y), 0);
		Vector3 zAxis = new Vector3(0, 0, Mathf.Sign(localCentre.z));

		resultsBuffer.Add (new ContactInfo (centre, collider.transform.TransformDirection(xAxis.normalized)));
		resultsBuffer.Add (new ContactInfo (centre, collider.transform.TransformDirection(yAxis.normalized)));
		resultsBuffer.Add (new ContactInfo (centre, collider.transform.TransformDirection(zAxis.normalized)));
		return resultsBuffer;
	}
	#endregion




	#region Capsule
	public static List<ContactInfo> ClosestPointsOnSurface (Collider collider, Vector3 segment0, Vector3 segment1, float radius, List<ContactInfo> resultsBuffer, bool multipleContacts = true)
	{
		resultsBuffer.Clear();

		if(collider is MeshCollider)
		{
			MeshAABBTree meshTree = collider.GetComponent<MeshAABBTree>();
			if(meshTree != null)
			{
				if(multipleContacts)
				{
					meshTree.ClosestPointsOnSurface(segment0, segment1, radius, resultsBuffer);
				}
				else
				{
					ContactInfo contact = meshTree.ClosestPointOnSurface(segment0, segment1, radius);
					if(contact.point != Vector3.zero) resultsBuffer.Add(contact);
				}
			}
		}
		else if(collider is BoxCollider) resultsBuffer.Add(ClosestPointOnSurface((BoxCollider)collider, segment0, segment1));
		else if(collider is SphereCollider) resultsBuffer.Add(ClosestPointOnSurface((SphereCollider)collider, segment0, segment1));
		else if(collider is CapsuleCollider) resultsBuffer.Add(ClosestPointOnSurface((CapsuleCollider)collider, segment0, segment1));

		return resultsBuffer;
	}



	public static ContactInfo ClosestPointOnSurface(BoxCollider collider, Vector3 segment0, Vector3 segment1)
	{
		Vector3 localSegment0 = ToLocal(collider, collider.center, segment0);
		Vector3 localSegment1 = ToLocal(collider, collider.center, segment1);
		Vector3 lineCenter = (localSegment0 + localSegment1) * .5f;
		Vector3 extents = collider.size;
		Vector3 halfExtents = extents * .5f;

		//We try to choose the best 3 faces on the box to do our rectangle tests.
		//Is it safe to use the lineCenter as the reference point?
		Vector3 xAxis = new Vector3(Mathf.Sign(lineCenter.x), 0, 0);
		Vector3 yAxis = new Vector3(0, Mathf.Sign(lineCenter.y), 0);
		Vector3 zAxis = new Vector3(0, 0, Mathf.Sign(lineCenter.z));

		Rect3D xRect = new Rect3D(Vector3.Scale(xAxis, halfExtents), Vector3.forward, Vector3.up, extents.z, extents.y);
		Rect3D yRect = new Rect3D(Vector3.Scale(yAxis, halfExtents), Vector3.right, Vector3.forward, extents.x, extents.z);
		Rect3D zRect = new Rect3D(Vector3.Scale(zAxis, halfExtents), Vector3.right, Vector3.up, extents.x, extents.y);

		IntersectPoints xIntersect = Geometry.ClosestPointOnRectangleToLine(localSegment0, localSegment1, xRect, true);
		float xDistance = (xIntersect.second - xIntersect.first).sqrMagnitude;
		IntersectPoints yIntersect = Geometry.ClosestPointOnRectangleToLine(localSegment0, localSegment1, yRect, true);
		float yDistance = (yIntersect.second - yIntersect.first).sqrMagnitude;
		IntersectPoints zIntersect = Geometry.ClosestPointOnRectangleToLine(localSegment0, localSegment1, zRect, true);
		float zDistance = (zIntersect.second - zIntersect.first).sqrMagnitude;

		IntersectPoints closestIntersect = new IntersectPoints();
		Vector3 closestNormal = Vector3.zero;
		if(xDistance <= yDistance && xDistance <= zDistance)
		{
			closestIntersect = xIntersect;
			closestNormal = xAxis;
		}
		else if(yDistance <= xDistance && yDistance <= zDistance)
		{
			closestIntersect = yIntersect;
			closestNormal = yAxis;
		}
		else
		{
			closestIntersect = zIntersect;
			closestNormal = zAxis;
		}

		//Two intersect distances might be the same, so we need to choose the best normal
		//Must compare with ExtMathf.Approximately since float precision can cause errors, especially when dealing with different scales.
		if(ExtMathf.Approximately(xDistance, yDistance, .0001f) || ExtMathf.Approximately(xDistance, zDistance, .0001f) || ExtMathf.Approximately(yDistance, zDistance, .0001f))
		{
			//We need to scale by the colliders scale for reasons I am not too sure of. Has to do with if the collider is scaled weird,
			//in local space it is just a uniform box which throws off our direction calculation below. Not sure if we should use local or lossy scale.
			Vector3 closestDirection = Vector3.Scale(closestIntersect.first - closestIntersect.second, collider.transform.localScale);

			float xDot = Vector3.Dot(closestDirection, xAxis);
			float yDot = Vector3.Dot(closestDirection, yAxis);
			float zDot = Vector3.Dot(closestDirection, zAxis);

			if(xDot >= yDot && xDot >= zDot)
			{
				closestNormal = xAxis;
			}
			else if(yDot >= xDot && yDot >= zDot)
			{
				closestNormal = yAxis;
			}
			else
			{
				closestNormal = zAxis;
			}
		}

		return ToGlobal(collider, collider.center, new ContactInfo(closestIntersect.second, closestNormal));
	}


	public static ContactInfo ClosestPointOnSurface(SphereCollider collider, Vector3 segment0, Vector3 segment1)
	{
		Vector3 localSegment0 = ToLocal(collider, collider.center, segment0);
		Vector3 localSegment1 = ToLocal(collider, collider.center, segment1);

		Vector3 closest = Geometry.ClosestPointOnLineSegmentToPoint(Vector3.zero, localSegment0, localSegment1);

		ContactInfo contact = new ContactInfo();
		contact.normal = closest - Vector3.zero;
		contact.normal = CheckAndSetNormal(contact.normal, Vector3.zero, (localSegment0 + localSegment1) * .5f);
		contact.point = Vector3.zero + (contact.normal * collider.radius);

		return ToGlobal(collider, collider.center, contact);
	}


	public static ContactInfo ClosestPointOnSurface(CapsuleCollider collider, Vector3 segment0, Vector3 segment1)
	{
		CapsuleShape points = CapsuleShape.CapsuleColliderLocalPoints(collider);

		Vector3 localSegment0 = ToLocal(collider, collider.center, segment0);
		Vector3 localSegment1 = ToLocal(collider, collider.center, segment1);

		IntersectPoints closests = Geometry.ClosestPointsOnTwoLineSegments(localSegment0, localSegment1, points.top, points.bottom);

		ContactInfo contact = new ContactInfo();
		contact.normal = closests.first - closests.second;
		contact.normal = CheckAndSetNormal(contact.normal, Vector3.zero, (localSegment0 + localSegment1) * .5f);
		contact.point = closests.second + (contact.normal * collider.radius);

		return ToGlobal(collider, collider.center, contact);
	}
	#endregion


	static Vector3 ToLocal(Collider collider, Vector3 colliderCenter, Vector3 point)
	{
		return collider.transform.InverseTransformPoint(point) - colliderCenter;
	}

	static ContactInfo ToGlobal(Collider collider, Vector3 colliderCenter, ContactInfo contact)
	{
		contact.point = collider.transform.TransformPoint(contact.point + colliderCenter);
		contact.normal = collider.transform.TransformDirection(contact.normal); //transform.TransformVector might be better?
		return contact;
	}

	static Vector3 CheckAndSetNormal(Vector3 normal, Vector3 colliderCenter, Vector3 pointCenter)
	{
		if(normal == Vector3.zero)
		{
			normal = pointCenter - colliderCenter; //closest points are right ontop of eachother, so we use centers

			if(normal == Vector3.zero)
			{
				normal = Vector3.up; //We are right ontop of eachother, set normal to anything
			}
		}

		return normal.normalized;
	}

}
