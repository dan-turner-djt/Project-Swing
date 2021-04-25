using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoosterController : MonoBehaviour {

	float speed = 18;
	public float useTime;


	public Vector3 GetVelocity ()
	{
		return transform.forward * speed;
	}
}
