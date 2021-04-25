using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GenericMenu : GenericUIComponent 
{

	public List<GameObject> interactables;

	public enum MenuType
	{
		Fixed, Floating
	}
	public MenuType menuType;
	public bool temporaryMenu = false;


	public GameObject defaultSelectedButton;
	public GameObject selectedButton;

	public override void DoAwake()
	{
		base.DoAwake ();
	}

	public override void DoStart ()
	{
		base.DoStart ();
	}
		
	

	public override void DoUpdate () 
	{
		
	}


	public virtual void TurnedOn (GameObject previousMenu)
	{
		
	}


	public virtual void TurnedOff ()
	{
		
	}


	public virtual void BackCancelled ()
    {

    }


	public virtual void SleepMenu (GameObject exception = null)
	{
		DisableInteractables (exception);
	}


	public virtual void WakeMenu ()
	{
		EnableInteractables ();
	}


	public void SetButtonSelected (GameObject button)
	{
		EventSystem.current.SetSelectedGameObject (null);
		EventSystem.current.SetSelectedGameObject(button);
	}



	public void EnableInteractables ()
	{
		for (int i = 0; i < interactables.Count; i++) 
		{
			interactables [i].GetComponent<Button> ().interactable = true;
		}
	}


	public void DisableInteractables (GameObject exception = null)
	{
		for (int i = 0; i < interactables.Count; i++) 
		{
			if (GameObject.ReferenceEquals(exception, interactables[i])) continue;

			interactables [i].GetComponent<Button> ().interactable = false;
		}
	}





}
