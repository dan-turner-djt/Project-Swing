using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractionsController : MonoBehaviour 
{
	public PlayerActionsController playerController;


	bool touchingHintObject;

	public enum DeathType
	{
		Fall, Damage, Crush
	}

	public LayerMask interactablesLayer;

	List <GameObject> hitInteractables = new List <GameObject>();


	public void DoUpdate ()
	{
		
	}



	public void CheckForInteractables ()
	{
		Collider[] hits = Physics.OverlapSphere (transform.position, playerController.sphereRadius, interactablesLayer);

		for (int i = 0; i < hits.Length; i++) 
		{
			if (hits [i].gameObject.tag == "DeathPlane") 
			{
				if (!playerController.dying)  //prevent repeatedly telling player to die
				{
					InteractWithDeathPlane (hits [i].gameObject); //deal with this straight away, as there is no point continuing collision if we know we're going to die anyway
				}
			} 
			else 
			{
				hitInteractables.Add (hits [i].gameObject); //otherwise add it to the list to be dealt with after collision has completed
			}
		}
	}


	public void DealWithInteractables (float deltaTime)
	{
		bool foundHintObject = false;

		for (int i = 0; i < hitInteractables.Count; i++) 
		{
			GameObject interactableObject = hitInteractables [i].gameObject;

			switch (interactableObject.tag)
			{
			case "Collectable":
				InteractWithCollectable (interactableObject);
				break;
			case "Checkpoint":
				InteractWithCheckpoint (interactableObject);
				break;
			case "GroundBooster":
				InteractWithGroundBooster (interactableObject);
				break;
			case "AirBooster":
				InteractWithAirBooster (interactableObject);
				break;
			case "Spring":
				InteractWithSpring (interactableObject);
				break;
			case "HintObject":
				foundHintObject = true;
				InteractWithHintObject (interactableObject);
				break;

			default:
				Debug.Log ("unknown interactable");
				break;
			}
		}


		hitInteractables.Clear (); //contents should only exist for one update

		if (!foundHintObject)
        {
			touchingHintObject = false;
        }
	}


	void InteractWithDeathPlane (GameObject deathPlane)
	{
		Die (DeathType.Fall);
	}
		

	void InteractWithCollectable (GameObject collectable)
	{
		playerController.sc.AddCollectable ();
		GameObject.Destroy (collectable);
	}


	void InteractWithCheckpoint (GameObject checkpointBall)
	{
		checkpointBall.GetComponentInParent<CheckpointController> ().CheckpointTouched();
	}


	void InteractWithHintObject (GameObject hintObject)
    {
		if (!touchingHintObject)
        {
			HintObjectController hoc = hintObject.GetComponent<HintObjectController>();
			string textSequenceName = hoc.textSequenceName;

			touchingHintObject = true;

			playerController.sc.StartTextSequence(textSequenceName, false);
		}
		
    }

	void InteractWithGroundBooster (GameObject booster)
	{
		if (!playerController.groundInfo.GetIsGrounded()) 
		{
			return; //only allow interaction if grounded
		}

		//this so the coroutine is always started on the last touch of the booster (and we only keep one running)
		if (playerController.cUseGroundBooster != null) 
			StopCoroutine (playerController.cUseGroundBooster);
			

		if (playerController.cUseAirBooster != null) 
		{
			StopCoroutine (playerController.cUseAirBooster);
			playerController.currentlyUsingAirBooster = false;
		}
		if (playerController.cUseSpring != null) 
		{
			StopCoroutine (playerController.cUseSpring);
			playerController.currentlyUsingSpring = false;
		}
		

		playerController.cUseGroundBooster = playerController.UseGroundBooster (booster);
		StartCoroutine (playerController.cUseGroundBooster);
	}


	void InteractWithAirBooster (GameObject booster)
	{
		//this so the coroutine is always started on the last touch of the booster (and we only keep one running)
		if (playerController.cUseAirBooster != null)
			StopCoroutine (playerController.cUseAirBooster);

		if (playerController.cUseGroundBooster != null) 
		{
			StopCoroutine (playerController.cUseGroundBooster);
			playerController.currentlyUsingBooster = false;
		}
		if (playerController.cUseSpring != null) 
		{
			StopCoroutine (playerController.cUseSpring);
			playerController.currentlyUsingSpring = false;
		}

		playerController.isJumping = false;

		playerController.cUseAirBooster = playerController.UseAirBooster (booster);
		StartCoroutine (playerController.cUseAirBooster);
	}


	void InteractWithSpring (GameObject spring)
	{
		//this so the coroutine is always started on the last touch of the spring (and we only keep one running)
		if (playerController.cUseSpring != null)
			StopCoroutine (playerController.cUseSpring);

		if (playerController.cUseGroundBooster != null) 
		{
			StopCoroutine (playerController.cUseGroundBooster);
			playerController.currentlyUsingBooster = false;
		}
		if (playerController.cUseAirBooster != null) 
		{
			StopCoroutine (playerController.cUseAirBooster);
			playerController.currentlyUsingAirBooster = false;
		}
		

		playerController.cUseSpring = playerController.UseSpring (spring);
		StartCoroutine (playerController.cUseSpring);
	}


	public void Die (DeathType deathType)
	{
		playerController.dying = true;
		playerController.currentlyControllable = false;

		playerController.sc.gameController.ChangePlayerLives (-1);

		switch (deathType) 
		{
		case DeathType.Fall:
			FallDeath ();
			break;
		case DeathType.Damage:
			DamageDeath ();
			break;
		case DeathType.Crush:
			CrushDeath ();
			break;
		default:
			Debug.Log ("unknown death type");
			break;
		}
	}


	void FallDeath ()
	{
		playerController.sc.PlayerDied ();
	}

	void DamageDeath ()
	{
		
		playerController.sc.PlayerDied ();
	}

	void CrushDeath ()
	{
		Debug.Log ("crushed!");
		playerController.sc.PlayerDied ();
	}


}
