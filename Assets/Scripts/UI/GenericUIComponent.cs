using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericUIComponent : MonoBehaviour {

	public UIStackManager uiStackManager;

	public bool loadedBefore = false;
	protected string toBeConfirmed;

	public enum ComponentType
	{
		Menu, Selector
	}
	public ComponentType componentType;


	public virtual void DoAwake () 
	{
		uiStackManager = GameObject.FindGameObjectWithTag("SceneController").GetComponent<UIStackManager>();
	}

	public virtual void DoStart ()
	{
		
	}


	public virtual void DoUpdate ()
	{

	}


	public virtual void ReceiveConfirmation (bool response)
	{

	}

	public bool CheckSelectorButtonPressSet (GameObject selector)
	{
		if (!GameObject.ReferenceEquals (uiStackManager.GetLatestStackGameObject (), selector)) 
		{
			//uiStackManager.AddComponentToStack (selector);
			return false;
		} 
		else 
		{
			//uiStackManager.RemoveComponentFromStack ();
			return true;
		}
	}


	public virtual void SetMessage (string message) 
	{

	}


}
