using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpringController : MonoBehaviour {

	float power = 18;
	public float useTime;


	public Vector3 GetVelocity ()
	{
		return transform.up * power;
	}


	
}
