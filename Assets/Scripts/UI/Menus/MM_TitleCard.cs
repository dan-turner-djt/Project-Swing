using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MM_TitleCard : GenericMenu {

	public MainMenuSceneController sc;
	GameController gameController;
	public GameObject button_Start;
	public TextMeshProUGUI startText;
	public GameObject homeMenuObject;


	void Awake ()
	{
		base.DoAwake ();
	}

	void Start()
	{
		base.DoStart ();
		gameController = sc.gameController;


		interactables.Add (button_Start);
	}
		
	public override void DoUpdate ()
	{
		//im doing this just because it should always be selectable no matter what

		if (selectedButton == null) 
		{
			selectedButton = defaultSelectedButton;
		} 

		SetButtonSelected (selectedButton);


	}

	public override void TurnedOn(GameObject previousMenu)
	{
		if (selectedButton == null) 
		{
			selectedButton = defaultSelectedButton;
		} 


		SetButtonSelected (selectedButton);

		StartCoroutine (MakeTextFlash ());
	}



	public override void SleepMenu (GameObject exception = null)
	{
		DisableInteractables (exception);
	}


	public override void WakeMenu ()
	{
		EnableInteractables ();
	}



	public void StartButtonPressed (GameObject nextMenu)
	{
		selectedButton = button_Start;

		StartCoroutine (TransitionToMenu(nextMenu));
	}


	IEnumerator MakeTextFlash ()
	{
		while (true)
		{
			startText.alpha = (startText.alpha > 0.5f) ? 0 : 1;
			yield return new WaitForSeconds (0.6f);
		}
	}


	IEnumerator MakeTextBlink ()
	{
		while (true)
		{
			startText.alpha = (startText.alpha > 0.5f) ? 0 : 1;
			yield return new WaitForSeconds (0.05f);
		}
	}

	IEnumerator TransitionToMenu (GameObject nextMenu)
	{
		StopCoroutine (MakeTextFlash ());
		StartCoroutine (MakeTextBlink ());

		yield return new WaitForSeconds (0.8f);
		StopCoroutine (MakeTextBlink ());
		uiStackManager.AddComponentToStack (nextMenu);
	}
}
