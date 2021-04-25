using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentSceneRecord
{
	public int savedCheckpointIndex;
	public float savedElapsedGameTime;

	public PersistentSceneRecord ()
	{
		savedCheckpointIndex = 0;
		savedElapsedGameTime = 0;
	}

}
