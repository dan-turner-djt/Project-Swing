using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PauseOptionsMenu : GenericMenu
{

	public ActionSceneController sc;

	public GameObject cameraControlSelectorObj;
	public UI_Selector cameraControlSelector;

	public GameObject cameraAutoSelectorObj;
	public UI_Selector cameraAutoSelector;

	public GameObject button_Back;
	public GameObject button_cameraControlSelect;
	public GameObject button_cameraAutoSelect;

	public CameraController cc;

	void Awake()
	{
		base.DoAwake();

		sc = GameObject.FindGameObjectWithTag("SceneController").GetComponent<ActionSceneController>();
	}

	void Start()
	{
		base.DoStart();

		interactables.Add(button_Back);
		interactables.Add(button_cameraControlSelect);
		interactables.Add(button_cameraAutoSelect);

	}



	public void BackButtonPressed()
	{
		BackButtonAction();
	}

	public void BackButtonAction()
	{
		uiStackManager.RemoveComponentFromStack(gameObject);
	}


	public void CameraControlSelectorButtonPressed()
	{
		selectedButton = button_cameraControlSelect;
		bool buttonSet = CheckSelectorButtonPressSet(cameraControlSelectorObj);


		if (buttonSet)
		{
			CameraControlSelectorButtonAction(true);
		}
		else
		{
			uiStackManager.AddComponentToStack(cameraControlSelectorObj);
		}
	}

	public void CameraControlSelectorButtonAction(bool response)
	{
		if (response)
		{
			cc.SetCameraControlSetting(cameraControlSelector.GetCurrentSelectionIndex());

			uiStackManager.RemoveComponentFromStack(cameraControlSelector.gameObject);
		}
		else
		{
			uiStackManager.RemoveComponentFromStack(cameraControlSelector.gameObject, true);
		}

	}


	public void CameraAutoSelectorButtonPressed()
	{
		selectedButton = button_cameraAutoSelect;
		bool buttonSet = CheckSelectorButtonPressSet(cameraAutoSelectorObj);

		if (buttonSet)
		{
			CameraAutoSelectorButtonAction(true);
		}
		else
		{
			uiStackManager.AddComponentToStack(cameraAutoSelectorObj);
		}
	}

	public void CameraAutoSelectorButtonAction(bool response)
	{
		if (response)
		{
			cc.SetCameraAutoSetting(cameraAutoSelector.GetCurrentSelectionIndex());

			uiStackManager.RemoveComponentFromStack(cameraAutoSelector.gameObject);
		}
		else
		{
			uiStackManager.RemoveComponentFromStack(cameraAutoSelector.gameObject, true);
		}

	}



	public override void TurnedOn(GameObject previousMenu)
	{

		cc = sc.GetMainCamera();

		if (previousMenu == null)
		{
			selectedButton = defaultSelectedButton;
		}

		SetButtonSelected(selectedButton);


		bool firstTime = !loadedBefore;
		loadedBefore = true;

		if (firstTime)
		{
			if (uiStackManager == null)
			{
				Debug.Log("not set");
			}

			cameraControlSelector.DoStart(cc.GetCameraControlList(), (int)cc.cameraControlSetting);
			cameraAutoSelector.DoStart(cc.GetCameraAutoList(), (int)cc.cameraAutoSetting);

		}

		cameraControlSelector.ComesIntoView();
		cameraControlSelector.ComesIntoView();

	}


	public override void TurnedOff()
	{
		
	}

}
