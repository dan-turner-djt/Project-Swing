using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GeneralSceneController : MonoBehaviour {

	public GameObject gameControllerPrefab;
	public GameController gameController;
	public PhysicsSimulator physicsSimulator;
	public PersistentSceneRecord sceneRecord;
	public TextBoxManager textBoxManager;

	protected GeneralInput input;
	protected UIStackManager UIStackManager;

	public List<Updatable> updatables = new List<Updatable>();
	public List<Updatable> lateUpdatables = new List<Updatable>();
	public List <PhysicsSimulatable> physicsSimulatables = new List <PhysicsSimulatable> ();

	public bool gamePaused = false;
	protected bool interactedWithAMenu = false;

	protected float elapsedGameTime = 0;

	public void DoAwake ()
	{
		input = gameObject.GetComponent<GeneralInput> ();
		UIStackManager = gameObject.GetComponent<UIStackManager> ();
		textBoxManager = gameObject.GetComponent<TextBoxManager>();


		//create a game controller if we dont find one already made

		GameObject[] gameControllerList = GameObject.FindGameObjectsWithTag ("GameController");

		if (gameControllerList.Length == 0) 
		{
			Instantiate (gameControllerPrefab);

		}

		gameController = GameObject.FindGameObjectWithTag ("GameController").GetComponent<GameController> ();



		if (gameController.sceneRecord == null) 
		{
			gameController.CreateNewSceneRecord ();
		}

		sceneRecord = gameController.sceneRecord;
	}

	public void DoStart ()
	{
		if (GetIsNextSceneLoaded ()) 
		{
			gameController.currentlyLoadingScene = false;
		}
	}



	public virtual void DoUpdate()
	{
		input.DoUpdate ();
		UIStackManager.DoUpdate ();

		if (!gamePaused && !interactedWithAMenu) 
		{
			elapsedGameTime += Time.deltaTime;
		}
	}


	public void UpdateUpdatables ()
	{
		if (!gamePaused && !interactedWithAMenu) 
		{
			foreach (var updatable in updatables) 
			{
				updatable.DoUpdate ();
			}

			/*foreach (var updatable in lateUpdatables) 
			{
				updatable.DoUpdate ();
			}*/

			if (physicsSimulator != null)
			{
				physicsSimulator.DoUpdate ();
			}



			foreach (var updatable in updatables) 
			{
				updatable.DoFinalUpdate ();
			}
		}
	}


	public void ChangeScene(string scene)
	{
		gameController.ChangeScene (scene);
	}

	public void RestartScene()
	{
		Scene current = SceneManager.GetActiveScene ();
		gameController.RestartScene (current.name);
	}


	public virtual bool CanRemoveBottomMenu (GameObject menu)
	{
		return true;
	}


	public virtual void AddUpdatableToList (GameObject updatableObject)
	{
		updatables.Add (updatableObject.GetComponent<Updatable>());
	}


	public virtual void AddLateUpdatableToList (GameObject updatableObject)
	{
		lateUpdatables.Add (updatableObject.GetComponent<Updatable>());
	}

	public virtual void AddPhysicsSimulatableToList (GameObject physicsSimulatableObject)
	{
		physicsSimulatables.Add (physicsSimulatableObject.GetComponent<PhysicsSimulatable>());
	}


	public virtual Vector3 GetPlayerSpawnPos ()
	{
		return Vector3.zero;
	}


	public virtual bool GetIsNextSceneLoaded ()
	{
		return true;
	}


	public void StartTextSequence (string sequenceName, bool controllable)
    {
		bool started = textBoxManager.StartTextSequence(sequenceName, controllable);
    }
}


