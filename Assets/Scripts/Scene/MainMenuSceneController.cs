using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuSceneController : GeneralSceneController {

	public GameObject titleCardObject;
	public GameObject homeMenuObject;
	public GameObject levelSelectMenuObject;
	public GameObject optionsMenuObject;

	MM_TitleCard titleCardMenu;
	MM_HomeMenu homeMenu;
	MM_LevelSelectMenu levelSelectMenu;
	MM_OptionsMenu optionsMenu;

	void Awake ()
	{
		base.DoAwake ();
	}

	void Start () 
	{
		base.DoStart ();

		bool cameFromFirstTimeScreen = gameController.loadingFirstTimeScene;
		gameController.loadingFirstTimeScene = false;

		titleCardMenu = titleCardObject.GetComponent<MM_TitleCard> ();
		homeMenu = homeMenuObject.GetComponent<MM_HomeMenu> ();
		levelSelectMenu = levelSelectMenuObject.GetComponent<MM_LevelSelectMenu> ();
		optionsMenu = optionsMenuObject.GetComponent<MM_OptionsMenu> ();


		UIStackManager.AddComponentToStack (titleCardObject);

		if (!cameFromFirstTimeScreen) 
		{
			UIStackManager.AddComponentToStack (homeMenuObject);
		} 


		gameController.ChangePlayerLives (Mathf.Max(3, gameController.playerLives), true);
	}


	public override void DoUpdate () 
	{
		base.DoUpdate ();
		base.UpdateUpdatables ();


	}


	public override void AddUpdatableToList (GameObject updatableObject)
	{
		base.AddUpdatableToList (updatableObject);
	}



	public override bool CanRemoveBottomMenu (GameObject menu)
	{
		return false;
	}


}
