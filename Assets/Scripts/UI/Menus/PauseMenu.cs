using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenu : GenericMenu {

	public ActionSceneController sc;


	public GameObject button_Continue;
	public GameObject button_Restart;
	public GameObject button_Options;
	public GameObject button_Quit;

	void Awake ()
	{
		base.DoAwake ();

		sc = GameObject.FindGameObjectWithTag("SceneController").GetComponent<ActionSceneController>();
	}

	void Start ()
	{
		base.DoStart ();

		interactables.Add (button_Continue);
		interactables.Add (button_Restart);
		interactables.Add (button_Options);
		interactables.Add (button_Quit);
	}



	public override void ReceiveConfirmation (bool response)
	{
		
		if (toBeConfirmed == "quitButtonAction") 
		{
			QuitButtonAction (response);
		}
		else if (toBeConfirmed == "restartButtonAction") 
		{
			RestartButtonAction (response);
		}


		toBeConfirmed = "";
	}


    public override void BackCancelled()
    {
        base.BackCancelled();

		sc.UnPause();
    }


    public void ContinueButtonPressed () 
	{
		selectedButton = button_Continue;

		uiStackManager.RemoveComponentFromStack (gameObject);
		sc.UnPause ();

	}
		
	public void RestartButtonPressed () 
	{
		selectedButton = button_Restart;

		toBeConfirmed = "restartButtonAction";
		string message = "Are you sure you want to restart? (Any unsaved progress will be lost)";
		uiStackManager.AskForResponse (message);
	}

	public void RestartButtonAction (bool response)
	{
		if (response) 
		{
			sc.RestartScene ();
		}

	}

	public void OptionsButtonPressed () 
	{
		selectedButton = button_Options;

		uiStackManager.AddComponentToStack(sc.pauseOptionsMenu);
	}
		
	public void QuitButtonPressed () 
	{
		selectedButton = button_Quit;

		toBeConfirmed = "quitButtonAction";
		string message = "Are you sure you want to quit? (Any unsaved progress will be lost)";
		uiStackManager.AskForResponse (message);
	}

	public void QuitButtonAction(bool response)
	{
		if (response)  
		{
			sc.QuitScene ();
		}

	}


	public override void TurnedOn(GameObject previousMenu)
	{
		if (previousMenu == null) 
		{
			selectedButton = defaultSelectedButton;
		}



		SetButtonSelected (selectedButton);
	}


	public override void TurnedOff ()
	{
		
	}
		
}
