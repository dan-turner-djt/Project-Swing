using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CollisionTests
{
    static Vector3 gravityDir = Vector3.down;
    static float sphereRadius;
    static float maxRadiusMoveDivider;
    static float maxRadiusMove { get { return sphereRadius / maxRadiusMoveDivider; } }

    static List<Component> ignoreColliders = new List<Component>();
    static LayerMask collisionLayers;

    public static List<List<CollisionSphereDebug.DebugInfoContainer>> RunAllTests ()
    {
        List<List<CollisionSphereDebug.DebugInfoContainer>> allDebugInfo = new();

       // allDebugInfo.Add(TestGroundingTest());
       // allDebugInfo.Add(TestGroundingAndLowerTest());

        return allDebugInfo;
    }

    public static List<CollisionSphereDebug.DebugInfoContainer> RunSingleTest ()
    {
        List<CollisionSphereDebug.DebugInfoContainer> debugInfo = new();

        // Add whatever test you want to run here
        //debugInfo = TestGroundingTest();

        return debugInfo;
    }

    public static void SetInitialParameters (float _sphereRadius, float _maxRadiusMoveDivider, LayerMask _collisionLayers)
    {
        collisionLayers = _collisionLayers;
        sphereRadius = _sphereRadius;
        maxRadiusMoveDivider = _maxRadiusMoveDivider;
    }

    public static CollisionSphereDebug.DebugInfoContainer GroundingCollisionFromPlayer (Vector3 position, Vector3 transformUp, bool playerGrounded)
    {
        Vector3 velocity = Vector3.zero;

        CollisionSphereDebug.DebugInfoContainer debugInfo = CollisionController.Grounding(position, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, playerGrounded, new CollisionController.GroundingInfo(), true, true).debugInfo;
        return debugInfo;
    }

    public static CollisionSphereDebug.DebugInfoContainer LowerGroundingCollisionFromPlayer (Vector3 position, Vector3 transformUp, bool playerGrounded)
    {
        Vector3 velocity = Vector3.zero;
        Vector3 newOrigin = position - maxRadiusMove * transformUp;
        Vector3 staircaseNormal = transformUp;

        CollisionSphereDebug.DebugInfoContainer debugInfo = CollisionController.LowerGrounding(newOrigin, position, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, playerGrounded, CollisionController.LowerGroundingCheckType.Step, Vector3.zero, true).debugInfo;
        return debugInfo;
    }

    public static CollisionSphereDebug.DebugInfoContainer EdgeGroundingCollisionFromPlayer(Vector3 position, Vector3 transformUp, bool playerGrounded)
    {
        Vector3 velocity = Vector3.zero;
        Vector3 newOrigin = position - maxRadiusMove * transformUp;
        Vector3 staircaseNormal = transformUp;

        CollisionSphereDebug.DebugInfoContainer debugInfo = CollisionController.EdgeGrounding(position, position, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, playerGrounded, new CollisionController.GroundingInfo(), true, true).debugInfo;
        return debugInfo;
    }

    public static CollisionSphereDebug.DebugInfoContainer WallCollisionFromPlayer (Vector3 position, Vector3 transformUp, bool playerGrounded)
    {
        Vector3 velocity = Vector3.zero;

        CollisionSphereDebug.DebugInfoContainer debugInfo = CollisionController.WallCollision(true, position, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, playerGrounded, maxRadiusMove, true).debugInfo;
        return debugInfo;
    }

    // Start of tests //

    /*public static List<CollisionSphereDebug.DebugInfoContainer> TestGroundingTest()
    {
        Debug.Log("--TestGroundingTest--");
        bool testPassed = true;
        List<CollisionSphereDebug.DebugInfoContainer> debugInfo = new();

        // Set starting parameters
        Vector3 velocity = Vector3.zero;
        Vector3 transformUp = Vector3.up;
        Vector3 origin = new Vector3(25.17528f, 11.18433f, -2.057081f);

        CollisionController.GroundingInfo groundingInfo1 = CollisionController.Grounding(origin, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, true, new PlayerPhysicsController.GroundInfo(), true, true);
        CollisionSphereDebug.DebugInfoContainer debugInfo1 = groundingInfo1.debugInfo;
        debugInfo.Add(debugInfo1);

        if (groundingInfo1.collider == null)
        {
            Debug.Log("found nothing, failing");
            testPassed = false;
        }

        //CollisionController.GroundingInfo groundingInfo2 = CollisionController.Grounding(groundingInfo1.newPosition, groundingInfo1.groundNormal, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, groundingInfo1.grounded, true, true);
        //CollisionSphereDebug.DebugInfoContainer debugInfo2 = groundingInfo2.debugInfo;
        //debugInfo.Add(debugInfo2);

        if (!testPassed)
        {
            Debug.Log("Test failed!");
        }
        else
        {
            Debug.Log("Test passed!");
        }

        return debugInfo;
    }

    public static List<CollisionSphereDebug.DebugInfoContainer> TestGroundingAndLowerTest ()
    {
        Debug.Log("--TestGroundingAndLowerTest--");
        bool testPassed = true;
        List<CollisionSphereDebug.DebugInfoContainer> debugInfo = new();

        // Set starting parameters
        Vector3 velocity = Vector3.zero;
        Vector3 transformUp = Vector3.up;
        Vector3 origin = new Vector3(0, 0.9f, 2);
        Vector3 newOrigin = origin - maxRadiusMove * transformUp;

        CollisionController.GroundingInfo groundingInfo1 = CollisionController.LowerGrounding(newOrigin, origin, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, true, CollisionController.LowerGroundingCheckType.Convex, Vector3.zero, true, true);
        CollisionSphereDebug.DebugInfoContainer debugInfo1 = groundingInfo1.debugInfo;
        debugInfo.Add(debugInfo1);

        if (groundingInfo1.collider == null)
        {
            Debug.Log("found nothing, failing");
            testPassed = false;
        }

        if (!testPassed)
        {
            Debug.Log("Test failed!");
        }
        else
        {
            Debug.Log("Test passed!");
        }

        return debugInfo;
    }


    public static List<CollisionSphereDebug.DebugInfoContainer> TestGroundingAndLowerOnEdgeTest()
    {
        Debug.Log("--TestGroundingAndLowerOnEdgeTest--");
        bool testPassed = true;
        List<CollisionSphereDebug.DebugInfoContainer> debugInfo = new();

        // Set starting parameters
        Vector3 velocity = Vector3.zero;
        Vector3 transformUp = Vector3.up;
        Vector3 origin = new Vector3(28, 11.18f, -4.90f);
        

        CollisionController.GroundingInfo groundingInfo0 = CollisionController.Grounding(origin, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, true, new PlayerPhysicsController.GroundInfo(), true, true);
        CollisionSphereDebug.DebugInfoContainer debugInfo0 = groundingInfo0.debugInfo;
        debugInfo.Add(debugInfo0);

        if (groundingInfo0.collider != null)
        {
            Debug.Log("grounding found collider, failing");
            testPassed = false;
        }

        transformUp = groundingInfo0.groundNormal;
        Vector3 newOrigin = origin - maxRadiusMove * transformUp;
        

        CollisionController.GroundingInfo groundingInfo1 = CollisionController.LowerGrounding(newOrigin, origin, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, true, CollisionController.LowerGroundingCheckType.Convex, Vector3.zero, true, true);
        CollisionSphereDebug.DebugInfoContainer debugInfo1 = groundingInfo1.debugInfo;
        debugInfo.Add(debugInfo1);

        origin = groundingInfo1.newPosition;
        transformUp = groundingInfo1.groundNormal;

        CollisionController.GroundingInfo groundingInfo2 = CollisionController.Grounding(origin, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, groundingInfo1.grounded, new PlayerPhysicsController.GroundInfo(), true, true);
        CollisionSphereDebug.DebugInfoContainer debugInfo2 = groundingInfo2.debugInfo;
        debugInfo.Add(debugInfo2);

        if (groundingInfo2.collider == null)
        {
            Debug.Log("grounding 2 found nothing, failing");
            testPassed = false;
        }

        if (groundingInfo1.groundNormal.normalized != groundingInfo2.groundNormal.normalized)
        {
            Debug.Log("lowergrounding and grounding 2 found normals were different, failing");
            testPassed = false;
        }


        origin = groundingInfo2.newPosition;
        transformUp = groundingInfo2.groundNormal;

        CollisionController.GroundingInfo groundingInfo3 = CollisionController.Grounding(origin, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, groundingInfo1.grounded, new PlayerPhysicsController.GroundInfo(), true, true);
        CollisionSphereDebug.DebugInfoContainer debugInfo3 = groundingInfo2.debugInfo;
        debugInfo.Add(debugInfo3);


        if (groundingInfo3.collider == null)
        {
            Debug.Log("grounding 2 found nothing, failing");
            testPassed = false;
        }

        if (groundingInfo2.groundNormal.normalized != groundingInfo3.groundNormal.normalized)
        {
            Debug.Log("grounding 2 and grounding 3 found normals were different, failing");
            testPassed = false;
        }

        if (!testPassed)
        {
            Debug.Log("Test failed!");
        }
        else
        {
            Debug.Log("Test passed!");
        }

        return debugInfo;
    }


    public static List<CollisionSphereDebug.DebugInfoContainer> TestGroundingOnEdgeTest()
    {
        Debug.Log("--TestGroundingOnEdgeTest--");
        bool testPassed = true;
        List<CollisionSphereDebug.DebugInfoContainer> debugInfo = new();

        // Set starting parameters
        Vector3 velocity = Vector3.zero;
        Vector3 transformUp = Vector3.up;
        Vector3 origin = new Vector3(26, 11.18f, -4.90f);

        CollisionController.GroundingInfo groundingInfo1 = CollisionController.Grounding(origin, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, true, new PlayerPhysicsController.GroundInfo(), true, true);
        CollisionSphereDebug.DebugInfoContainer debugInfo1 = groundingInfo1.debugInfo;

        debugInfo.Add(debugInfo1);

        if (!testPassed)
        {
            Debug.Log("Test failed!");
        }
        else
        {
            Debug.Log("Test passed!");
        }

        return debugInfo;
    }*/

    // End of tests //
}
