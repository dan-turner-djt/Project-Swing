using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Updatable : MonoBehaviour {

	public ActionSceneController sc;
	public bool lateUpdatable = false;

	protected void DoAwake ()
	{
		sc = GameObject.Find ("SceneController").GetComponent<ActionSceneController> ();
	}


	protected void DoStart () 
	{
		/*if (lateUpdatable) 
		{
			sc.AddLateUpdatableToList (gameObject);
		} 
		else 
		{
			sc.AddUpdatableToList (gameObject);
		}*/

		sc.AddUpdatableToList (gameObject);
	}
	

	public virtual void DoUpdate () 
	{
		
	}

	public virtual void DoFinalUpdate ()
	{

	}
}
