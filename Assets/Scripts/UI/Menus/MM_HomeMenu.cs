using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MM_HomeMenu : GenericMenu {

	public MainMenuSceneController sc;

	public GameObject button_Play;
	public GameObject button_LevelEdit;
	public GameObject button_Options;
	public GameObject button_Quit;

	void Awake ()
	{
		base.DoAwake ();
	}

	void Start()
	{
		base.DoStart ();

		interactables.Add (button_Play);
		interactables.Add (button_LevelEdit);
		interactables.Add (button_Options);
		interactables.Add (button_Quit);
	}


	public override void TurnedOn(GameObject previousMenu)
	{
		if (selectedButton == null) 
		{
			selectedButton = defaultSelectedButton;
		} 

			
		SetButtonSelected (selectedButton);

	}



	public override void ReceiveConfirmation (bool response)
	{
		
		if (toBeConfirmed == "quitButtonAction") 
		{
			QuitButtonAction (response);
		}



		toBeConfirmed = "";
	}


	public override void SleepMenu (GameObject exception = null)
	{
		DisableInteractables (exception);
	}


	public override void WakeMenu ()
	{
		EnableInteractables ();
	}



	public void PlayButtonPressed (GameObject nextMenu)
	{
		selectedButton = button_Play;
		uiStackManager.AddComponentToStack (nextMenu);
	}


	public void OptionsButtonPressed (GameObject nextMenu)
	{
		selectedButton = button_Options;
		uiStackManager.AddComponentToStack (nextMenu);
	}

	public void QuitButtonPressed ()
	{
		selectedButton = button_Quit;

		toBeConfirmed = "quitButtonAction";
		string message = "Are you sure you want to quit?";
		uiStackManager.AskForResponse (message);
	}

	public void QuitButtonAction (bool response)
	{
		if (response) 
		{
			uiStackManager.sc.gameController.QuitGame();
		}

	}
}
