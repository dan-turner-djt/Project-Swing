using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour 
{
	public TextMeshProUGUI timerText;
	public TextMeshProUGUI itemCounterText;
	public TextMeshProUGUI livesCounterText;


	public void DoStart()
	{
		
	}



	public void DoUpdate (float timerTime, float itemCount, int livesCount)
	{
		float t = timerTime;

		float minutes = t / 60;
		string minutesTens = ((int)minutes / 10).ToString ();
		string minutesUnits = ((int)minutes % 10).ToString ();
		float seconds = t % 60;
		string secondsTens = ((int)seconds / 10).ToString ();
		string secondsUnits = ((int) seconds % 10).ToString ();
		float decimalSeconds = (t % 1) * 100;
		string decimalSecondsTens = ((int)decimalSeconds / 10).ToString ();
		string decimalSecondsUnits = ((int) decimalSeconds % 10).ToString ();

		timerText.SetText (minutesTens + "" + minutesUnits + ":" + secondsTens + "" + secondsUnits + "." + decimalSecondsTens + "" + decimalSecondsUnits);


		itemCounterText.SetText (itemCount.ToString());

		livesCounterText.SetText (livesCount.ToString ());
	}

}
