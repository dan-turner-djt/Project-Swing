using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct ContactInfo
{
	public Vector3 point;
	public Vector3 normal;

	public ContactInfo(Vector3 point, Vector3 normal)
	{
		this.point = point;
		this.normal = normal;
	}
}
