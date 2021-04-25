using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MM_LevelSelectMenu : GenericMenu 
{
	public GameObject levelSelectorObj;
	public UI_Selector levelSelector;

	public GameObject button_Play;
	public GameObject button_LevelSelect;
	public GameObject button_Back;


	void Awake ()
	{
		base.DoAwake ();
	}

	void Start ()
	{
		base.DoStart ();

		interactables.Add (button_Play);
		interactables.Add (button_LevelSelect);
		interactables.Add (button_Back);

	}


	public override void TurnedOn(GameObject previousMenu)
	{
		bool firstTime = !loadedBefore;
		loadedBefore = true;

		if (firstTime) 
		{
			if (uiStackManager == null)
            {
				Debug.Log("not set");
            }
			levelSelector.DoStart (uiStackManager.sc.gameController.levelList, 0);
			//Debug.Log ("level list length: " + gameController.levelList.Count);
		}

		if (previousMenu == null) 
		{
			selectedButton = defaultSelectedButton;
		}


		SetButtonSelected (selectedButton);


		levelSelector.ComesIntoView ();
	}



	public override void DoUpdate()
	{
		
	}




	public void PlayButtonPressed () 
	{
		uiStackManager.sc.ChangeScene (levelSelector.GetCurrentSelection());
	}


	public void BackButtonPressed ()
	{
		uiStackManager.RemoveComponentFromStack (gameObject);
	}


	public void LevelSelectorButtonPressed ()
	{
		selectedButton = button_LevelSelect;
		bool buttonSet = CheckSelectorButtonPressSet (levelSelectorObj);

		if (buttonSet) 
		{
			uiStackManager.RemoveComponentFromStack (levelSelector.gameObject);
		} 
		else 
		{
			uiStackManager.AddComponentToStack (levelSelectorObj);
		}
	}
}
