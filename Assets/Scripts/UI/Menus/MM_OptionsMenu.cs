using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MM_OptionsMenu : GenericMenu 
{


	public GameObject fullScreenSelectorObj;
	public UI_Selector fullScreenSelector;

	public GameObject resolutionSelectorObj;
	public UI_Selector resolutionSelector;

	public GameObject qualitySelectorObj;
	public UI_Selector qualitySelector;


	public GameObject button_Back;
	public GameObject button_FullScreen;
	public GameObject button_Resolution;
	public GameObject button_Quality;

	void Awake ()
	{
		base.DoAwake ();
	}

	void Start()
	{
		base.DoStart ();

		interactables.Add (button_Back);
		interactables.Add (button_FullScreen);
		interactables.Add (button_Resolution);
		interactables.Add (button_Quality);
	}



	public override void TurnedOn(GameObject previousMenu)
	{
		bool firstTime = !loadedBefore;
		loadedBefore = true;

		if (firstTime) 
		{
			fullScreenSelector.DoStart (uiStackManager.sc.gameController.toggleList, uiStackManager.sc.gameController.GetFullScreenIndex());
			resolutionSelector.DoStart (uiStackManager.sc.gameController.GetCurrentResolutionsList(), uiStackManager.sc.gameController.GetCurrentResolutionIndex());
			qualitySelector.DoStart (uiStackManager.sc.gameController.GetQualityList (), uiStackManager.sc.gameController.GetCurrentQualityIndex());


		} 
		else 
		{
			//resolutions selector depends on current fullscreen setting - may need updating if full screen has just changed
			resolutionSelector.DoStart (uiStackManager.sc.gameController.GetCurrentResolutionsList(), uiStackManager.sc.gameController.GetCurrentResolutionIndex());
		}


		if (previousMenu == null) 
		{
			selectedButton = defaultSelectedButton;
		}



		SetButtonSelected (selectedButton);

		//currently does nothing
		fullScreenSelector.ComesIntoView ();
		resolutionSelector.ComesIntoView ();
		qualitySelector.ComesIntoView ();


	}


	public override void ReceiveConfirmation (bool response)
	{
		if (toBeConfirmed == "qualityButtonAction") 
		{
			QualityButtonAction (response);
		}



		toBeConfirmed = "";
	}


	public override void DoUpdate()
	{

	}
		




	public void FullscreenButtonPressed ()
	{
		selectedButton = button_FullScreen;
		bool buttonSet = CheckSelectorButtonPressSet (fullScreenSelectorObj);


		if (buttonSet) 
		{
			bool fullScreen = (fullScreenSelector.GetCurrentSelectionIndex () == 0) ? true : false;

			uiStackManager.sc.gameController.SetFullscreenOptions (fullScreen);


			uiStackManager.RemoveComponentFromStack (fullScreenSelector.gameObject);
		} 
		else 
		{
			uiStackManager.AddComponentToStack (fullScreenSelectorObj);
		}


	}


	public void ResolutionButtonPressed ()
	{
		selectedButton = button_Resolution;
		bool buttonSet = CheckSelectorButtonPressSet (resolutionSelectorObj);


		if (buttonSet) 
		{
			bool fullScreen = (fullScreenSelector.GetSetSelectionIndex () == 0) ? true : false;

			uiStackManager.sc.gameController.SetResolutionOptions (fullScreen, resolutionSelector.GetCurrentSelectionIndex ());


			uiStackManager.RemoveComponentFromStack (resolutionSelector.gameObject);
		} 
		else 
		{
			uiStackManager.AddComponentToStack (resolutionSelectorObj);
		}



	}


	public void QualityButtonPressed ()
	{
		selectedButton = button_Quality;
		bool buttonSet = CheckSelectorButtonPressSet (qualitySelectorObj);


		if (buttonSet) 
		{
			/*toBeConfirmed = "qualityButtonAction";
			string message = "Requires a game restart to take effect, set quality and restart?";
			uiStackManager.AskForResponse (message);*/

			QualityButtonAction(true);
		} 
		else 
		{
			uiStackManager.AddComponentToStack (qualitySelectorObj);
		}
	
	}

	public void QualityButtonAction (bool response)
	{
		if (response) 
		{
			uiStackManager.sc.gameController.SetQuality (qualitySelector.GetCurrentSelectionIndex ());
			//uiStackManager.sc.gameController.RestartApplication ();

			uiStackManager.RemoveComponentFromStack(qualitySelector.gameObject);
		} 
		else 
		{
			uiStackManager.RemoveComponentFromStack (qualitySelector.gameObject, true);
		}


	}


	public void BackButtonPressed ()
	{
		uiStackManager.RemoveComponentFromStack (gameObject);
	}


}
