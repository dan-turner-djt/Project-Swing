using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingSceneController : GeneralSceneController 
{
	public TextMeshProUGUI loadingSceneText;

	bool loadingALevel;
	string sceneBeingLoaded;

	void Awake ()
	{
		base.DoAwake ();
	}

	void Start ()
	{
		base.DoStart ();

		sceneBeingLoaded = gameController.sceneBeingLoaded;

		if (gameController.loadingLevelScene) 
		{
			loadingALevel = true;
		}

		SetLoadingSceneText ();
	}


	public override void DoUpdate () 
	{
		base.DoUpdate ();
		base.UpdateUpdatables ();
	}


	public override bool GetIsNextSceneLoaded ()
	{
		return false;
	}


	void SetLoadingSceneText ()
	{
		if (loadingSceneText != null) 
		{
			if (loadingALevel) 
			{
				loadingSceneText.SetText (sceneBeingLoaded);
			} 
			else 
			{
				loadingSceneText.SetText ("");
			}
		}


	}
}
