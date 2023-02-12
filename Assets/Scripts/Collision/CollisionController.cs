using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CollisionController
{
    const float maxStepHeightMod = 0.4f; //percent of the sphere radius
    const float extraCheckDistance = 0.005f;
    const float extraDepenetrationDistance = 0.005f;
    const float fallOffSlopeSpeedThreshold = 4f;

    public enum LowerGroundingCheckType
    {
        Convex, Step, Staircase
    }


    public static WallLoopInfo WallLoop (Vector3 origin, Vector3 transformUp, Vector3 velocity, Vector3 gravityDir, float sphereRadius, LayerMask collisionLayers, List<Component> ignoreColliders, bool playerGrounded, float maxRadiusMove, float wallAngleZeroAngle, bool debugModeOn)
    {
        WallLoopInfo wallLoopInfo = new();

        bool wallDepenetrated = false;
        int iterationsDone = 0;
        List<GroundCastInfo> allWallPoints = new();

        const int maxIterations = 10;
        for (int i = 0; i < maxIterations; i++)
        {
            iterationsDone++;

            CollisionController.WallInfo wallInfo = CollisionController.WallCollision(true, origin, transformUp, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, playerGrounded, maxRadiusMove, debugModeOn);
            origin = wallInfo.newPosition;
            if (wallInfo.depenetrated) wallDepenetrated = true;

            if (wallInfo.wallDepenPoints.Count > 0)
            {
                Vector3 localVelocity = ExtVector3.InverseTransformDirection(transformUp, velocity);
                PlayerPhysicsController.VelocityAgainstWallsNormalsInfo velocityWallsNormalsInfo = PlayerPhysicsController.SetWallsInfo(true, localVelocity, wallInfo.wallDepenPoints, transformUp, gravityDir);
                Vector3 newLocalVelocity = PlayerPhysicsController.LimitVelocityOnWalls(localVelocity, velocityWallsNormalsInfo, wallAngleZeroAngle, true);
                velocity = ExtVector3.TransformDirection(transformUp, newLocalVelocity);

                velocity = PlayerPhysicsController.LimitVelocityOnCeiling(velocity, gravityDir, wallInfo.wallDepenPoints);
                //collisionInfo.velocity = LimitVelocityOnStuckGround(collisionInfo.velocity, gravityDir, wallInfo.wallDepenPoints, wallInfo.depenetrationIterationsDone, groundInfo.GetIsGrounded());

                //Debug.Log("after wall: " + collisionInfo.velocity);

                allWallPoints.AddRange(wallInfo.wallDepenPoints);
            }

            if (!wallInfo.depenetrated) break;
        }

        //Debug.Log("loop iterations done: " + iterationsDone);

        wallLoopInfo.newPosition = origin;
        wallLoopInfo.newVelocity = velocity;
        wallLoopInfo.iterationsDone = iterationsDone;
        wallLoopInfo.depenetrated = wallDepenetrated;
        return wallLoopInfo;
    }

    public static WallInfo WallCollision (bool forCollision, Vector3 origin, Vector3 transformUp, Vector3 velocity, Vector3 gravityDir, float sphereRadius, LayerMask collisionLayers, List<Component> ignoreColliders, bool playerGrounded, float maxRadiusMove, bool debugModeOn = false)
    {
        WallInfo wallInfo = new WallInfo();

        List<GroundingSphereCollisionInfo> wallContactsBuffer = new List<GroundingSphereCollisionInfo>();
        float checkRadius;
        // Make the check radius bigger so we can collect normals that have been depenetrated from
        checkRadius = GetExtraCheckRadius(sphereRadius, extraDepenetrationDistance, true);

        GroundingSphereCollisionDetect.DetectSphereCollisions(origin, origin, checkRadius, collisionLayers, ignoreColliders, wallContactsBuffer, transformUp, 0, true, true);

        // Process points
        List<GroundCastInfo> processedPoints = new List<GroundCastInfo>();
        List<GroundCastInfo> walkableGroundPoints = new List<GroundCastInfo>();
        List<GroundCastInfo> wallPoints = new List<GroundCastInfo>();
        for (int i = 0; i < wallContactsBuffer.Count; i++)
        {
            GroundingSphereCollisionInfo collisionPoint = wallContactsBuffer[i];

            GroundingEdgeCollisionInfo normalsInfo = new GroundingEdgeCollisionInfo();
            Vector3 normal = collisionPoint.realNormal;

            if (collisionPoint.isOnEdge)
            {
                normalsInfo = GroundingSphereCollisionDetect.DetectEdgeCollisions(collisionPoint.collider, collisionPoint.closestPointOnSurface, 0.0001f, collisionPoint.realNormal, collisionPoint.interpolatedNormal, transformUp, gravityDir, velocity, 0);

                normal = normalsInfo.calculatedGroundNormal;
            }

            GroundCastInfo processedGround = new GroundCastInfo(origin, sphereRadius, collisionPoint.closestPointOnSurface, normal, normalsInfo, collisionPoint.collider, collisionPoint.isOnEdge);
            processedPoints.Add(processedGround);

            Vector3 maxStepPoint = GetMaxStepPoint(origin, transformUp, sphereRadius);
            CanWalkToSlopeInfo canWalkToSlopeInfo = CanWalkToSlope(processedGround.GetCalculatedGroundNormal(), transformUp, gravityDir, playerGrounded, processedGround.staircaseNormal);
            // These are special points which we want to use for velocity limiting but not depenetration (will be checked later)
            processedGround.wallActingAsFloor = canWalkToSlopeInfo.wallActingAsFloor;

            // Check for points below max step level
            if (!CheckPointIsAboveMaxStepPoint(processedGround.point, maxStepPoint, transformUp))
            {
                if (!playerGrounded && processedGround.edgeInfo.GetOnHardEdge())
                {
                    if (RaycastCheckValidStep (origin, processedGround, gravityDir, sphereRadius, collisionLayers))
                    {
                        // Valid step found
                        processedGround.walkable = true;
                        walkableGroundPoints.Add(processedGround);
                        continue;
                    }

                    // Not found a valid step so treat like a wall
                    processedGround.walkable = false;
                    // Set this so we dont stop velocity on these kind of edges to help slide over them better
                    processedGround.ignoreWallForVelocityLimiting = true;
                    wallPoints.Add(processedGround);
                    continue;

                }

                // Too short, treat like a step and ignore
                processedGround.walkable = true;
                walkableGroundPoints.Add(processedGround);
                continue;
            }

            if (canWalkToSlopeInfo.can && !processedGround.wallActingAsFloor)
            {
                if (processedGround.edgeInfo.GetOnHardEdge())
                {
                    processedGround.walkable = false;
                    wallPoints.Add(processedGround);
                    continue;
                }
                else
                {
                    processedGround.walkable = true;
                    walkableGroundPoints.Add(processedGround);
                    continue;
                }
            }

            processedGround.walkable = false;
            wallPoints.Add(processedGround);
        }

        // Set depenetration direction to use for each wall point
        List<GroundCastInfo> wallDepenPoints = new List<GroundCastInfo>();
        for (int i = 0; i < wallPoints.Count; i++)
        {
            GroundCastInfo info = wallPoints[i];
            Vector3 depenetrationNormal = info.GetInterpolatedNormal();

            if (playerGrounded)
            {
                depenetrationNormal = Vector3.ProjectOnPlane(depenetrationNormal, transformUp).normalized;
                if (depenetrationNormal == Vector3.zero) continue;
            }
            else
            {
                if (Vector3.Angle(depenetrationNormal, gravityDir) > 45)
                {
                    if (info.edgeInfo.GetOnHardEdge())
                    {
                        depenetrationNormal = Vector3.ProjectOnPlane(info.edgeInfo.calculatedEdgeNormal, gravityDir).normalized;
                        if (depenetrationNormal == Vector3.zero) continue;
                    }
                    else
                    {
                        depenetrationNormal = Vector3.ProjectOnPlane(depenetrationNormal, gravityDir).normalized;
                        if (depenetrationNormal == Vector3.zero) continue;
                    }

                }
                else
                {
                    Vector3 flattened = Vector3.ProjectOnPlane(depenetrationNormal, gravityDir);
                    depenetrationNormal = (depenetrationNormal - flattened).normalized;
                    if (depenetrationNormal == Vector3.zero) continue;
                }
            }

            info.wallDepenDir = depenetrationNormal;
            wallDepenPoints.Add(info);
        }

        if (!forCollision)
        {
            // Collecting normals for velocity limitting, return what we have now without doing any depenetration
            wallInfo.newPosition = origin;
            wallInfo.depenetrated = false;
            wallInfo.wallDepenPoints = wallDepenPoints;
            wallInfo.depenetrationIterationsDone = 0;

            wallInfo.debugInfo = WallSetDebugInfo(debugModeOn, origin, origin, transformUp, wallContactsBuffer, walkableGroundPoints, processedPoints, wallPoints, wallDepenPoints);
            return wallInfo;
        }

        // Do depenetration
        SphereCollisionDetect.WallDepenInfo depenInfo = SphereCollisionDetect.Depenetrate(ref wallDepenPoints, origin, sphereRadius, extraDepenetrationDistance, velocity, gravityDir, transformUp, playerGrounded, 100);
        Vector3 depenetration = Vector3.ClampMagnitude(depenInfo.totalDepenetration, maxRadiusMove); //We clamp to make sure we dont depenetrate too much into possibly unsafe areas **this may be risky but unconfirmed**

        Vector3 newOrigin = origin + depenetration;
        wallInfo.newPosition = newOrigin;
        wallInfo.depenetrated = depenInfo.depenetrated;
        wallInfo.wallDepenPoints = wallDepenPoints;
        wallInfo.depenetrationIterationsDone = depenInfo.iterationsDone;

        if (wallInfo.depenetrated) Debug.Log("wall depenetrated!");

        wallInfo.debugInfo = WallSetDebugInfo(debugModeOn, origin, newOrigin, transformUp, wallContactsBuffer, walkableGroundPoints, processedPoints, wallPoints, wallDepenPoints);
        return wallInfo;
    }


    public static CollisionSphereDebug.DebugInfoContainer WallSetDebugInfo(bool debugModeOn, Vector3 origin, Vector3 newOrigin, Vector3 transformUp, 
        List<GroundingSphereCollisionInfo> wallContactsBuffer, List<GroundCastInfo> walkableGroundPoints, List<GroundCastInfo> processedPoints, List<GroundCastInfo> wallPoints, List<GroundCastInfo> wallDepenPoints)
    {
        CollisionSphereDebug.DebugInfoContainer debugInfo = new();
        debugInfo.wallDebugInfo = new();

        if (debugModeOn)
        {
            // Create raw debug
            List<CollisionSphereDebug.DebugPointInfo> rawPointsDebugInfo = new();
            foreach (GroundingSphereCollisionInfo info in wallContactsBuffer)
            {
                rawPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.closestPointOnSurface, info.realNormal));
            }
            debugInfo.wallDebugInfo.rawSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, rawPointsDebugInfo);

            // Create processed debug
            List<CollisionSphereDebug.DebugPointInfo> processedPointsDebugInfo = new();
            foreach (GroundCastInfo info in processedPoints)
            {
                processedPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.wallDebugInfo.processedSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, processedPointsDebugInfo);

            // Create walkable debug
            List<CollisionSphereDebug.DebugPointInfo> walkablePointsDebugInfo = new();
            foreach (GroundCastInfo info in walkableGroundPoints)
            {
                walkablePointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.wallDebugInfo.walkableSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, walkablePointsDebugInfo);

            // Create raw wall debug
            List<CollisionSphereDebug.DebugPointInfo> wallPointsDebugInfo = new();
            foreach (GroundCastInfo info in wallPoints)
            {
                wallPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.wallDebugInfo.wallSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, wallPointsDebugInfo);

            // Create depen wall debug
            List<CollisionSphereDebug.DebugPointInfo> wallDepenPointsDebugInfo = new();
            foreach (GroundCastInfo info in wallDepenPoints)
            {
                wallDepenPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.wallDepenDir));
            }
            debugInfo.wallDebugInfo.wallDepenSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, wallDepenPointsDebugInfo);

            // Create depentrated debug
            List<CollisionSphereDebug.DebugPointInfo> depenPoint = new List<CollisionSphereDebug.DebugPointInfo>();
            debugInfo.wallDebugInfo.depenSphereInfo = new CollisionSphereDebug.DebugDrawInfo(newOrigin, transformUp, depenPoint);

            debugInfo.debugType = DebugInfoManager.PlayerDebugModeType.Wall;
        }
        else
        {
            debugInfo.debugType = DebugInfoManager.PlayerDebugModeType.None;
        }

        return debugInfo;
    }


    public static GroundingLoopInfo GroundingLoop (PlayerPhysicsController.GroundInfo startingInfo, Vector3 origin, Vector3 velocity, Vector3 gravityDir, float sphereRadius, float maxRadiusMove, LayerMask collisionLayers, List<Component> ignoreColliders, bool debugMode, bool ignoreSlopePhysics = false)
    {
        // Set infoToKeep to be last groundInfo and run with it from there
        GroundingInfo continuousInfo = new ();
        continuousInfo.grounded = startingInfo.GetIsGrounded();
        continuousInfo.groundNormal = startingInfo.up;
        continuousInfo.collider = startingInfo.collider;
        continuousInfo.staircaseNormal = startingInfo.staircaseNormal;

        List<List<CollisionSphereDebug.DebugInfoContainer>> allDebugInfo = new();

        bool groundDepenetrated = false;
        const int maxIterations = 10;
        int iterationsDone = 0;
        for (int i = 0; i < maxIterations; i++)
        {
            iterationsDone++;
            List<CollisionSphereDebug.DebugInfoContainer> iterationDebugInfo = new();

            bool groundSuccessful = false;

            // Do grounding once always
            CollisionController.GroundingInfo groundingInfo = CollisionController.Grounding(origin, continuousInfo.groundNormal, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, continuousInfo.grounded, continuousInfo, i == 0, debugMode);
            iterationDebugInfo.Add(groundingInfo.debugInfo);
            groundSuccessful = groundingInfo.depenetrated;
            // Stop going further is used to prevent continuing to check when the grounding was just the final check to confirm nothing new was found since last time
            bool stopGoingFurther = continuousInfo.depenetrated && !groundSuccessful;

            // Start checking for lower ground if we didn't find anything and are already grounded
            if (continuousInfo.grounded && !groundSuccessful && !stopGoingFurther)
            {
                // First do a shallower check for convex slopes
                Vector3 newOrigin = origin - 0.1f * continuousInfo.groundNormal;
                groundingInfo = CollisionController.LowerGrounding(newOrigin, origin, continuousInfo.groundNormal, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, continuousInfo.grounded, CollisionController.LowerGroundingCheckType.Convex, Vector3.zero, debugMode);
                iterationDebugInfo.Add(groundingInfo.debugInfo);
                groundSuccessful = groundingInfo.depenetrated;

                if (!groundSuccessful)
                {
                    // If nothing found, do a deeper check for steps
                    // This is quite deep in order to suck to steep staircases properly too
                    newOrigin = origin - maxRadiusMove * continuousInfo.groundNormal;
                    groundingInfo = CollisionController.LowerGrounding(newOrigin, origin, continuousInfo.groundNormal, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, continuousInfo.grounded, CollisionController.LowerGroundingCheckType.Step, Vector3.zero, debugMode);
                    iterationDebugInfo.Add(groundingInfo.debugInfo);
                    groundSuccessful = groundingInfo.depenetrated;

                    //Debug.Log("step check");
                    if (groundingInfo.depenetrated)
                    {
                        Debug.Log("step check depenetrated");
                    }


                    // If nothing found and on a staircase, check deeper to pick up a steep staircase
                    if (continuousInfo.staircaseNormal != Vector3.zero && !groundSuccessful)
                    {
                        newOrigin = origin - 0.5f * continuousInfo.groundNormal;
                        groundingInfo = CollisionController.LowerGrounding(newOrigin, origin, continuousInfo.groundNormal, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, continuousInfo.grounded, CollisionController.LowerGroundingCheckType.Staircase, continuousInfo.staircaseNormal, debugMode);
                        iterationDebugInfo.Add(groundingInfo.debugInfo);
                        groundSuccessful = groundingInfo.depenetrated;

                        if (groundingInfo.depenetrated)
                        {
                            Debug.Log("staircase depenetrated");
                        }

                        /*float staircaseMaxChecks = 5;
                        float counter = 0;
                        for (int i = 0; counter < staircaseMaxChecks; counter++)
                        {
                            newOrigin = origin - maxRadiusMove * transformUp - counter * ((3 - maxRadiusMove) / staircaseMaxChecks) * transformUp;
                            groundingInfo = CollisionController.LowerGrounding(newOrigin, origin, transformUp, collisionInfo.velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, groundInfo.GetIsGrounded(), CollisionController.LowerGroundingCheckType.Staircase, groundInfo.staircaseNormal, sc.gameController.debugMode);

                            if (groundingInfo.collider != null)
                            {
                                break;
                            }
                        }

                        Debug.Log(counter);*/

                        if (!groundingInfo.depenetrated)
                        {
                            
                        }
                    }
                }
            }

            if (!groundSuccessful && !stopGoingFurther)
            {
                // Do special edge grounding
                Debug.Log("doing on edge grounding!");

                groundingInfo = CollisionController.EdgeGrounding(origin, origin, continuousInfo.groundNormal, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, continuousInfo.grounded, continuousInfo, i == 0, debugMode);
                iterationDebugInfo.Add(groundingInfo.debugInfo);
                groundSuccessful = groundingInfo.depenetrated;

                stopGoingFurther = continuousInfo.depenetrated && !groundSuccessful;

                if (continuousInfo.grounded && !groundSuccessful && !stopGoingFurther)
                {
                    Vector3 newOrigin = origin - 2 * extraDepenetrationDistance * continuousInfo.groundNormal;
                    groundingInfo = CollisionController.EdgeGrounding(newOrigin, origin, continuousInfo.groundNormal, velocity, gravityDir, sphereRadius, collisionLayers, ignoreColliders, continuousInfo.grounded, continuousInfo, i == 0, debugMode);
                    iterationDebugInfo.Add(groundingInfo.debugInfo);
                    groundSuccessful = groundingInfo.depenetrated;
                }
            }

            // Set new info using whatever grounding info we got
            bool previouslyGrounded = continuousInfo.grounded;
            Vector3 previousGroundNormal = continuousInfo.groundNormal;
            continuousInfo = groundingInfo;
            origin = groundingInfo.newPosition;
            groundDepenetrated = groundingInfo.depenetrated;

            allDebugInfo.Add(iterationDebugInfo);

            if (groundDepenetrated)
            {
                velocity = ModifyVelocityAfterCollision(velocity, gravityDir, continuousInfo, previouslyGrounded, previousGroundNormal, ignoreSlopePhysics);
            }
            else
            {
                break;
            }
        }

        Debug.Log("grounding loop iterations done: " + iterationsDone);

        GroundingLoopInfo groundingLoopInfo = new ();
        groundingLoopInfo.groundingInfo = continuousInfo;
        groundingLoopInfo.newVelocity = velocity;
        groundingLoopInfo.allDebugInfo = allDebugInfo;
        groundingLoopInfo.iterationsDone = iterationsDone;

        return groundingLoopInfo;
    }

    public static GroundingInfo Grounding(Vector3 origin, Vector3 transformUp, Vector3 velocity, Vector3 gravityDir, float sphereRadius, LayerMask collisionLayers, List<Component> ignoreColliders, bool playerGrounded, GroundingInfo previousGroundInfo, bool firstCheck, bool debugModeOn = false)
    {
        GroundingInfo groundingInfo = new GroundingInfo();
        float checkRadius = sphereRadius;

        // Get collision points
        List<GroundingSphereCollisionInfo> groundContactsBuffer = new List<GroundingSphereCollisionInfo>();
        GroundingSphereCollisionDetect.DetectSphereCollisions(origin, origin, checkRadius, collisionLayers, ignoreColliders, groundContactsBuffer, transformUp, 0, true, true);

        // Process points
        List<GroundCastInfo> walkableGroundPoints = new List<GroundCastInfo>();
        List<GroundCastInfo> nonWalkableGroundPoints = new List<GroundCastInfo>();
        List<GroundCastInfo> processedPoints = new List<GroundCastInfo>();
        for (int i = 0; i < groundContactsBuffer.Count; i++)
        {
            GroundingSphereCollisionInfo collisionPoint = groundContactsBuffer[i];

            GroundingEdgeCollisionInfo normalsInfo = new GroundingEdgeCollisionInfo();
            Vector3 normal = collisionPoint.interpolatedNormal;

            if (collisionPoint.isOnEdge)
            {
                normalsInfo = GroundingSphereCollisionDetect.DetectEdgeCollisions(collisionPoint.collider, collisionPoint.closestPointOnSurface, 0.0001f, collisionPoint.realNormal, collisionPoint.interpolatedNormal, transformUp, gravityDir, velocity, 0);
                normal = normalsInfo.calculatedGroundNormal;
            }

            GroundCastInfo processedGround = new GroundCastInfo(origin, sphereRadius, collisionPoint.closestPointOnSurface, normal, normalsInfo, collisionPoint.collider, collisionPoint.isOnEdge);
            processedPoints.Add(processedGround);

            if (CanWalkToSlope(processedGround.GetCalculatedGroundNormal(), transformUp, gravityDir, playerGrounded, processedGround.staircaseNormal).can)
            {
                processedGround.walkable = true;
                walkableGroundPoints.Add(processedGround);
            }
            else
            {
                processedGround.walkable = false;
                nonWalkableGroundPoints.Add(processedGround);
            }
        }

        // Find sphere points
        List<GroundCastInfo> slopePoints = new List<GroundCastInfo>();
        foreach (GroundCastInfo info in walkableGroundPoints)
        {
            float distanceToLine = (info.point - Geometry.ClosestPointOnLineToPoint(info.point, origin, transformUp)).magnitude;

            if (!info.edgeInfo.GetOnHardEdge())
            {
                float angle = Vector3.Angle((origin - info.point).normalized, info.GetCalculatedGroundNormal());
                if (info.staircaseNormal == Vector3.zero && angle > 10f) 
                {
                    // Interp and real are too unaligned to be a properly contacted concave slope, ignore
                    //Debug.Log("too disaligned: " + angle);
                    continue;
                }

                // Don't accept points which are behind a wall point
                bool foundBlocking = false;
                /*foreach (GroundCastInfo otherInfo in nonWalkableGroundPoints)
                {
                    MPlane plane = new MPlane(otherInfo.GetCalculatedGroundNormal(), otherInfo.point, false);
                    if (plane.IsBehindPlane(info.point))
                    {
                        foundBlocking = true;
                        break;
                    }
                }*/

                if (!foundBlocking)
                {
                    slopePoints.Add(info);
                    continue;
                }
            }
        }
 
        // Choose best direct point
        /*GroundCastInfo bestDirectGroundPoint = new GroundCastInfo();
        if (directGroundPoints.Count > 0)
        {
            //Debug.Log("found direct");

            Vector3 highestDirectPoint = float.MinValue * transformUp;
            Vector3 maxStepPoint = GetMaxStepPoint(origin, transformUp, sphereRadius);
            foreach (GroundCastInfo info in directGroundPoints)
            {
                if (playerGrounded)
                {
                    if (CheckPointIsAboveMaxStepPoint (info.point, maxStepPoint, transformUp))
                    {
                        // Above max step height
                        continue;
                    }
                }

                if (ExtVector3.MagnitudeInDirection((info.point - highestDirectPoint), transformUp, false) > 0)
                {
                    highestDirectPoint = info.point;
                    bestDirectGroundPoint = info;
                }
            }

            if (bestDirectGroundPoint.hasHit)
            {
                concaveSlopePoints.Add(bestDirectGroundPoint);
            }
        }
        else if (playerGrounded)
        {
           // Debug.Log("not found direct");
        }*/

        

        // Choose best point
        GroundCastInfo best = ChooseBestGroundingGround (slopePoints, velocity, gravityDir, origin, playerGrounded, transformUp);
        bool foundBest = best.hasHit;

        //Debug.Log("transformUp: " + transformUp + ", normal: " + best.GetCalculatedGroundNormal());
        
        // Depenetrate
        Vector3 newOrigin = origin;
        Vector3 newTransformUp = transformUp;

        // If it IS the first check then continue to set not grounded
        if (!firstCheck && !foundBest)
        {
            // We found nothing which means we are already depenetrated properly, so just return the info for the previous grounding instead of setting grounding empty
            groundingInfo.depenetrated = false;
            groundingInfo.grounded = true;
            groundingInfo.newPosition = origin;
            groundingInfo.groundNormal = transformUp;
            groundingInfo.collider = previousGroundInfo.collider;
            groundingInfo.staircaseNormal = previousGroundInfo.staircaseNormal;

            groundingInfo.debugInfo = GroundingSetDebugInfo(debugModeOn, origin, newOrigin, transformUp, newTransformUp, false, groundContactsBuffer, walkableGroundPoints, processedPoints, slopePoints, new GroundCastInfo());
            return groundingInfo;
        }

        if (foundBest)
        {
            Vector3 depenDir = best.GetCalculatedGroundNormal();
            Vector3 extraDepenDir = best.GetCalculatedGroundNormal();

            bool isStaircase = false;
            if (best.staircaseNormal != Vector3.zero)
            {
                isStaircase = true;
                depenDir = -gravityDir;
                extraDepenDir = -gravityDir;
            }

            if (!playerGrounded || isStaircase)
            {
                depenDir = -gravityDir;
            }

            Vector3 depenetration = (Geometry.DepenetrateSphereFromPlaneInDirection(origin, sphereRadius, depenDir, best.point, best.GetCalculatedGroundNormal()).distance) * depenDir;
            depenetration += +extraDepenetrationDistance * extraDepenDir;
            newOrigin += depenetration;
            groundingInfo.depenetrated = true;

            newTransformUp = isStaircase ? -gravityDir : best.GetCalculatedGroundNormal();
            groundingInfo.grounded = true;
            groundingInfo.newPosition = newOrigin;
            groundingInfo.groundNormal = newTransformUp;
            groundingInfo.collider = best.collider;
            groundingInfo.staircaseNormal = isStaircase ? best.GetCalculatedGroundNormal() : Vector3.zero;
        }
        else
        {
            groundingInfo.grounded = false;
            groundingInfo.newPosition = origin;
            groundingInfo.groundNormal = -gravityDir;
        }

        groundingInfo.debugInfo = GroundingSetDebugInfo(debugModeOn, origin, newOrigin, transformUp, newTransformUp, foundBest, groundContactsBuffer, walkableGroundPoints, processedPoints, slopePoints, best);
        return groundingInfo;
    }

    public static CollisionSphereDebug.DebugInfoContainer GroundingSetDebugInfo (bool debugModeOn, Vector3 origin, Vector3 newOrigin, Vector3 transformUp, Vector3 newTransformUp, bool foundBest,
        List<GroundingSphereCollisionInfo> groundContactsBuffer, List<GroundCastInfo> walkableGroundPoints, List<GroundCastInfo> processedPoints, List<GroundCastInfo> slopePoints, GroundCastInfo best)
    {
        CollisionSphereDebug.DebugInfoContainer debugInfo = new ();
        debugInfo.groundingDebugInfo = new ();

        if (debugModeOn)
        {
            // Create raw debug
            List<CollisionSphereDebug.DebugPointInfo> rawPointsDebugInfo = new();
            foreach (GroundingSphereCollisionInfo info in groundContactsBuffer)
            {
                rawPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.closestPointOnSurface, info.realNormal));
            }
            debugInfo.groundingDebugInfo.rawSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, rawPointsDebugInfo);

            // Create processed debug
            List<CollisionSphereDebug.DebugPointInfo> processedPointsDebugInfo = new();
            foreach (GroundCastInfo info in processedPoints)
            {
                processedPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.groundingDebugInfo.processedSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, processedPointsDebugInfo);

            // Create walkable debug
            List<CollisionSphereDebug.DebugPointInfo> walkablePointsDebugInfo = new();
            foreach (GroundCastInfo info in walkableGroundPoints)
            {
                walkablePointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.groundingDebugInfo.walkableSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, walkablePointsDebugInfo);

            // Create concave slopes debug
            List<CollisionSphereDebug.DebugPointInfo> slopePointsDebugInfo = new();
            foreach (GroundCastInfo info in slopePoints)
            {
                slopePointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.groundingDebugInfo.slopesSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, slopePointsDebugInfo);

            // Create best debug
            List<CollisionSphereDebug.DebugPointInfo> bestGroundPoint = new List<CollisionSphereDebug.DebugPointInfo>();
            if (foundBest)
            {
                CollisionSphereDebug.DebugPointInfo bestPointDebugInfo = new CollisionSphereDebug.DebugPointInfo(best.point, best.GetCalculatedGroundNormal());
                bestGroundPoint.Add(bestPointDebugInfo);
            }
            debugInfo.groundingDebugInfo.bestSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, bestGroundPoint);

            // Create depentrated debug
            List<CollisionSphereDebug.DebugPointInfo> depenPoint = new List<CollisionSphereDebug.DebugPointInfo>();
            debugInfo.groundingDebugInfo.depenSphereInfo = new CollisionSphereDebug.DebugDrawInfo(newOrigin, newTransformUp, depenPoint);

            debugInfo.debugType = DebugInfoManager.PlayerDebugModeType.Grounding;
        }
        else
        {
            debugInfo.debugType = DebugInfoManager.PlayerDebugModeType.None;
        }

        return debugInfo;
    }

    public static GroundingInfo LowerGrounding (Vector3 origin, Vector3 originalOrigin, Vector3 transformUp, Vector3 velocity, Vector3 gravityDir, float sphereRadius, LayerMask collisionLayers, List<Component> ignoreColliders, bool playerGrounded, LowerGroundingCheckType checkType, Vector3 staircaseNormal, bool debugModeOn = false, int iteration = 0)
    {
        GroundingInfo groundingInfo = new GroundingInfo();
        float maxStepHeight = sphereRadius * maxStepHeightMod;

        if (checkType == LowerGroundingCheckType.Staircase && staircaseNormal == Vector3.zero)
        {
            Debug.Log("not already on a staircase");

            // We need a valid staircaseNormal to check, set empty and return
            groundingInfo.grounded = false;
            groundingInfo.newPosition = originalOrigin;
            groundingInfo.groundNormal = -gravityDir;

            groundingInfo.debugInfo = LowerGroundingSetDebugInfo(debugModeOn, origin, origin, transformUp, transformUp, false, new List<GroundingSphereCollisionInfo>(), new List<GroundCastInfo>(), new List<GroundCastInfo>(), new List<GroundCastInfo>(), new GroundCastInfo());
            return groundingInfo;
        }

        // Get collision points
        List<GroundingSphereCollisionInfo> groundContactsBuffer = new List<GroundingSphereCollisionInfo>();
        float checkRadius = sphereRadius;

        GroundingSphereCollisionDetect.DetectSphereCollisions(origin, origin, checkRadius, collisionLayers, ignoreColliders, groundContactsBuffer, transformUp, 0, true, true);

        // Process points
        List<GroundCastInfo> processedPoints = new List<GroundCastInfo>();
        List<GroundCastInfo> walkableGroundPoints = new List<GroundCastInfo>();
        List<GroundCastInfo> nonWalkableGroundPoints = new List<GroundCastInfo>();
        for (int i = 0; i < groundContactsBuffer.Count; i++)
        {
            GroundingSphereCollisionInfo collisionPoint = groundContactsBuffer[i];

            GroundingEdgeCollisionInfo normalsInfo = new GroundingEdgeCollisionInfo();
            Vector3 normal = collisionPoint.interpolatedNormal;

            if (collisionPoint.isOnEdge)
            {
                normalsInfo = GroundingSphereCollisionDetect.DetectEdgeCollisions(collisionPoint.collider, collisionPoint.closestPointOnSurface, 0.0001f, collisionPoint.realNormal, collisionPoint.interpolatedNormal, transformUp, gravityDir, velocity, 0);
                normal = normalsInfo.calculatedGroundNormal;
            }

            GroundCastInfo processedGround = new GroundCastInfo(origin, sphereRadius, collisionPoint.closestPointOnSurface, normal, normalsInfo, collisionPoint.collider, collisionPoint.isOnEdge);
            processedPoints.Add(processedGround);

            if (CanWalkToSlope(processedGround.GetCalculatedGroundNormal(), transformUp, gravityDir, playerGrounded, processedGround.staircaseNormal).can)
            {
                processedGround.walkable = true;
                walkableGroundPoints.Add(processedGround);
            }
            else
            {
                processedGround.walkable = false;
                nonWalkableGroundPoints.Add(processedGround);
            }
        }

        // Find direct ground points and concave slope points
        List<GroundCastInfo> slopePoints = new List<GroundCastInfo>();
        foreach (GroundCastInfo info in walkableGroundPoints)
        {
            if (!info.edgeInfo.GetOnHardEdge())
            {
                if (CanWalkOnConvexSlope (info.normal, transformUp, gravityDir, info.staircaseNormal))
                {
                    if (checkType == LowerGroundingCheckType.Convex)
                    {
                        if (playerGrounded && ExtVector3.MagnitudeInDirection(info.point - (originalOrigin - (sphereRadius + extraDepenetrationDistance) * transformUp), transformUp, false) >= -0.0001f
                            && info.GetCalculatedGroundNormal() == transformUp)
                        {
                            // Above original ground level and normal same as currently (not really convex)
                            //continue;
                        }
                    }
                    
                    if (checkType == LowerGroundingCheckType.Step)
                    {
                        Vector3 minStepPoint = GetMinStepPoint(originalOrigin, transformUp, sphereRadius);
                        if (CheckPointIsBelowMinStepPoint(info.point, minStepPoint, transformUp))
                        {
                            // Below min step height
                            continue;
                        }

                        if (playerGrounded && ExtVector3.MagnitudeInDirection(info.point - (originalOrigin - (sphereRadius + extraDepenetrationDistance) * transformUp), transformUp, false) >= -0.0001f)
                        {
                            //Point is too high to be taken as a lower step now
                            //continue;
                        }
                    }
                    
                    if (checkType == LowerGroundingCheckType.Staircase)
                    {
                        if (Vector3.Angle (info.staircaseNormal, staircaseNormal) > 0.0001f)
                        {
                            // We only want to take it if its the same staircase (if the normal is the same, it /should/ be)
                            Debug.Log("different staircase normal");
                            continue;
                        }
                    }

                    slopePoints.Add(info);
                }

            }
        }

        // Choose best point
        GroundCastInfo best = new GroundCastInfo();
        bool foundBest = false;
        if (slopePoints.Count > 0)
        {
            // Choose best concave slope point
            float smallestDistance = float.MaxValue;
            foreach (GroundCastInfo info in slopePoints)
            {
                // Choose the point closest to the sphere centre
                float distance = (origin - info.point).magnitude;

                if (distance < smallestDistance)
                {
                    smallestDistance = distance;
                    best = info;
                    foundBest = true;
                }
                else if (foundBest && distance == smallestDistance)
                {
                    // Just case, use closest to gravity flat ground if distance is the same
                    float myAngleDiff = Vector3.Angle(-gravityDir, info.GetCalculatedGroundNormal());
                    float theirAngleDiff = Vector3.Angle(-gravityDir, best.GetCalculatedGroundNormal());

                    if (myAngleDiff < theirAngleDiff)
                    {
                        smallestDistance = distance;
                        best = info;
                        foundBest = true;
                    }
                }
            }
        }

        // Depenetrate
        Vector3 newOrigin = origin;
        Vector3 newTransformUp = transformUp;
        if (foundBest)
        {
            Vector3 depenDir = best.GetCalculatedGroundNormal();
            Vector3 extraDepenDir = best.GetCalculatedGroundNormal();

            bool isStaircase = false;
            if (best.staircaseNormal != Vector3.zero)
            {
                Debug.Log("lower staircase");
                isStaircase = true;
                depenDir = -gravityDir;
                extraDepenDir = -gravityDir;
            }

            Vector3 normal = best.GetCalculatedGroundNormal();
            Vector3 depenetration = (Geometry.DepenetrateSphereFromPlaneInDirection(origin, sphereRadius, depenDir, best.point, best.GetCalculatedGroundNormal()).distance) * depenDir;
            newOrigin += depenetration;
            newOrigin += extraDepenetrationDistance * extraDepenDir;
            groundingInfo.depenetrated = true;
            groundingInfo.newPosition = newOrigin;

            newTransformUp = isStaircase ? -gravityDir : best.GetCalculatedGroundNormal();
            groundingInfo.grounded = true;
            groundingInfo.groundNormal = newTransformUp;
            groundingInfo.collider = best.collider;
            groundingInfo.staircaseNormal = isStaircase ? best.GetCalculatedGroundNormal() : Vector3.zero;
        }
        else
        {
            // Set empty
            groundingInfo.grounded = false;
            groundingInfo.newPosition = originalOrigin;
            groundingInfo.groundNormal = -gravityDir;

            //Debug.Log("not found best, " + checkType);
        }

        groundingInfo.debugInfo = LowerGroundingSetDebugInfo(debugModeOn, origin, newOrigin, transformUp, newTransformUp, foundBest, groundContactsBuffer, walkableGroundPoints, processedPoints, slopePoints, best);
        return groundingInfo;
    }

    public static CollisionSphereDebug.DebugInfoContainer LowerGroundingSetDebugInfo(bool debugModeOn, Vector3 origin, Vector3 newOrigin, Vector3 transformUp, Vector3 newTransformUp, bool foundBest,
        List<GroundingSphereCollisionInfo> groundContactsBuffer, List<GroundCastInfo> walkableGroundPoints, List<GroundCastInfo> processedPoints, List<GroundCastInfo> slopePoints, GroundCastInfo best)
    {
        CollisionSphereDebug.DebugInfoContainer debugInfo = new();
        debugInfo.lowerGroundingConvexDebugInfo = new();

        if (debugModeOn)
        {
            // Create raw debug
            List<CollisionSphereDebug.DebugPointInfo> rawPointsDebugInfo = new();
            foreach (GroundingSphereCollisionInfo info in groundContactsBuffer)
            {
                rawPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.closestPointOnSurface, info.realNormal));
            }
            debugInfo.lowerGroundingConvexDebugInfo.rawSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, rawPointsDebugInfo);

            // Create processed debug
            List<CollisionSphereDebug.DebugPointInfo> processedPointsDebugInfo = new();
            foreach (GroundCastInfo info in processedPoints)
            {
                processedPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.lowerGroundingConvexDebugInfo.processedSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, processedPointsDebugInfo);

            // Create walkable debug
            List<CollisionSphereDebug.DebugPointInfo> walkablePointsDebugInfo = new();
            foreach (GroundCastInfo info in walkableGroundPoints)
            {
                walkablePointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.lowerGroundingConvexDebugInfo.walkableSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, walkablePointsDebugInfo);

            // Create convex slopes debug
            List<CollisionSphereDebug.DebugPointInfo> convexSlopePointsDebugInfo = new();
            foreach (GroundCastInfo info in slopePoints)
            {
                convexSlopePointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.lowerGroundingConvexDebugInfo.slopesSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, convexSlopePointsDebugInfo);

            // Create best debug
            List<CollisionSphereDebug.DebugPointInfo> bestGroundPoint = new List<CollisionSphereDebug.DebugPointInfo>();
            if (foundBest)
            {
                CollisionSphereDebug.DebugPointInfo bestPointDebugInfo = new CollisionSphereDebug.DebugPointInfo(best.point, best.GetCalculatedGroundNormal());
                bestGroundPoint.Add(bestPointDebugInfo);
            }
            debugInfo.lowerGroundingConvexDebugInfo.bestSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, bestGroundPoint);

            // Create depentrated debug
            List<CollisionSphereDebug.DebugPointInfo> depenPoint = new List<CollisionSphereDebug.DebugPointInfo>();
            debugInfo.lowerGroundingConvexDebugInfo.depenSphereInfo = new CollisionSphereDebug.DebugDrawInfo(newOrigin, newTransformUp, depenPoint);

            debugInfo.debugType = DebugInfoManager.PlayerDebugModeType.LowerGrounding;
        }
        else
        {
            debugInfo.debugType = DebugInfoManager.PlayerDebugModeType.None;
        }

        return debugInfo;
    }


    public static GroundingInfo EdgeGrounding (Vector3 origin, Vector3 originalOrigin, Vector3 transformUp, Vector3 velocity, Vector3 gravityDir, float sphereRadius, LayerMask collisionLayers, List<Component> ignoreColliders, bool playerGrounded, GroundingInfo previousGroundInfo, bool firstCheck, bool debugModeOn = false)
    {
        GroundingInfo groundingInfo = new GroundingInfo();
        float maxStepHeight = sphereRadius * maxStepHeightMod;
        float checkRadius = sphereRadius;
        Vector3 checkOrigin = origin + (maxStepHeight + extraDepenetrationDistance - sphereRadius) * transformUp;
        Vector3 lowerOrigin = origin - sphereRadius * transformUp;

        // Get collision points
        List<GroundingSphereCollisionInfo> groundContactsBuffer = new List<GroundingSphereCollisionInfo>();
        GroundingSphereCollisionDetect.DetectSphereCollisions(checkOrigin, checkOrigin, checkRadius, collisionLayers, ignoreColliders, groundContactsBuffer, transformUp, 0, true, true);

        // Process points
        List<GroundCastInfo> walkableGroundPoints = new List<GroundCastInfo>();
        List<GroundCastInfo> nonWalkableGroundPoints = new List<GroundCastInfo>();
        List<GroundCastInfo> processedPoints = new List<GroundCastInfo>();
        for (int i = 0; i < groundContactsBuffer.Count; i++)
        {
            GroundingSphereCollisionInfo collisionPoint = groundContactsBuffer[i];

            GroundingEdgeCollisionInfo normalsInfo = new GroundingEdgeCollisionInfo();
            Vector3 normal = collisionPoint.interpolatedNormal;

            if (collisionPoint.isOnEdge)
            {
                normalsInfo = GroundingSphereCollisionDetect.DetectEdgeCollisions(collisionPoint.collider, collisionPoint.closestPointOnSurface, 0.0001f, collisionPoint.realNormal, collisionPoint.interpolatedNormal, transformUp, gravityDir, velocity, 0);
                normal = normalsInfo.calculatedGroundNormal;
            }

            GroundCastInfo processedGround = new GroundCastInfo(checkOrigin, sphereRadius, collisionPoint.closestPointOnSurface, normal, normalsInfo, collisionPoint.collider, collisionPoint.isOnEdge);
            processedPoints.Add(processedGround);

            

            if (CanWalkToSlope(processedGround.GetCalculatedGroundNormal(), transformUp, gravityDir, playerGrounded, processedGround.staircaseNormal).can)
            {
                processedGround.walkable = true;
                walkableGroundPoints.Add(processedGround);
            }
            else
            {
                processedGround.walkable = false;
                nonWalkableGroundPoints.Add(processedGround);
            }
        }

        // Find sphere points
        List<GroundCastInfo> validPoints = new List<GroundCastInfo>();
        foreach (GroundCastInfo info in walkableGroundPoints)
        {
            if (info.staircaseNormal != Vector3.zero) continue;

            if (!info.edgeInfo.GetOnHardEdge()) continue;

            float slopeAngle = Vector3.Angle(-gravityDir, info.GetCalculatedGroundNormal());
            SlopeInfo.SlopeType slopeType = SlopeInfo.GetSlopeType(slopeAngle);

            if (!(slopeType == SlopeInfo.SlopeType.Shallow || slopeType == SlopeInfo.SlopeType.None)) continue;

            Vector3 lateral = Vector3.ProjectOnPlane(lowerOrigin - info.point, info.GetCalculatedGroundNormal());

            if (lateral.magnitude > sphereRadius * 0.5f) continue;

            float vertical = ExtVector3.MagnitudeInDirection(lowerOrigin - info.point, info.GetCalculatedGroundNormal(), false);

            if (vertical > 0) continue;

            //if (Mathf.Abs(vertical) > maxStepHeight) continue;

            if (RaycastCheckValidStep(checkOrigin, info, gravityDir, sphereRadius, collisionLayers))
            {
                continue;
            }

            validPoints.Add(info);

        }

        // Choose best point
        GroundCastInfo best = new GroundCastInfo();
        bool foundBest = best.hasHit;
        if (validPoints.Count > 0)
        {
            Vector3 highestPoint = float.MinValue * transformUp;
            foreach (GroundCastInfo info in validPoints)
            {
                if (ExtVector3.MagnitudeInDirection((info.point - highestPoint), transformUp, false) > 0)
                {
                    highestPoint = info.point;
                    best = info;
                    foundBest = true;
                }
            }
        }
        else if (playerGrounded)
        {

        }


        // Depenetrate
        Vector3 newOrigin = origin;
        Vector3 newTransformUp = transformUp;

        // If it IS the first check then continue to set not grounded
        if (!firstCheck && !foundBest)
        {
            // We found nothing which means we are already depenetrated properly, so just return the info for the previous grounding instead of setting grounding empty
            groundingInfo.depenetrated = false;
            groundingInfo.grounded = true;
            groundingInfo.newPosition = origin;
            groundingInfo.groundNormal = transformUp;
            groundingInfo.collider = previousGroundInfo.collider;
            groundingInfo.staircaseNormal = previousGroundInfo.staircaseNormal;

            groundingInfo.debugInfo = GroundingSetDebugInfo(debugModeOn, origin, newOrigin, transformUp, newTransformUp, false, groundContactsBuffer, walkableGroundPoints, processedPoints, validPoints, new GroundCastInfo());
            return groundingInfo;
        }

        if (foundBest)
        {
            Vector3 depenDir = best.GetCalculatedGroundNormal();
            Vector3 extraDepenDir = best.GetCalculatedGroundNormal();

            bool isStaircase = false;
            if (best.staircaseNormal != Vector3.zero)
            {
                isStaircase = true;
                depenDir = -gravityDir;
                extraDepenDir = -gravityDir;
            }

            if (!playerGrounded || isStaircase)
            {
                //depenDir = -gravityDir;
            }


            float vertical = ExtVector3.MagnitudeInDirection(lowerOrigin - best.point, best.GetCalculatedGroundNormal(), false);

            Vector3 toMove = (-vertical + extraDepenetrationDistance) * best.GetCalculatedGroundNormal();
            newOrigin += toMove;

            groundingInfo.depenetrated = true;

            newTransformUp = isStaircase ? -gravityDir : best.GetCalculatedGroundNormal();
            groundingInfo.grounded = true;
            groundingInfo.newPosition = newOrigin;
            groundingInfo.groundNormal = newTransformUp;
            groundingInfo.collider = best.collider;
            groundingInfo.staircaseNormal = isStaircase ? best.GetCalculatedGroundNormal() : Vector3.zero;
        }
        else
        {
            groundingInfo.grounded = false;
            groundingInfo.newPosition = originalOrigin;
            groundingInfo.groundNormal = -gravityDir;

            Debug.Log("found nothing!");
        }

        groundingInfo.debugInfo = EdgeGroundingSetDebugInfo(debugModeOn, origin, newOrigin, transformUp, newTransformUp, foundBest, groundContactsBuffer, walkableGroundPoints, processedPoints, validPoints, best);
        return groundingInfo;
    }

    public static CollisionSphereDebug.DebugInfoContainer EdgeGroundingSetDebugInfo(bool debugModeOn, Vector3 origin, Vector3 newOrigin, Vector3 transformUp, Vector3 newTransformUp, bool foundBest,
        List<GroundingSphereCollisionInfo> groundContactsBuffer, List<GroundCastInfo> walkableGroundPoints, List<GroundCastInfo> processedPoints, List<GroundCastInfo> validPoints, GroundCastInfo best)
    {
        CollisionSphereDebug.DebugInfoContainer debugInfo = new();
        debugInfo.edgeGroundingDebugInfo = new();

        if (debugModeOn)
        {
            // Create raw debug
            List<CollisionSphereDebug.DebugPointInfo> rawPointsDebugInfo = new();
            foreach (GroundingSphereCollisionInfo info in groundContactsBuffer)
            {
                rawPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.closestPointOnSurface, info.realNormal));
            }
            debugInfo.edgeGroundingDebugInfo.rawSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, rawPointsDebugInfo);

            // Create processed debug
            List<CollisionSphereDebug.DebugPointInfo> processedPointsDebugInfo = new();
            foreach (GroundCastInfo info in processedPoints)
            {
                processedPointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.edgeGroundingDebugInfo.processedSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, processedPointsDebugInfo);

            // Create walkable debug
            List<CollisionSphereDebug.DebugPointInfo> walkablePointsDebugInfo = new();
            foreach (GroundCastInfo info in walkableGroundPoints)
            {
                walkablePointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.edgeGroundingDebugInfo.walkableSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, walkablePointsDebugInfo);

            // Create convex slopes debug
            List<CollisionSphereDebug.DebugPointInfo> convexSlopePointsDebugInfo = new();
            foreach (GroundCastInfo info in validPoints)
            {
                convexSlopePointsDebugInfo.Add(new CollisionSphereDebug.DebugPointInfo(info.point, info.GetCalculatedGroundNormal()));
            }
            debugInfo.edgeGroundingDebugInfo.validSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, convexSlopePointsDebugInfo);

            // Create best debug
            List<CollisionSphereDebug.DebugPointInfo> bestGroundPoint = new List<CollisionSphereDebug.DebugPointInfo>();
            if (foundBest)
            {
                CollisionSphereDebug.DebugPointInfo bestPointDebugInfo = new CollisionSphereDebug.DebugPointInfo(best.point, best.GetCalculatedGroundNormal());
                bestGroundPoint.Add(bestPointDebugInfo);
            }
            debugInfo.edgeGroundingDebugInfo.bestSphereInfo = new CollisionSphereDebug.DebugDrawInfo(origin, transformUp, bestGroundPoint);

            // Create depentrated debug
            List<CollisionSphereDebug.DebugPointInfo> depenPoint = new List<CollisionSphereDebug.DebugPointInfo>();
            debugInfo.edgeGroundingDebugInfo.depenSphereInfo = new CollisionSphereDebug.DebugDrawInfo(newOrigin, newTransformUp, depenPoint);

            debugInfo.debugType = DebugInfoManager.PlayerDebugModeType.EdgeGrounding;
        }
        else
        {
            debugInfo.debugType = DebugInfoManager.PlayerDebugModeType.None;
        }

        return debugInfo;
    }


    public static GroundCastInfo ChooseBestGroundingGround (List<GroundCastInfo> points, Vector3 velocity, Vector3 gravityDir, Vector3 origin, bool playerGrounded, Vector3 playerTransformUp)
    {
        GroundCastInfo best = new ();
        bool foundBest = false;

        Vector3 lateralVelocity = Vector3.ProjectOnPlane(velocity, playerTransformUp);

        if (playerGrounded && lateralVelocity != Vector3.zero)
        {
            if (points.Count > 0)
            {
                // Choose best concave slope point
                

                List<GroundCastInfo> positiveVelocityPoints = new();
                List<GroundCastInfo> zeroVelocityPoints = new();
                List<GroundCastInfo> negativeVelocityPoints = new();

                for (int i = 0; i < points.Count; i++)
                {
                    // Choose the point closest to the sphere centre
                    GroundCastInfo info = points[i];
                    Vector3 normal = info.GetCalculatedGroundNormal();
                    float velocityAgainstNormal = ExtVector3.MagnitudeInDirection(lateralVelocity, -normal, false);
                    info.velocityAgainstNormal = velocityAgainstNormal;

                    if (Mathf.Abs (velocityAgainstNormal) < 0.0001f)
                    {
                        zeroVelocityPoints.Add(info);
                        negativeVelocityPoints.Add(info);
                    }
                    else if (velocityAgainstNormal < 0)
                    {
                        negativeVelocityPoints.Add(info);
                    }
                    else
                    {
                        positiveVelocityPoints.Add(info);
                    }
                }

                float biggestVelocityAgainstNormal = float.MinValue;
                foreach (GroundCastInfo info in positiveVelocityPoints)
                {
                    if (info.velocityAgainstNormal > biggestVelocityAgainstNormal)
                    {
                        biggestVelocityAgainstNormal = info.velocityAgainstNormal;
                        best = info;
                        foundBest = true;
                    }
                }

                //if (foundBest) Debug.Log("found positive");

                if (!foundBest)
                {
                    float smallestVelocityAgainstNormal = float.MaxValue;
                    foreach (GroundCastInfo info in negativeVelocityPoints)
                    {
                        if (Mathf.Abs(info.velocityAgainstNormal) < 0.0001f)
                        {
                            //Debug.Log("its a zero");
                            smallestVelocityAgainstNormal = 0;
                            best = info;
                            foundBest = true;
                        }

                        if (info.velocityAgainstNormal < smallestVelocityAgainstNormal)
                        {
                            smallestVelocityAgainstNormal = info.velocityAgainstNormal;
                            best = info;
                            foundBest = true;
                        }
                    }

                    //if (foundBest) Debug.Log("found negative");

                    if (!foundBest)
                    {
                        // Choose best concave slope point
                        float smallestDistance = float.MaxValue;
                        foreach (GroundCastInfo info in zeroVelocityPoints)
                        {
                            // Choose the point closest to the sphere centre
                            float distance = (origin - info.point).magnitude;

                            if (distance < smallestDistance)
                            {
                                smallestDistance = distance;
                                best = info;
                                foundBest = true;
                                //Debug.Log("found zero");
                            }
                            else if (foundBest && distance == smallestDistance)
                            {
                                // Just case, use closest to gravity flat ground if distance is the same
                                float myAngleDiff = Vector3.Angle(-gravityDir, info.GetCalculatedGroundNormal());
                                float theirAngleDiff = Vector3.Angle(-gravityDir, best.GetCalculatedGroundNormal());

                                if (myAngleDiff < theirAngleDiff)
                                {
                                    smallestDistance = distance;
                                    best = info;
                                    foundBest = true;
                                    //Debug.Log("found zero equal");
                                }
                            }
                        }
                        
                    }
                }
            }
        }
        else
        {
            if (points.Count > 0)
            {
                // Choose best concave slope point
                float smallestDistance = float.MaxValue;
                foreach (GroundCastInfo info in points)
                {
                    // Choose the point closest to the sphere centre
                    float distance = (origin - info.point).magnitude;
                    if (distance < smallestDistance)
                    {
                        smallestDistance = distance;
                        best = info;
                        foundBest = true;
                    }
                    else if (foundBest && distance == smallestDistance)
                    {
                        // Just case, use closest to gravity flat ground if distance is the same
                        float myAngleDiff = Vector3.Angle(-gravityDir, info.GetCalculatedGroundNormal());
                        float theirAngleDiff = Vector3.Angle(-gravityDir, best.GetCalculatedGroundNormal());

                        if (myAngleDiff < theirAngleDiff)
                        {
                            smallestDistance = distance;
                            best = info;
                            foundBest = true;
                        }
                    }
                }
            }
        }

        

        return best;
    }


    static Vector3 ModifyVelocityAfterCollision(Vector3 currentVelocity, Vector3 gravityDir, GroundingInfo groundInfo, bool wasGroundedBefore, Vector3 previousGroundNormal, bool ignoreSlopePhysics = false)
    {
        // Don't include the additional velocity etc because it should be one-time only.
        // collisionInfo.velocity is the velocity we will edit, which started as velocity originally
        Vector3 oldLocal = ExtVector3.InverseTransformDirection(previousGroundNormal, currentVelocity);
        Vector3 previousLateralVelocity = new Vector3(oldLocal.x, 0, oldLocal.z);

        //SetGroundPivot(Quaternion.FromToRotation(Vector3.up, groundInfo.groundNormal));

        if (groundInfo.grounded)
        {
            if (!wasGroundedBefore) // Hit from in air
            {
                float slopeAngle = 0;
                Vector3 slopeDir;
                SlopeInfo.SlopeType slopeType = SlopeInfo.SlopeType.None;

                if (!ignoreSlopePhysics)
                {
                    slopeAngle = Vector3.Angle(-gravityDir, groundInfo.groundNormal);
                    slopeType = SlopeInfo.GetSlopeType(slopeAngle);
                    slopeDir = ExtVector3.InverseTransformDirection(groundInfo.groundNormal, gravityDir);
                    slopeDir.y = 0;
                }

                if (SlopeInfo.IsSlopeSteepOrUp(slopeAngle))
                {

                }
                else if (slopeType == SlopeInfo.SlopeType.Shallow || true)
                {
                    currentVelocity = Vector3.ProjectOnPlane(currentVelocity, gravityDir);
                }
                else if (slopeType == SlopeInfo.SlopeType.Moderate)
                {

                }
            }
            else  // Hit and already on ground
            {
                currentVelocity = ExtVector3.TransformDirection(groundInfo.groundNormal, previousLateralVelocity);

                // Rotate velocity direction while preserving magnitude
                /*float magnitude = collisionInfo.velocity.magnitude;
				Vector3 flattenedVelocity = Vector3.ProjectOnPlane(collisionInfo.velocity, collisionInfo.transformUp);
				collisionInfo.velocity = flattenedVelocity.normalized * magnitude;*/

                /*if (groundInfo.previouslyWall)
                {
                    //if we returned to the ground from a non-gravity walkable wall, then limit the speed against that normal first
                    //collisionInfo.velocity = Vector3.ProjectOnPlane(collisionInfo.velocity, oldUp);
                    currentVelocity = Vector3.zero;
                    //Debug.Log("done it");
                }*/
            }
        }
        else
        {
            if (wasGroundedBefore) //first update after leaving the ground
            {

            }

        }

        if (groundInfo.grounded)
        {
            currentVelocity = Vector3.ProjectOnPlane(currentVelocity, groundInfo.groundNormal);
        }

        return currentVelocity;
    }


    public static DirectCheckInfo DirectGroundDetect (Vector3 origin, Vector3 transformUp, float sphereRadius, int layerMask)
    {
        DirectCheckInfo directInfo = new();

        RaycastHit hitInfo;
        if (Physics.Raycast (origin, -transformUp, out hitInfo, sphereRadius + 0.001f, layerMask))
        {
            directInfo.hasHit = true;
            directInfo.hitDistance = hitInfo.distance;
        }

        return directInfo;
    }


    public struct CanWalkToSlopeInfo
    {
        public bool can;
        public bool wallActingAsFloor;

        public CanWalkToSlopeInfo (bool _can, bool _wallActingAsFloor)
        {
            this.can = _can;
            this.wallActingAsFloor = _wallActingAsFloor;
        }
    }

    public static CanWalkToSlopeInfo CanWalkToSlope(Vector3 normal, Vector3 comparedNormal, Vector3 gravityDir, bool grounded, Vector3 staircaseNormal)
    {
        if (normal == Vector3.zero || comparedNormal == Vector3.zero) return new CanWalkToSlopeInfo();

        if (staircaseNormal != Vector3.zero)
        {
            return new CanWalkToSlopeInfo(CanWalkOnStaircase(staircaseNormal, gravityDir), false);
        }

        if (grounded)
        {
            /*if (ExtVector3.Angle(normal, -gravityDir) < ExtVector3.Angle(comparedNormal, -gravityDir)) //going toward gravityUp?
            {
                Debug.Log("going towards gravUp!");

                if (CanWalkOnSlope(normal, comparedNormal))
                {
                    return true;
                }

                //drop onto ground off a wall check if too steep for normal check
                return CanWalkOnSlope(normal, -gravityDir);
            }
            return CanWalkOnSlope(normal, comparedNormal);*/
            // Not sure what will happen if on a slanted ceiling and touch some parallel slanted ground, but means all walkable slope v shapes work

            if (CanWalkOnSlope(normal, comparedNormal, gravityDir, staircaseNormal))
            {
                return new CanWalkToSlopeInfo (CanWalkOnSlope(normal, comparedNormal, gravityDir, staircaseNormal), false);
            }

            // This handles going from a wall onto a floor
            if (CanWalkOnSlope(normal, -gravityDir, gravityDir, staircaseNormal))
            {
                // We only want to set wallActingAsFloor true if our current slope normal is not walkable from gravity as well
                return new CanWalkToSlopeInfo(true, !CanWalkOnSlope(comparedNormal, -gravityDir, gravityDir, staircaseNormal));
            }

            return new CanWalkToSlopeInfo ();

        }
        return new CanWalkToSlopeInfo(CanWalkOnSlope(normal, comparedNormal, gravityDir, staircaseNormal), false);
    }

    public static bool CanWalkOnSlope(Vector3 normal, Vector3 comparedNormal, Vector3 gravityDir, Vector3 staircaseNormal)
    {
        if (normal == Vector3.zero || comparedNormal == Vector3.zero) return false;
        if (staircaseNormal != Vector3.zero)
        {
            return CanWalkOnStaircase(staircaseNormal, gravityDir);
        }
        return ExtVector3.Angle(normal, comparedNormal) < SlopeInfo.concaveSlopeLimit;
    }

    public static bool CanWalkOnConvexSlope(Vector3 normal, Vector3 comparedNormal, Vector3 gravityDir, Vector3 staircaseNormal)
    {
        if (normal == Vector3.zero || comparedNormal == Vector3.zero) return false;
        if (staircaseNormal != Vector3.zero)
        {
            return CanWalkOnStaircase (staircaseNormal, gravityDir);
        }
        return ExtVector3.Angle(normal, comparedNormal) < SlopeInfo.convexSlopeLimit;
    }

    public static bool RaycastCheckValidStep (Vector3 origin, GroundCastInfo info, Vector3 gravityDir, float sphereRadius, LayerMask collisionLayers)
    {
        Vector3 dir = Vector3.ProjectOnPlane((origin - info.point), gravityDir).normalized;
        if (dir == Vector3.zero) return false;
        Vector3 raycastStartPoint = info.point + 0.01f * dir;

        RaycastHit hitInfo;
        if (Physics.Raycast(raycastStartPoint, gravityDir, out hitInfo, sphereRadius * maxStepHeightMod, collisionLayers))
        {
            if (CanWalkOnSlope(hitInfo.normal, -gravityDir, gravityDir, Vector3.zero))
            {
                return true;
            }
        }

        return false;
    }

    public static Vector3 GetMaxStepPoint (Vector3 origin, Vector3 transformUp, float sphereRadius)
    {
        return origin - transformUp * (sphereRadius - (sphereRadius * maxStepHeightMod));
    }

    public static Vector3 GetMinStepPoint(Vector3 origin, Vector3 transformUp, float sphereRadius)
    {
        return origin - transformUp * (sphereRadius + (sphereRadius * maxStepHeightMod));
    }

    public static bool CheckPointIsAboveMaxStepPoint (Vector3 point, Vector3 maxStepPoint, Vector3 transformUp)
    {
        return ExtVector3.MagnitudeInDirection((point - maxStepPoint), transformUp, false) > 0;
    }

    public static bool CheckPointIsBelowMinStepPoint(Vector3 point, Vector3 minStepPoint, Vector3 transformUp)
    {
        return ExtVector3.MagnitudeInDirection((point - minStepPoint), transformUp, false) < 0;
    }

    public static bool CanWalkOnStaircase (Vector3 staircaseNormal, Vector3 gravityDir)
    {
        return ExtVector3.Angle(-gravityDir, staircaseNormal) < SlopeInfo.staircaseSlopeLimit;
    }

    public static float GetExtraCheckRadius (float sphereRadius, float extraDepenDist, bool addExtra)
    {
        if (addExtra)
        {
            return sphereRadius + extraDepenDist + extraCheckDistance;
        }

        return sphereRadius + extraDepenDist;
    }

    public static float GetFallOffSlopeSpeedThreshold ()
    {
        return fallOffSlopeSpeedThreshold;
    }

    public struct GroundingLoopInfo
    {
        public GroundingInfo groundingInfo;
        public Vector3 newVelocity;
        public List<List<CollisionSphereDebug.DebugInfoContainer>> allDebugInfo;
        public int iterationsDone;
    }

    public struct GroundingInfo
    {
        public bool grounded;
        public Vector3 newPosition;
        public Vector3 groundNormal;
        public Collider collider;
        public bool depenetrated;
        public Vector3 staircaseNormal; //acts as staircase check, not staircase == Vector3.zero
        public CollisionSphereDebug.DebugInfoContainer debugInfo;
    }

    public struct WallLoopInfo
    {
        public Vector3 newPosition;
        public Vector3 newVelocity;
        public List<List<CollisionSphereDebug.DebugInfoContainer>> allDebugInfo;
        public int iterationsDone;
        public List<GroundCastInfo> allWallDepenPoints;
        public bool depenetrated;
    }

    public struct WallInfo
    {
        public Vector3 newPosition;
        public bool depenetrated;
        public CollisionSphereDebug.DebugInfoContainer debugInfo;
        public List<GroundCastInfo> wallDepenPoints;
        public int depenetrationIterationsDone;
    }

    public struct DirectCheckInfo
    {
        public bool hasHit;
        public float hitDistance;
    }
}
