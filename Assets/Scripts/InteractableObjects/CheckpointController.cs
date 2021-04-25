using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointController : MonoBehaviour {

	public ActionSceneController sc;

	public GameObject ballObject;
	Color activatedColour;

	public bool activated;


	void Start () 
	{
		sc = GameObject.Find ("SceneController").GetComponent<ActionSceneController> ();

		activatedColour = ballObject.GetComponent<MeshRenderer> ().material.color;

		if (sc.GetCurrentActivatedCheckpoint() == null || !GameObject.ReferenceEquals (sc.GetCurrentActivatedCheckpoint(), gameObject)) //check its not the saved checkpoint before setting it unactivated
		{
			DeactivateCheckpoint ();
		}


	}
	

	public void CheckpointTouched ()
	{
		if (!activated) 
		{
			ActivateCheckpoint ();
		}
	}


	public void ActivateCheckpoint ()
	{
		activated = true;
		ballObject.GetComponent<MeshRenderer> ().material.color = activatedColour;

		sc.SetNewCheckPoint (gameObject);
	}


	public void DeactivateCheckpoint ()
	{
		activated = false;
		ballObject.GetComponent<MeshRenderer> ().material.color = new Color (1, 1, 1, 1);
	}
}
