using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ConfirmMenu : GenericMenu {


	public TextMeshProUGUI textObj;


	void Awake ()
	{
		base.DoAwake ();
	}

	void Start () 
	{
		base.DoStart ();
	}


	public override void DoUpdate()
	{

	}


	public override void TurnedOn(GameObject previousMenu)
	{
		if (previousMenu == null) 
		{
			selectedButton = defaultSelectedButton;
		}


		SetButtonSelected (selectedButton);

	}


	public override void SetMessage(string message)
	{
		textObj.SetText (message);
	}


	public void YesButtonPressed()
	{
		uiStackManager.GetConfirmationResponse (true, this.gameObject);
	}


	public void NoButtonPressed()
	{
		uiStackManager.GetConfirmationResponse (false, this.gameObject);
	}
	

}
