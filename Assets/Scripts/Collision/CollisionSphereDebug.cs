using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSphereDebug : MonoBehaviour
{
    public GameObject lineRendererPrefab;

    const float lineLength = 0.7f;
    Color groundingColor = new Color(1f, 0f, 0f, 0.3f);
    Color lowerGroundingColor = new Color(1f, 1f, 0f, 0.3f);
    Color edgeGroundingColor = new Color(0f, 1f, 0f, 0.3f);
    Color wallColor = new Color(0f, 0f, 1f, 0.3f);

    List<GameObject> drawnPoints = new();

    bool sphereHidden = true;
    int previousCollisionDebugDrawMode = 0;

    ActionSceneController sc;
    DebugInfoManager debugInfoManager;

    DebugInfoContainer debugInfo;
    DebugInfoManager.PlayerDebugModeType debugType;

    private void Update()
    {
        int newCollisionDebugDrawMode = debugInfoManager.pointsDrawMode;

        if (!sphereHidden && newCollisionDebugDrawMode != previousCollisionDebugDrawMode)
        {
            ChooseDrawModeAndDraw(newCollisionDebugDrawMode, debugInfo);
            previousCollisionDebugDrawMode = newCollisionDebugDrawMode;
        }
    }

    public void DoStart (ActionSceneController sceneController, DebugInfoContainer givenInfo, DebugInfoManager.PlayerDebugModeType debugType)
    {
        sc = sceneController;
        debugInfoManager = sc.debugInfoManager;
        debugInfo = givenInfo;
        this.debugType = debugType;

        SetMaterialColour();
    }

    public void ShowSphere()
    {
        sphereHidden = false;
        gameObject.GetComponent<MeshRenderer>().enabled = true;
        ChooseDrawModeAndDraw(debugInfoManager.pointsDrawMode, debugInfo);
    }

    public void HideSphere()
    {
        sphereHidden = true;
        DestroyDrawnPoints();
        gameObject.GetComponent<MeshRenderer>().enabled = false;
    }

    void ChooseDrawModeAndDraw (int mode, DebugInfoContainer info)
    {
        ChosenTypeInfo toDraw;

        switch (debugType)
        {
            case DebugInfoManager.PlayerDebugModeType.Grounding:
                toDraw = info.groundingDebugInfo.GetDrawInfoForMode(mode);
                DrawPoints(toDraw);
                break;
            case DebugInfoManager.PlayerDebugModeType.LowerGrounding:
                toDraw = info.lowerGroundingConvexDebugInfo.GetDrawInfoForMode(mode);
                DrawPoints(toDraw);
                break;
            case DebugInfoManager.PlayerDebugModeType.EdgeGrounding:
                toDraw = info.edgeGroundingDebugInfo.GetDrawInfoForMode(mode);
                DrawPoints(toDraw);
                break;
            case DebugInfoManager.PlayerDebugModeType.Wall:
                toDraw = info.wallDebugInfo.GetDrawInfoForMode(mode);
                DrawPoints(toDraw);
                break;
            default:
                Debug.Log("Invalid player debug mode!");
                break;
        }
    }

    void DrawPoints (ChosenTypeInfo drawInfo)
    {
        DestroyDrawnPoints();

        transform.position = drawInfo.toDraw.spherePos;

        foreach (DebugPointInfo info in drawInfo.toDraw.debugPointsInfo)
        {
            CreateNewLineRenderer(info.position, info.position + lineLength * info.normal, drawInfo.color);
        }

        CreateNewLineRenderer(transform.position, transform.position + lineLength * -drawInfo.toDraw.transformUp, Color.yellow);
    }

    void CreateNewLineRenderer(Vector3 x, Vector3 y, Color color)
    {
        List<LineRenderer> lineRenderers = new List<LineRenderer>();
        GameObject newObject = Instantiate(lineRendererPrefab, transform.position, Quaternion.identity, this.transform);
        LineRenderer newLineRenderer = newObject.GetComponent<LineRenderer>();
        newLineRenderer.useWorldSpace = true;

        Vector3[] points = { x, y };
        newLineRenderer.SetPositions(points);
        newLineRenderer.material.color = color;
        lineRenderers.Add(newLineRenderer);

        drawnPoints.Add(newObject);
    }

    void DestroyDrawnPoints ()
    {
        foreach (GameObject point in drawnPoints)
        {
            Destroy(point);
        }
    }

    void SetMaterialColour ()
    {
        switch (debugType)
        {
            case DebugInfoManager.PlayerDebugModeType.Grounding:
                gameObject.GetComponent<Renderer>().material.SetColor("_Color", groundingColor);
                break;
            case DebugInfoManager.PlayerDebugModeType.LowerGrounding:
                gameObject.GetComponent<Renderer>().material.SetColor("_Color", lowerGroundingColor);
                break;
            case DebugInfoManager.PlayerDebugModeType.EdgeGrounding:
                gameObject.GetComponent<Renderer>().material.SetColor("_Color", edgeGroundingColor);
                break;
            case DebugInfoManager.PlayerDebugModeType.Wall:
                gameObject.GetComponent<Renderer>().material.SetColor("_Color", wallColor);
                break;
            default:
                Debug.Log("Invalid player debug mode!");
                break;
        }
    }

    public struct DebugDrawInfo
    {
        public Vector3 spherePos;
        public Vector3 transformUp;
        public List<DebugPointInfo> debugPointsInfo;

        public DebugDrawInfo(Vector3 spherePos, Vector3 transformUp, List<DebugPointInfo> debugPointsInfo)
        {
            this.spherePos = spherePos;
            this.transformUp = transformUp;
            this.debugPointsInfo = debugPointsInfo;
        }
    }
    public struct DebugPointInfo
    {
        public Vector3 position;
        public Vector3 normal;
        public DebugPointInfo (Vector3 position, Vector3 normal)
        {
            this.position = position;
            this.normal = normal;
        }
    }

    public struct ChosenTypeInfo
    {
        public DebugDrawInfo toDraw;
        public Color color;

        public ChosenTypeInfo (DebugDrawInfo toDraw, Color color)
        {
            this.toDraw = toDraw;
            this.color = color;
        }
    }

    public struct DebugInfoContainer
    {
        public GroundingGroundInfo groundingDebugInfo;
        public LowerGroundingGroundInfo lowerGroundingConvexDebugInfo;
        public EdgeGroundingGroundInfo edgeGroundingDebugInfo;
        public WallCollisionInfo wallDebugInfo;
        public DebugInfoManager.PlayerDebugModeType debugType;
    }

    public struct GroundingGroundInfo
    {
        public DebugDrawInfo rawSphereInfo;
        public DebugDrawInfo processedSphereInfo;
        public DebugDrawInfo walkableSphereInfo;
        public DebugDrawInfo directPointsSphereInfo;
        public DebugDrawInfo slopesSphereInfo;
        public DebugDrawInfo bestSphereInfo;
        public DebugDrawInfo depenSphereInfo;

        public ChosenTypeInfo GetDrawInfoForMode (int mode)
        {
            switch (mode)
            {
                case 0:
                    Debug.Log("Drawing raw points");
                    return new ChosenTypeInfo(this.rawSphereInfo, Color.black);
                case 1:
                    Debug.Log("Drawing processed points");
                    return new ChosenTypeInfo(this.processedSphereInfo, Color.green);
                case 2:
                    Debug.Log("Drawing walkable points");
                    return new ChosenTypeInfo(this.walkableSphereInfo, Color.blue);
                case 3:
                    Debug.Log("Drawing slope points");
                    return new ChosenTypeInfo(this.slopesSphereInfo, Color.cyan);
                case 4:
                    Debug.Log("Drawing best point");
                    return new ChosenTypeInfo(this.bestSphereInfo, Color.green);
                case 5:
                    Debug.Log("Drawing depenetrated sphere");
                    return new ChosenTypeInfo(this.depenSphereInfo, Color.black);
                default:
                    Debug.Log("Drawing depenetrated sphere");
                    return new ChosenTypeInfo(this.depenSphereInfo, Color.black);
            }
        }
    }

    public struct LowerGroundingGroundInfo
    {
        public DebugDrawInfo rawSphereInfo;
        public DebugDrawInfo processedSphereInfo;
        public DebugDrawInfo walkableSphereInfo;
        public DebugDrawInfo slopesSphereInfo;
        public DebugDrawInfo bestSphereInfo;
        public DebugDrawInfo depenSphereInfo;

        public ChosenTypeInfo GetDrawInfoForMode(int mode)
        {
            switch (mode)
            {
                case 0:
                    Debug.Log("Drawing raw points");
                    return new ChosenTypeInfo(this.rawSphereInfo, Color.black);
                case 1:
                    Debug.Log("Drawing processed points");
                    return new ChosenTypeInfo(this.processedSphereInfo, Color.green);
                case 2:
                    Debug.Log("Drawing walkable points");
                    return new ChosenTypeInfo(this.walkableSphereInfo, Color.blue);
                case 3:
                    Debug.Log("Drawing slope points");
                    return new ChosenTypeInfo(this.slopesSphereInfo, Color.cyan);
                case 4:
                    Debug.Log("Drawing best point");
                    return new ChosenTypeInfo(this.bestSphereInfo, Color.green);
                case 5:
                    Debug.Log("Drawing depenetrated sphere");
                    return new ChosenTypeInfo(this.depenSphereInfo, Color.black);
                default:
                    Debug.Log("Drawing depenetrated sphere");
                    return new ChosenTypeInfo(this.depenSphereInfo, Color.black);
            }
        }
    }

    public struct EdgeGroundingGroundInfo
    {
        public DebugDrawInfo rawSphereInfo;
        public DebugDrawInfo processedSphereInfo;
        public DebugDrawInfo walkableSphereInfo;
        public DebugDrawInfo validSphereInfo;
        public DebugDrawInfo bestSphereInfo;
        public DebugDrawInfo depenSphereInfo;

        public ChosenTypeInfo GetDrawInfoForMode(int mode)
        {
            switch (mode)
            {
                case 0:
                    Debug.Log("Drawing raw points");
                    return new ChosenTypeInfo(this.rawSphereInfo, Color.black);
                case 1:
                    Debug.Log("Drawing processed points");
                    return new ChosenTypeInfo(this.processedSphereInfo, Color.green);
                case 2:
                    Debug.Log("Drawing walkable points");
                    return new ChosenTypeInfo(this.walkableSphereInfo, Color.blue);
                case 3:
                    Debug.Log("Drawing slope points");
                    return new ChosenTypeInfo(this.validSphereInfo, Color.cyan);
                case 4:
                    Debug.Log("Drawing best point");
                    return new ChosenTypeInfo(this.bestSphereInfo, Color.green);
                case 5:
                    Debug.Log("Drawing depenetrated sphere");
                    return new ChosenTypeInfo(this.depenSphereInfo, Color.black);
                default:
                    Debug.Log("Drawing depenetrated sphere");
                    return new ChosenTypeInfo(this.depenSphereInfo, Color.black);
            }
        }
    }

    public struct WallCollisionInfo
    {
        public DebugDrawInfo rawSphereInfo;
        public DebugDrawInfo processedSphereInfo;
        public DebugDrawInfo walkableSphereInfo;
        public DebugDrawInfo wallSphereInfo;
        public DebugDrawInfo wallDepenSphereInfo;
        public DebugDrawInfo depenSphereInfo;

        public ChosenTypeInfo GetDrawInfoForMode(int mode)
        {
            switch (mode)
            {
                case 0:
                    Debug.Log("Drawing raw points");
                    return new ChosenTypeInfo(this.rawSphereInfo, Color.black);
                case 1:
                    Debug.Log("Drawing processed points");
                    return new ChosenTypeInfo(this.processedSphereInfo, Color.green);
                case 2:
                    Debug.Log("Drawing walkable points");
                    return new ChosenTypeInfo(this.walkableSphereInfo, Color.blue);
                case 3:
                    Debug.Log("Drawing wall points");
                    return new ChosenTypeInfo(this.wallSphereInfo, Color.cyan);
                case 4:
                    Debug.Log("Drawing depen wall points");
                    return new ChosenTypeInfo(this.wallDepenSphereInfo, Color.cyan);
                case 5:
                    Debug.Log("Drawing depenetrated sphere");
                    return new ChosenTypeInfo(this.depenSphereInfo, Color.black);
                default:
                    Debug.Log("Drawing depenetrated sphere");
                    return new ChosenTypeInfo(this.depenSphereInfo, Color.black);
            }
        }
    }
}
