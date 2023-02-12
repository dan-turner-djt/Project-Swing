using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {

	private static GameController instance;

	ResolutionManager resolutionManager;
	GeneralSceneController sceneController;
	GeneralInput input;

	public PersistentSceneRecord sceneRecord;

	public bool debugMode = false;

	public List<string> toggleList = new List<string> {"On", "Off"};
	public List<string> qualityList = new List<string> ();

	public List<string> levelList = new List<string> {"GeometryTest", "SwingTest", "ObjectsTest"};
	IDictionary<string, int> sceneBuildNumbers = new Dictionary<string, int> ()
	{
		{"FirstLoadingScene", 0},
		{"MainMenu", 1},
		{"LoadingScene", 2},
		{"GeometryTest", 3},
		{"SwingTest", 4},
		{"ObjectsTest", 5},
		{"DialogueTest", 6 }
	};

	public bool currentlyFullScreen;
	public bool currentlyLoadingScene;
	public bool loadingFirstTimeScene;
	public bool loadingLevelScene;
	public string sceneBeingLoaded;

	public bool frameByFrame = false;

	public int playerLives = 3;

	void Awake () 
	{
		DontDestroyOnLoad (this.gameObject);

		if (instance == null) 
		{
			instance = this;
		} 
		else 
		{
			Destroy (gameObject);
		}


		resolutionManager = GetComponent<ResolutionManager> ();


	}

	void Start () 
	{
		FillQualityList ();
		currentlyFullScreen = Screen.fullScreen;

		if (SceneManager.GetActiveScene().buildIndex == 0)
		{
			loadingFirstTimeScene = true;
			currentlyLoadingScene = true;

			int buildNumber;
			sceneBuildNumbers.TryGetValue ("MainMenu", out buildNumber);
			StartCoroutine (SceneAsync (buildNumber, 1f));
		}

		CheckAndFindSceneController ();
	}


	void CheckAndFindSceneController ()
	{
		if (sceneController == null && !loadingFirstTimeScene) 
		{
			sceneController = GameObject.FindGameObjectWithTag ("SceneController").GetComponent<GeneralSceneController> ();
		}
	}

	void Update () 
	{
		CheckAndFindSceneController();

		if (input == null)
        {
			input = sceneController.GetComponent<GeneralInput>();
		}

		// Turn on/off Debug Mode
		if (input.GetButtonDown(GeneralInput.AxesNames.DebugMode))
		{
			debugMode = !debugMode;
		}

		if (sceneController != null) 
		{
			sceneController.DoUpdate ();
		}
	}


	public void SetFullscreenOptions (bool fullScreenSelection)
	{
		currentlyFullScreen = fullScreenSelection;
		resolutionManager.SetFullscreen (fullScreenSelection);
	}

	public void SetResolutionOptions (bool fullScreenSelection, int resolutionSelectionIndex) 
	{
		resolutionManager.SetResolution (resolutionSelectionIndex, fullScreenSelection);
	}


	public int GetFullScreenIndex()
	{
		return (currentlyFullScreen)? 0 : 1;
	}
		

	public List<string> GetFullscreenResolutionsList() 
	{
		return resolutionManager.GetFullscreenResolutionsStringList ();
	}


	public List<string> GetCurrentResolutionsList ()
	{
		if (GetFullScreenIndex() == 0) //is fullscreen
		{
			return GetFullscreenResolutionsList ();
		} 
		else 
		{
			return GetWindowedResolutionsList ();
		}
	}

	public int GetCurrentResolutionIndex ()
	{
		if (GetFullScreenIndex() == 0) //is fullscreen
		{
			return GetCurrentFullscreenResIndex ();
		} 
		else 
		{
			return GetCurrentWindowedResIndex ();
		}
	}


	public List<string> GetWindowedResolutionsList() 
	{
		return resolutionManager.GetWindowedResolutionsStringList ();
	}


	public int GetCurrentFullscreenResIndex ()
	{
		return resolutionManager.GetCurrentFullscreenResIndex();
	}

	public int GetCurrentWindowedResIndex ()
	{
		return resolutionManager.GetCurrentWindowedResIndex();
	}


	public int GetCurrentQualityIndex ()
	{
		return QualitySettings.GetQualityLevel ();
	}


	void FillQualityList()
	{
		for (int i = 0; i < QualitySettings.names.Length; i++) 
		{
			qualityList.Add (QualitySettings.names [i]);
		}
	}

	public List<string> GetQualityList()
	{
		if (qualityList.Count == 0) 
		{
			FillQualityList ();
		}

		return qualityList;
	}


	public void SetQuality(int index)
	{
		QualitySettings.SetQualityLevel (index, true);
	}

	public void RestartScene(string scene)
	{
		SceneManager.LoadScene (scene);
		sceneController = null;
	}

	public void ChangeScene(string scene)
	{
		LoadNewScene (scene);
		sceneController = null;

		//when changing to a new scene delete the scene record
		sceneRecord = null;
	}

	void LoadNewScene (string scene)
	{
		SceneManager.LoadScene ("LoadingScene");
		int buildNumber;
		sceneBuildNumbers.TryGetValue (scene, out buildNumber);

		currentlyLoadingScene = true;
		sceneBeingLoaded = scene;
		float minWaitTime = 0.5f;


		if (levelList.Contains (scene)) 
		{
			//then it's a level scene
			minWaitTime = 1.8f;
			loadingLevelScene = true;
		}

		//loadingScene = true;
		StartCoroutine (SceneAsync (buildNumber, minWaitTime));
	}

	IEnumerator SceneAsync (int buildNumber, float minWaitTime)
	{
		yield return new WaitForSeconds (minWaitTime);

		AsyncOperation gameLevel = SceneManager.LoadSceneAsync (buildNumber);

		while (gameLevel.progress < 1) 
		{
			yield return new WaitForEndOfFrame ();
		}
	}


	public void CreateNewSceneRecord ()
	{
		sceneRecord = new PersistentSceneRecord ();
	}


	public void QuitGame()
	{
		currentlyLoadingScene = true;
		SceneManager.LoadScene ("LoadingScene");
		StartCoroutine (ExitApplication ());
	}

	public void RestartApplication()
	{
		currentlyLoadingScene = true;
		SceneManager.LoadScene ("LoadingScene");
		StartCoroutine (ExitAndRestartApplication());
	}

	IEnumerator ExitApplication ()
	{
		yield return new WaitForSeconds (0.5f);
		Application.Quit ();
	}

	IEnumerator ExitAndRestartApplication ()
	{
		yield return new WaitForSeconds (0.5f);
		System.Diagnostics.Process.Start(Application.dataPath.Replace("_Data", ".exe"));
		Application.Quit ();
	}

	public void ChangePlayerLives (int change, bool absoluteSet = false)
	{
		if (absoluteSet) 
		{
			playerLives = change;
			return;
		}


		playerLives += change;

		if (playerLives > 99) 
		{
			playerLives = 99;
		}


		if (playerLives < 0) 
		{
			playerLives = 0;
		}
	}
		
}
