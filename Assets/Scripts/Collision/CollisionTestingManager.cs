using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionTestingManager : MonoBehaviour
{
    ActionSceneController sc;
    DebugInfoManager debugInfoManager;

    bool doSingleTest = true;

    public void DoStart(ActionSceneController sc)
    {
        this.sc = sc;
        this.debugInfoManager = sc.debugInfoManager;
    }

    public void DoUpdate(bool debugModeOn)
    {
        if (debugModeOn)
        {
            if (Input.GetKey (KeyCode.LeftControl) && Input.GetKeyDown (KeyCode.T))
            {
                // Do tests and display
                if (doSingleTest)
                {
                    List<CollisionSphereDebug.DebugInfoContainer> resultInfo = CollisionTests.RunSingleTest();
                    debugInfoManager.CreateTestResultDebug(debugModeOn, resultInfo);
                }
                else
                {
                    List<List<CollisionSphereDebug.DebugInfoContainer>> allResultsInfo = CollisionTests.RunAllTests();
                    debugInfoManager.CreateAllTestResultsDebug(debugModeOn, allResultsInfo);
                }
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                // Do tests without displaying
                if (doSingleTest)
                {
                    List<CollisionSphereDebug.DebugInfoContainer> resultInfo = CollisionTests.RunSingleTest();
                }
                else
                {
                    List<List<CollisionSphereDebug.DebugInfoContainer>> allResultsInfo = CollisionTests.RunAllTests();
                }
            }
        }
    }

    public CollisionSphereDebug.DebugInfoContainer CreateDebugSphereFromPlayerWithCollision (ActionSceneController sceneController, DebugInfoManager.PlayerDebugModeType debugType, Vector3 position, Vector3 transformUp, bool playerGrounded)
    {
        Vector3 velocity = Vector3.zero;
        Vector3 gravityDir = sc.gravityDir;

        CollisionSphereDebug.DebugInfoContainer debugInfo = new();

        switch (debugType)
        {
            case DebugInfoManager.PlayerDebugModeType.Grounding:
                debugInfo = CollisionTests.GroundingCollisionFromPlayer(position, transformUp, playerGrounded);
                break;
            case DebugInfoManager.PlayerDebugModeType.LowerGrounding:
                debugInfo = CollisionTests.LowerGroundingCollisionFromPlayer(position, transformUp, playerGrounded);
                break;
            case DebugInfoManager.PlayerDebugModeType.EdgeGrounding:
                debugInfo = CollisionTests.EdgeGroundingCollisionFromPlayer(position, transformUp, playerGrounded);
                break;
            case DebugInfoManager.PlayerDebugModeType.Wall:
                debugInfo = CollisionTests.WallCollisionFromPlayer(position, transformUp, playerGrounded);
                break;
            default:
                Debug.Log("Invalid player debug mode!");
                break;
        }

        return debugInfo;
    }
}
