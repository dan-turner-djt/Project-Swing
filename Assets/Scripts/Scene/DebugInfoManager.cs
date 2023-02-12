using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugInfoManager : MonoBehaviour
{
    public GameObject debugSphere;

    ActionSceneController sc;

    List<List<CollisionSphereDebug>> debugInfos = new();

    public enum PlayerDebugModeType
    {
        None, Grounding, LowerGrounding, EdgeGrounding, Wall
    }

    public int pointsDrawMode = 0;
    public int sphereDrawMode = 0;
    int sphereCreateMode = 1;


    public void DoStart(ActionSceneController sc)
    {
        this.sc = sc;
    }

    public void DoUpdate(bool debugModeOn)
    {
        if (sc.gameController.debugMode)
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // Change sphere draw mode
                if (Input.GetKeyDown(KeyCode.Equals))
                {
                    sphereDrawMode++;
                    Debug.Log("Sphere draw mode: " + sphereDrawMode);
                    DrawNewSpheres();
                }
                else if (Input.GetKeyDown(KeyCode.Minus))
                {
                    sphereDrawMode = Mathf.Max(0, sphereDrawMode - 1);
                    Debug.Log("Sphere draw mode: " + sphereDrawMode);
                    DrawNewSpheres();
                }
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                // Change sphere create mode
                if (Input.GetKeyDown(KeyCode.Equals))
                {
                    sphereCreateMode = Mathf.Min(4, sphereCreateMode + 1);
                    Debug.Log("Sphere create mode: " + sphereCreateMode);
                }
                else if (Input.GetKeyDown(KeyCode.Minus))
                {
                    // 1 because 0 is None
                    sphereCreateMode = Mathf.Max(1, sphereCreateMode - 1);
                    Debug.Log("Sphere create mode: " + sphereCreateMode);
                }
            }
            else
            {
                // Change points draw mode
                if (Input.GetKeyDown(KeyCode.Equals))
                {
                    pointsDrawMode++;
                }
                else if (Input.GetKeyDown(KeyCode.Minus))
                {
                    pointsDrawMode = Mathf.Max(0, pointsDrawMode - 1);
                }
            }

            
        }
        else
        {
            // Delete all debug spheres if debug mode is not on
            if (debugInfos.Count > 0)
            {
                foreach (List<CollisionSphereDebug> spheresList in debugInfos)
                {
                    foreach (CollisionSphereDebug sphere in spheresList)
                    {
                        GameObject sphereObject = sphere.gameObject;
                        Destroy(sphereObject);
                    }    
                }

                debugInfos = new();
            }
        }
        
    }

    public void CreateCollisionResultDebug (bool debugModeOn, CollisionSphereDebug.DebugInfoContainer debugInfo)
    {
        if (!debugModeOn || debugInfo.debugType == PlayerDebugModeType.None) return;

        GameObject newDebugSphere = Instantiate(debugSphere, Vector3.zero, Quaternion.identity);
        CollisionSphereDebug newDebugSphereInfo = newDebugSphere.GetComponent<CollisionSphereDebug>();
        List<CollisionSphereDebug> sphereList = new() { newDebugSphereInfo };
        debugInfos.Add(sphereList);
        newDebugSphereInfo.DoStart(sc, debugInfo, debugInfo.debugType);
        newDebugSphereInfo.ShowSphere();
    }

    public void CreateGroundingLoopResultDebug(bool debugModeOn, List<List<CollisionSphereDebug.DebugInfoContainer>> allDebugInfo)
    {
        if (!debugModeOn) return;

        List<CollisionSphereDebug> sphereList = new();

        foreach (List<CollisionSphereDebug.DebugInfoContainer> iterationInfo in allDebugInfo)
        {
            foreach (CollisionSphereDebug.DebugInfoContainer singleInfo in iterationInfo)
            {
                if (singleInfo.debugType == PlayerDebugModeType.None) continue;

                GameObject newDebugSphere = Instantiate(debugSphere, Vector3.zero, Quaternion.identity);
                CollisionSphereDebug newDebugSphereInfo = newDebugSphere.GetComponent<CollisionSphereDebug>();
                sphereList.Add(newDebugSphereInfo);
                newDebugSphereInfo.DoStart(sc, singleInfo, singleInfo.debugType);
                newDebugSphereInfo.ShowSphere();
            }
        }

        debugInfos.Add(sphereList);
    }

    public void CreateAllTestResultsDebug(bool debugModeOn, List<List<CollisionSphereDebug.DebugInfoContainer>> allResultsInfo)
    {
        if (!debugModeOn) return;

        foreach (List<CollisionSphereDebug.DebugInfoContainer> resultInfo in allResultsInfo)
        {
            CreateTestResultDebug(debugModeOn, resultInfo);
        }
    }


    public void CreateTestResultDebug (bool debugModeOn, List<CollisionSphereDebug.DebugInfoContainer> resultsInfo)
    {
        if (!debugModeOn) return;

        List<CollisionSphereDebug> sphereList = new();
        foreach (CollisionSphereDebug.DebugInfoContainer debugInfo in resultsInfo)
        {
            GameObject newDebugSphere = Instantiate(debugSphere, Vector3.zero, Quaternion.identity);
            CollisionSphereDebug newDebugSphereInfo = newDebugSphere.GetComponent<CollisionSphereDebug>();
            sphereList.Add(newDebugSphereInfo);
            newDebugSphereInfo.DoStart(sc, debugInfo, debugInfo.debugType);
            newDebugSphereInfo.ShowSphere();
        }

        debugInfos.Add(sphereList);
    }

    public void CreateManualDebugWithCollision (bool debugModeOn, Vector3 position, Vector3 transformUp, Quaternion rotation, bool isGrounded)
    {
        if (!debugModeOn || (PlayerDebugModeType)sphereCreateMode == PlayerDebugModeType.None) return;

        CollisionSphereDebug.DebugInfoContainer debugInfo = sc.collisionTestingManager.CreateDebugSphereFromPlayerWithCollision(sc, (PlayerDebugModeType)sphereCreateMode, position, transformUp, isGrounded);

        CreateCollisionResultDebug(debugModeOn, debugInfo);
    }

    void DrawNewSpheres ()
    {
        foreach (List<CollisionSphereDebug> spheresList in debugInfos)
        {
            if (sphereDrawMode < spheresList.Count)
            {
                foreach (CollisionSphereDebug sphere in spheresList)
                {
                    // Hide everything
                    sphere.HideSphere();
                }

                // Reset this to 0 because we could be changing sphere debug type and we don't want things to get messy
                pointsDrawMode = 0;

                // Show the corect one
                spheresList[sphereDrawMode].ShowSphere();
            }
        }
    }
}
