using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionSceneController : GeneralSceneController 
{

	public GameObject hudPrefab;
	GameObject hudObject;
	HUDController hudController;

	public DebugInfoManager debugInfoManager;
	public CollisionTestingManager collisionTestingManager;

	public GameObject pauseMenu;
	public GameObject pauseOptionsMenu;

	public List <PlayerActionsController> players = new List <PlayerActionsController> ();
	public List <CameraController> cameras = new List <CameraController> ();
	public List <MovingPlatformController> movingPlatforms = new List <MovingPlatformController>();

	public GameObject playerStartPos;
	public List <GameObject> checkpointsList;

	public int itemCount = 0;

	public Vector3 gravityDir = -Vector3.up;

	public enum DebugModeType
    {
		None, Player, Camera
    }
	public DebugModeType debugModeType;

	void Awake ()
	{
		base.DoAwake ();
	}

	void Start () 
	{
		base.DoStart ();

		//mainCamera = GameObject.FindGameObjectWithTag ("CameraController");
		//player = GameObject.FindGameObjectWithTag ("Player");

		//cameraController = cameraObject.GetComponent<CameraController> ();
		//playerController = playerObject.GetComponent<PlayerActionsController> ();

		gameController.loadingLevelScene = false;

		hudObject = Instantiate(hudPrefab, GameObject.Find("Canvas").transform);
		hudController = hudObject.GetComponent<HUDController>();
		hudController.DoStart ();

		elapsedGameTime = sceneRecord.savedElapsedGameTime;

		debugInfoManager = GetComponent<DebugInfoManager>();
		debugInfoManager.DoStart(this);

		collisionTestingManager = GetComponent<CollisionTestingManager>();
		collisionTestingManager.DoStart(this);
	}


	public override void DoUpdate () {

		base.DoUpdate ();

		if (!interactedWithAMenu) 
		{
			if (input.GetButtonDown(GeneralInput.AxesNames.Start)) 
			{
				if (!gamePaused && !textBoxManager.GetIsTextBoxActive()) 
				{
					Pause ();
				} 
			} 

		}


		if (gameController.debugMode && !(Input.GetKeyDown (KeyCode.Alpha0) || !Input.GetKey (KeyCode.Alpha9))) 
		{
			//advance normally or frame by frame?
			gameController.frameByFrame = true;

		} 
		else 
		{
			gameController.frameByFrame = false;
			base.UpdateUpdatables ();
		}

		if (gameController.debugMode)
        {
			if (input.GetButtonDown(GeneralInput.AxesNames.DpadHorizontal))
			{
				float rawInput = input.GetRawInput(GeneralInput.AxesNames.DpadHorizontal);

				if (rawInput > 0)
                {
					debugModeType = (DebugModeType) Mathf.Min ((int) debugModeType + 1, System.Enum.GetValues (typeof(DebugModeType)).Length - 1);
                }
				else
                {
					debugModeType = (DebugModeType)Mathf.Max((int)debugModeType - 1, 0);
				}
			}
		}
		else
        {
			debugModeType = DebugModeType.None;
        }


		if (!gamePaused && !interactedWithAMenu) 
		{
			hudController.DoUpdate (elapsedGameTime, itemCount, gameController.playerLives);
		}



		/*if (!gamePaused && !interactedWith) 
		{
			foreach (var movingPlatformController in movingPlatforms) 
			{
				movingPlatformController.DoUpdate ();
			}

			foreach (var playerController in players) 
			{
				playerController.DoUpdate ();
			}

			foreach (var cameraController in cameras) 
			{
				cameraController.DoUpdate ();
			}


		}*/

		collisionTestingManager.DoUpdate(gameController.debugMode);
		debugInfoManager.DoUpdate(gameController.debugMode);


		interactedWithAMenu = false;
	}


	public void Pause () 
	{
		interactedWithAMenu = true;
		gamePaused = true;

		PauseComponenetsInUpdatables();

		pauseMenu.transform.SetAsLastSibling(); //renders it in front of every other UI component in the hierachy level
		UIStackManager.AddComponentToStack (pauseMenu);

	}

	public void UnPause ()
	{
		interactedWithAMenu = true;
		gamePaused = false;

		UnPauseComponenetsInUpdatables();
	}

	public void PauseComponenetsInUpdatables ()
    {
		foreach (Updatable u in updatables)
        {
			u.PauseAnimator();
        }
    }

	public void UnPauseComponenetsInUpdatables()
	{
		foreach (Updatable u in updatables)
		{
			u.UnPauseAnimator();
		}
	}

	public void RestartScene () 
	{
		interactedWithAMenu = true;
		base.RestartScene ();
	}

	public void QuitScene ()
	{
		interactedWithAMenu = true;
		base.ChangeScene ("MainMenu");
	}


	public override bool CanRemoveBottomMenu (GameObject menu)
	{
		return true;
	}


	public override void AddUpdatableToList (GameObject updatableObject)
	{
		base.AddUpdatableToList (updatableObject);
	}

	public override void AddPhysicsSimulatableToList (GameObject physicsSimulatableObject)
	{
		base.AddPhysicsSimulatableToList (physicsSimulatableObject);
	}


	public void AddPlayerToList (GameObject player)
	{
		players.Add (player.GetComponent<PlayerActionsController> ());
	}

	public void AddCameraToList (GameObject camera)
	{
		cameras.Add (camera.GetComponent<CameraController> ());
	}

	public void AddMovingPlatformToList (GameObject movingPlatform)
	{
		movingPlatforms.Add (movingPlatform.GetComponent<MovingPlatformController> ());
	}


	public override Vector3 GetPlayerSpawnPos () 
	{
		if (checkpointsList.Count > 0) 
		{
			if (sceneRecord == null || checkpointsList[0] == null) 
			{
				return Vector3.zero;
			}
			return checkpointsList [sceneRecord.savedCheckpointIndex].transform.position;
		} 
		else 
		{
			return Vector3.zero;
		}
	}


	public void SetNewCheckPoint (GameObject checkpoint)
	{
		int index = -1;

		for (int i = 0; i < checkpointsList.Count; i++) 
		{
			if (GameObject.ReferenceEquals (checkpointsList [i], checkpoint)) 
			{
				index = i;
			} 
			else 
			{
				CheckpointController checkpointController = checkpointsList[i].GetComponent<CheckpointController>();

				if (checkpointController != null) //make sure its not a starting pos
				{
					checkpointController.DeactivateCheckpoint ();
				}
			}
		}

		if (index != -1) 
		{
			sceneRecord.savedCheckpointIndex = index;
			sceneRecord.savedElapsedGameTime = elapsedGameTime; //this is so that the timer will continue counting from the time it was when the checkpoint was activated
		} 
		else 
		{
			Debug.Log ("checkpoint not found in list");
		}
	}


	public GameObject GetCurrentActivatedCheckpoint ()
	{
		if (checkpointsList.Count > 0 && sceneRecord != null) 
		{
			return checkpointsList [sceneRecord.savedCheckpointIndex];
		} 
		else 
		{
			return null;
		}
	}


	public void AddCollectable ()
	{
		itemCount = Mathf.Min (itemCount+1, 999);
	}



	public void PlayerDied ()
	{
		if (gameController.playerLives == 0) 
		{
			QuitScene ();
		} 
		else 
		{
			RestartScene ();
		}

	}

	public CameraController GetMainCamera()
    {
		return cameras[0];
    }
}
