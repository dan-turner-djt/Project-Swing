using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UI_Selector : GenericUIComponent {

	public GameObject buttonObj;
	public TextMeshProUGUI textObj;
	public GameObject leftArrow;
	public GameObject rightArrow;

	List<string> selectionList = new List<string>();

	private int defaultSelectionIndex = 0;
	private int currentSelectionIndex = 0;
	private int temporarySelectionIndex = 0;
	private float lastInput = 0;

	void Awake ()
	{
		base.DoAwake ();
	}

	void Start ()
	{
		base.DoStart ();
	}

	public void DoStart (List<string> list, int startingIndex)
	{
		FillSelectionList (list);
		defaultSelectionIndex = startingIndex;
		SetSelectionIndex (defaultSelectionIndex);
	}


	public override void DoUpdate () 
	{
		if (CheckSelected()) 
		{
			float currentInput = Input.GetAxisRaw ("Horizontal");

			if (currentInput != 0 && lastInput == 0) 
			{
				CycleList (Mathf.Sign (currentInput));
				UpdateText (true);
			}


			lastInput = currentInput;
		}

	}


	public override void ReceiveConfirmation (bool response)
	{
		//a selector receiving a response should pass the info to the menu holding it, since that is what handles the actions the selector incurs
		uiStackManager.PassResponseUp (response);
	}


	public void Activated() 
	{
		leftArrow.SetActive (true);
		rightArrow.SetActive (true);

		temporarySelectionIndex = currentSelectionIndex;
	}


	public void Deactivated(bool cancelled)
	{
		leftArrow.SetActive (false);
		rightArrow.SetActive (false);


		if (cancelled) 
		{
			
		} 
		else 
		{
			//set selection
			currentSelectionIndex = temporarySelectionIndex;
		}

		UpdateText ();
	}


	public void ComesIntoView()
	{

	}


	void CycleList (float dir) 
	{
		temporarySelectionIndex += (int) dir;

		if (temporarySelectionIndex >= selectionList.Count) 
		{
			temporarySelectionIndex = 0;
		} 
		else if (temporarySelectionIndex < 0) 
		{
			temporarySelectionIndex = selectionList.Count-1;
		}
	}


	public void UpdateText (bool useTemporary = false) 
	{
		if (useTemporary) 
		{
			textObj.SetText (selectionList[temporarySelectionIndex]);
		} 
		else 
		{
			textObj.SetText (selectionList[currentSelectionIndex]);
		}

	}




	bool CheckSelected () 
	{
		if (GameObject.ReferenceEquals (EventSystem.current.currentSelectedGameObject, buttonObj))
			return true;

		return false;
	}

	public void SetSelectionIndex (int setIndex)
	{
		currentSelectionIndex = setIndex;
		UpdateText (false);
	}

	public string GetCurrentSelection ()
	{
		return selectionList [currentSelectionIndex];
	}


	public int GetSetSelectionIndex ()
	{
		return currentSelectionIndex;
	}

	public int GetCurrentSelectionIndex ()
	{
		return temporarySelectionIndex;
	}


	public void FillSelectionList (List<string> list) 
	{
		selectionList = list;
	}


	public void RefreshSelecter (List<string> list)
	{
		FillSelectionList (list);
	}


	/*public void ButtonPressed ()
	{
		if (!GameObject.ReferenceEquals (uiStackManager.GetLatestStackGameObject (), gameObject)) 
		{
			uiStackManager.AddComponentToStack (gameObject);
		} 
		else 
		{
			uiStackManager.RemoveComponentFromStack ();
		}


	}*/
}
