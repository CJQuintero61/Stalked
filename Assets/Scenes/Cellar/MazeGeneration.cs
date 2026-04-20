using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a procedural maze and sets up interaction triggers for the Start and End.
/// Includes corrected roof generation logic.
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Dimensions")]
    public int width = 10;
    public int height = 10;

    [Header("Visual Settings")]
    public float cellSize = 2f;
    public float wallThickness = 0.2f;
    public float wallHeight = 2f;

    [Header("Optional Prefabs")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Roof Settings")]
    [Tooltip("Should a roof be generated on top of the maze?")]
    public bool generateRoof = false;
    [Tooltip("Optional custom roof prefab. Tiled like the floor.")]
    public GameObject roofPrefab;
    [Tooltip("Material to apply to the roof if no prefab is used.")]
    public Material roofMaterial;

    [Header("Lighting")]
    public GameObject torchPrefab;
    [Range(0f, 1f)] public float torchDensity = 0.1f;
    public float torchHeight = 1.2f;

    [Header("Markers")]
    public bool showStartEndText = true;

    [Header("Materials")]
    public Material wallMaterial;
    public Material pillarMaterial;
    public Material floorMaterial;

    [Header("Custom Prefab Settings")]
    public float customWallYOffset = 0f;

    [Header("Interaction Settings")]
    [Tooltip("The tag assigned to your Player object.")]
    public string playerTag = "Player";
    [Tooltip("UI Prompt to show when the player can exit.")]
    public GameObject exitUIPrompt;

    private class Cell
    {
        public bool visited = false;
        public bool topWall = true;
        public bool rightWall = true;
        public bool bottomWall = true;
        public bool leftWall = true;
    }

    private Cell[,] grid;
    private GameObject currentMazeParent;

    void Start()
    {
        GenerateMazeData();
        BuildMazeIntoScene();
    }

    void GenerateMazeData()
    {
        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new Cell();
            }
        }

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(0, 0);
        grid[current.x, current.y].visited = true;
        stack.Push(current);

        while (stack.Count > 0)
        {
            current = stack.Pop();
            List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(current);

            if (unvisitedNeighbors.Count > 0)
            {
                stack.Push(current);
                Vector2Int chosen = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];
                RemoveWalls(current, chosen);
                grid[chosen.x, chosen.y].visited = true;
                stack.Push(chosen);
            }
        }
    }

    List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        if (cell.y + 1 < height && !grid[cell.x, cell.y + 1].visited) neighbors.Add(new Vector2Int(cell.x, cell.y + 1));
        if (cell.x + 1 < width && !grid[cell.x + 1, cell.y].visited) neighbors.Add(new Vector2Int(cell.x + 1, cell.y));
        if (cell.y - 1 >= 0 && !grid[cell.x, cell.y - 1].visited) neighbors.Add(new Vector2Int(cell.x, cell.y - 1));
        if (cell.x - 1 >= 0 && !grid[cell.x - 1, cell.y].visited) neighbors.Add(new Vector2Int(cell.x - 1, cell.y));
        return neighbors;
    }

    void RemoveWalls(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x)
        {
            if (a.y < b.y) { grid[a.x, a.y].topWall = false; grid[b.x, b.y].bottomWall = false; }
            else { grid[a.x, a.y].bottomWall = false; grid[b.x, b.y].topWall = false; }
        }
        else
        {
            if (a.x < b.x) { grid[a.x, a.y].rightWall = false; grid[b.x, b.y].leftWall = false; }
            else { grid[a.x, a.y].leftWall = false; grid[b.x, b.y].rightWall = false; }
        }
    }

    void BuildMazeIntoScene()
    {
        currentMazeParent = new GameObject("Procedural Maze");
        currentMazeParent.transform.position = Vector3.zero;

        // 1. Generate the Floor
        if (floorPrefab != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3 cellPos = new Vector3(x * cellSize, 0, y * cellSize);
                    Instantiate(floorPrefab, cellPos, floorPrefab.transform.rotation, currentMazeParent.transform);
                }
            }
        }
        else
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(currentMazeParent.transform);
            if (floorMaterial != null) floor.GetComponent<MeshRenderer>().material = floorMaterial;
            floor.transform.localScale = new Vector3((width * cellSize) / 10f, 1, (height * cellSize) / 10f);
            floor.transform.position = new Vector3((width * cellSize) / 2f - (cellSize / 2f), 0, (height * cellSize) / 2f - (cellSize / 2f));
        }

        // 2. Generate the Roof
        if (generateRoof)
        {
            if (roofPrefab != null)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Vector3 cellPos = new Vector3(x * cellSize, wallHeight, y * cellSize);
                        Instantiate(roofPrefab, cellPos, roofPrefab.transform.rotation, currentMazeParent.transform);
                    }
                }
            }
            else
            {
                GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Plane);
                roof.name = "Roof";
                roof.transform.SetParent(currentMazeParent.transform);
                
                if (roofMaterial != null)
                {
                    roof.GetComponent<MeshRenderer>().material = roofMaterial;
                }
                
                roof.transform.localScale = new Vector3((width * cellSize) / 10f, 1, (height * cellSize) / 10f);
                roof.transform.position = new Vector3(
                    (width * cellSize) / 2f - (cellSize / 2f), 
                    wallHeight, 
                    (height * cellSize) / 2f - (cellSize / 2f)
                );
                
                // Rotate the roof 180 degrees so the textured face points DOWN
                roof.transform.rotation = Quaternion.Euler(180, 0, 0);
            }
        }

        // 3. Trigger Setup
        CreateTriggerZone(new Vector2Int(0, 0), "StartTrigger", currentMazeParent.transform);
        CreateTriggerZone(new Vector2Int(width - 1, height - 1), "EndTrigger", currentMazeParent.transform);

        // 4. Generate Walls and Pillars
        Vector3 primitiveWallScale = new Vector3(cellSize - wallThickness, wallHeight, wallThickness);
        if (wallPrefab == null)
        {
            Vector3 pillarScale = new Vector3(wallThickness, wallHeight, wallThickness);
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    Vector3 pillarPos = new Vector3(x * cellSize - (cellSize / 2f), wallHeight / 2f, y * cellSize - (cellSize / 2f));
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    pillar.transform.SetParent(currentMazeParent.transform);
                    pillar.transform.position = pillarPos;
                    pillar.transform.localScale = pillarScale;
                    if (pillarMaterial != null) pillar.GetComponent<MeshRenderer>().material = pillarMaterial;
                    else if (wallMaterial != null) pillar.GetComponent<MeshRenderer>().material = wallMaterial;
                }
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 cellPos = new Vector3(x * cellSize, 0, y * cellSize);
                if (grid[x, y].topWall) CreateWall(cellPos + new Vector3(0, wallHeight / 2f, cellSize / 2f), Quaternion.identity, primitiveWallScale, currentMazeParent.transform);
                if (grid[x, y].rightWall) CreateWall(cellPos + new Vector3(cellSize / 2f, wallHeight / 2f, 0), Quaternion.Euler(0, 90, 0), primitiveWallScale, currentMazeParent.transform);
                if (y == 0 && grid[x, y].bottomWall) CreateWall(cellPos + new Vector3(0, wallHeight / 2f, -cellSize / 2f), Quaternion.identity, primitiveWallScale, currentMazeParent.transform);
                if (x == 0 && grid[x, y].leftWall) CreateWall(cellPos + new Vector3(-cellSize / 2f, wallHeight / 2f, 0), Quaternion.Euler(0, 90, 0), primitiveWallScale, currentMazeParent.transform);
            }
        }

        if (torchPrefab != null) SpawnTorches(currentMazeParent.transform);
        CreateStartAndEndMarkers(currentMazeParent.transform);
    }

    void CreateTriggerZone(Vector2Int gridPos, string zoneTag, Transform parent)
    {
        GameObject trigger = new GameObject(zoneTag);
        trigger.transform.SetParent(parent);
        trigger.transform.position = new Vector3(gridPos.x * cellSize, wallHeight / 2f, gridPos.y * cellSize);
        
        BoxCollider box = trigger.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(cellSize * 0.8f, wallHeight, cellSize * 0.8f);

        MazeTrigger helper = trigger.AddComponent<MazeTrigger>();
        helper.isStart = (zoneTag == "StartTrigger");
    }

    void SpawnTorches(Transform parent)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Random.value <= torchDensity)
                {
                    List<int> availableWalls = new List<int>();
                    if (grid[x, y].topWall) availableWalls.Add(0);
                    if (grid[x, y].rightWall) availableWalls.Add(1);
                    if (grid[x, y].bottomWall) availableWalls.Add(2);
                    if (grid[x, y].leftWall) availableWalls.Add(3);

                    if (availableWalls.Count > 0)
                    {
                        int chosenWall = availableWalls[Random.Range(0, availableWalls.Count)];
                        Vector3 cellPos = new Vector3(x * cellSize, 0, y * cellSize);
                        Vector3 torchPos = Vector3.zero;
                        Quaternion torchRot = Quaternion.identity;
                        float offsetToWall = (cellSize / 2f) - (wallThickness / 2f);

                        if (chosenWall == 0) { torchPos = cellPos + new Vector3(0, torchHeight, offsetToWall); torchRot = Quaternion.Euler(0, 180, 0); }
                        else if (chosenWall == 1) { torchPos = cellPos + new Vector3(offsetToWall, torchHeight, 0); torchRot = Quaternion.Euler(0, 270, 0); }
                        else if (chosenWall == 2) { torchPos = cellPos + new Vector3(0, torchHeight, -offsetToWall); torchRot = Quaternion.Euler(0, 0, 0); }
                        else if (chosenWall == 3) { torchPos = cellPos + new Vector3(-offsetToWall, torchHeight, 0); torchRot = Quaternion.Euler(0, 90, 0); }

                        Instantiate(torchPrefab, torchPos, torchRot * torchPrefab.transform.rotation, parent);
                    }
                }
            }
        }
    }

    void CreateStartAndEndMarkers(Transform parent)
    {
        if (!showStartEndText) return;
        CreateTextMarker(new Vector2Int(0, 0), "Start", Color.red, parent);
        CreateTextMarker(new Vector2Int(width - 1, height - 1), "End", Color.green, parent);
    }

    void CreateTextMarker(Vector2Int gridPos, string text, Color color, Transform parent)
    {
        GameObject textObj = new GameObject(text + " Text");
        textObj.transform.SetParent(parent);
        textObj.transform.position = new Vector3(gridPos.x * cellSize, wallHeight + 0.5f, gridPos.y * cellSize);
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        textMesh.characterSize = 0.1f;
        textMesh.fontSize = 64; 
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
    }

    void CreateWall(Vector3 centerPosition, Quaternion rotation, Vector3 primitiveScale, Transform parent)
    {
        if (wallPrefab != null)
        {
            Quaternion finalRotation = rotation * wallPrefab.transform.rotation;
            Vector3 prefabPosition = new Vector3(centerPosition.x, customWallYOffset, centerPosition.z);
            Instantiate(wallPrefab, prefabPosition, finalRotation, parent);
        }
        else
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            if (wallMaterial != null) wall.GetComponent<MeshRenderer>().material = wallMaterial;
            wall.transform.position = centerPosition;
            wall.transform.rotation = rotation;
            wall.transform.localScale = primitiveScale;
            wall.transform.SetParent(parent);
        }
    }
}

public class MazeTrigger : MonoBehaviour 
{
    public bool isStart;
}