using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a procedural maze using the Recursive Backtracker algorithm.
/// </summary>
public class MazeGenerator : MonoBehaviour
{
    [Header("Maze Dimensions")]
    [Tooltip("Number of cells in the X direction")]
    public int width = 10;
    [Tooltip("Number of cells in the Z direction")]
    public int height = 10;

    [Header("Visual Settings")]
    [Tooltip("The physical size of each cell in Unity units")]
    public float cellSize = 2f;
    public float wallThickness = 0.2f;
    public float wallHeight = 2f;

    [Header("Optional Prefabs (Uses Primitives if Empty)")]
    public GameObject wallPrefab;
    public GameObject floorPrefab;

    [Header("Roof Settings")]
    [Tooltip("Should a roof be generated on top of the maze?")]
    public bool generateRoof = false;
    [Tooltip("Optional custom roof prefab. Tiled like the floor.")]
    public GameObject roofPrefab;
    [Tooltip("Material to apply to the roof if no prefab is used.")]
    public Material roofMaterial;

    [Header("Lighting (Torches)")]
    [Tooltip("Optional torch prefab to attach to walls.")]
    public GameObject torchPrefab;
    [Tooltip("How often should a torch spawn? (0 = never, 1 = every cell)")]
    [Range(0f, 1f)] public float torchDensity = 0.1f;
    [Tooltip("How high up the wall the torch is placed.")]
    public float torchHeight = 1.2f;

    [Header("Markers")]
    [Tooltip("Toggle the floating Start and End text on or off")]
    public bool showStartEndText = true;

    [Header("Materials (For Primitives)")]
    [Tooltip("Material to apply if Wall Prefab is empty")]
    public Material wallMaterial;
    [Tooltip("Material to apply to the corner pillars. Uses Wall Material if left empty.")]
    public Material pillarMaterial;
    [Tooltip("Material to apply if Floor Prefab is empty")]
    public Material floorMaterial;

    [Header("Custom Prefab Settings")]
    [Tooltip("Adjust this if your custom walls are floating in the air or sunk into the ground.")]
    public float customWallYOffset = 0f;

    // A data structure to represent each cell in our maze grid
    private class Cell
    {
        public bool visited = false;
        
        // We track the walls. True means the wall exists, false means it's an open path.
        public bool topWall = true;    // +Z direction
        public bool rightWall = true;  // +X direction
        public bool bottomWall = true; // -Z direction
        public bool leftWall = true;   // -X direction
    }

    private Cell[,] grid;

    // Parent object for organizing maze pieces in the hierarchy
    private GameObject currentMazeParent;

    void Start()
    {
        GenerateMazeData();
        BuildMazeIntoScene();
    }

    /// <summary>
    /// Executes the Recursive Backtracker algorithm to carve paths through the grid.
    /// </summary>
    void GenerateMazeData()
    {
        // 1. Initialize the grid of cells
        grid = new Cell[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                grid[x, y] = new Cell();
            }
        }

        // 2. Setup the starting point and our tracker stack
        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        Vector2Int current = new Vector2Int(0, 0);
        grid[current.x, current.y].visited = true;
        stack.Push(current);

        // 3. Main algorithm loop
        while (stack.Count > 0)
        {
            current = stack.Pop();
            List<Vector2Int> unvisitedNeighbors = GetUnvisitedNeighbors(current);

            // If the current cell has unvisited neighbors
            if (unvisitedNeighbors.Count > 0)
            {
                // Push current cell back to the stack so we can backtrack to it later
                stack.Push(current);

                // Pick a random unvisited neighbor
                Vector2Int chosen = unvisitedNeighbors[Random.Range(0, unvisitedNeighbors.Count)];

                // Remove the walls between the current cell and the chosen neighbor
                RemoveWalls(current, chosen);

                // Mark the chosen neighbor as visited and push it to the stack
                grid[chosen.x, chosen.y].visited = true;
                stack.Push(chosen);
            }
        }
    }

    /// <summary>
    /// Checks the 4 adjacent cells (Top, Right, Bottom, Left) and returns those not yet visited.
    /// </summary>
    List<Vector2Int> GetUnvisitedNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();

        // Check Top
        if (cell.y + 1 < height && !grid[cell.x, cell.y + 1].visited) 
            neighbors.Add(new Vector2Int(cell.x, cell.y + 1));
        
        // Check Right
        if (cell.x + 1 < width && !grid[cell.x + 1, cell.y].visited) 
            neighbors.Add(new Vector2Int(cell.x + 1, cell.y));
        
        // Check Bottom
        if (cell.y - 1 >= 0 && !grid[cell.x, cell.y - 1].visited) 
            neighbors.Add(new Vector2Int(cell.x, cell.y - 1));
        
        // Check Left
        if (cell.x - 1 >= 0 && !grid[cell.x - 1, cell.y].visited) 
            neighbors.Add(new Vector2Int(cell.x - 1, cell.y));

        return neighbors;
    }

    /// <summary>
    /// Knocks down the shared wall between two adjacent cells.
    /// </summary>
    void RemoveWalls(Vector2Int a, Vector2Int b)
    {
        if (a.x == b.x) // Same X, meaning they are vertically adjacent
        {
            if (a.y < b.y) // b is above a
            {
                grid[a.x, a.y].topWall = false;
                grid[b.x, b.y].bottomWall = false;
            }
            else // b is below a
            {
                grid[a.x, a.y].bottomWall = false;
                grid[b.x, b.y].topWall = false;
            }
        }
        else // Same Y, meaning they are horizontally adjacent
        {
            if (a.x < b.x) // b is to the right of a
            {
                grid[a.x, a.y].rightWall = false;
                grid[b.x, b.y].leftWall = false;
            }
            else // b is to the left of a
            {
                grid[a.x, a.y].leftWall = false;
                grid[b.x, b.y].rightWall = false;
            }
        }
    }

    /// <summary>
    /// Translates the mathematical grid data into physical 3D GameObjects in the scene.
    /// </summary>
    void BuildMazeIntoScene()
    {
        // Create an empty parent object to keep the hierarchy clean
        currentMazeParent = new GameObject("Procedural Maze");
        currentMazeParent.transform.position = Vector3.zero;

        // Generate the Floor
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
            
            if (floorMaterial != null)
            {
                floor.GetComponent<MeshRenderer>().material = floorMaterial;
            }
            
            floor.transform.localScale = new Vector3((width * cellSize) / 10f, 1, (height * cellSize) / 10f);
            floor.transform.position = new Vector3(
                (width * cellSize) / 2f - (cellSize / 2f), 
                0, 
                (height * cellSize) / 2f - (cellSize / 2f)
            );
        }

        // Generate the Roof
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
                
                // Rotate the roof 180 degrees on the X axis so the textured face points DOWN into the maze
                roof.transform.rotation = Quaternion.Euler(180, 0, 0);
            }
        }

        // To perfectly prevent Z-fighting and holes, primitive walls use a "Pillars and Edges" system.
        Vector3 primitiveWallScale = new Vector3(cellSize - wallThickness, wallHeight, wallThickness);

        // 1. Generate Corner Pillars (Only for primitives)
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
                    
                    if (pillarMaterial != null)
                    {
                        pillar.GetComponent<MeshRenderer>().material = pillarMaterial;
                    }
                    else if (wallMaterial != null)
                    {
                        pillar.GetComponent<MeshRenderer>().material = wallMaterial;
                    }
                }
            }
        }

        // 2. Generate the Walls between the pillars
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 cellPos = new Vector3(x * cellSize, 0, y * cellSize);

                if (grid[x, y].topWall)
                    CreateWall(cellPos + new Vector3(0, wallHeight / 2f, cellSize / 2f), 
                                 Quaternion.identity, primitiveWallScale, currentMazeParent.transform);
                
                if (grid[x, y].rightWall)
                    CreateWall(cellPos + new Vector3(cellSize / 2f, wallHeight / 2f, 0), 
                                 Quaternion.Euler(0, 90, 0), primitiveWallScale, currentMazeParent.transform);

                if (y == 0 && grid[x, y].bottomWall)
                    CreateWall(cellPos + new Vector3(0, wallHeight / 2f, -cellSize / 2f), 
                                 Quaternion.identity, primitiveWallScale, currentMazeParent.transform);

                if (x == 0 && grid[x, y].leftWall)
                    CreateWall(cellPos + new Vector3(-cellSize / 2f, wallHeight / 2f, 0), 
                                 Quaternion.Euler(0, 90, 0), primitiveWallScale, currentMazeParent.transform);
            }
        }

        // 3. Spawn Torches
        if (torchPrefab != null)
        {
            SpawnTorches(currentMazeParent.transform);
        }

        // Add visual markers for the start and end of the maze
        CreateStartAndEndMarkers(currentMazeParent.transform);
    }

    /// <summary>
    /// Randomly attaches torches to the walls of the maze based on the Torch Density setting.
    /// </summary>
    void SpawnTorches(Transform parent)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Roll the dice to see if this cell gets a torch
                if (Random.value <= torchDensity)
                {
                    // Find all walls surrounding this specific cell
                    List<int> availableWalls = new List<int>();
                    if (grid[x, y].topWall) availableWalls.Add(0);
                    if (grid[x, y].rightWall) availableWalls.Add(1);
                    if (grid[x, y].bottomWall) availableWalls.Add(2);
                    if (grid[x, y].leftWall) availableWalls.Add(3);

                    // If the cell has at least one wall, pick one randomly to attach the torch to
                    if (availableWalls.Count > 0)
                    {
                        int chosenWall = availableWalls[Random.Range(0, availableWalls.Count)];
                        
                        Vector3 cellPos = new Vector3(x * cellSize, 0, y * cellSize);
                        Vector3 torchPos = Vector3.zero;
                        Quaternion torchRot = Quaternion.identity;

                        // Calculate how far from the center the wall surface is
                        float offsetToWall = (cellSize / 2f) - (wallThickness / 2f);

                        // Position the torch slightly away from the center, against the chosen wall
                        // We also rotate it so the "forward" direction faces into the center of the cell
                        if (chosenWall == 0) // Top
                        {
                            torchPos = cellPos + new Vector3(0, torchHeight, offsetToWall);
                            torchRot = Quaternion.Euler(0, 180, 0); 
                        }
                        else if (chosenWall == 1) // Right
                        {
                            torchPos = cellPos + new Vector3(offsetToWall, torchHeight, 0);
                            torchRot = Quaternion.Euler(0, 270, 0);
                        }
                        else if (chosenWall == 2) // Bottom
                        {
                            torchPos = cellPos + new Vector3(0, torchHeight, -offsetToWall);
                            torchRot = Quaternion.Euler(0, 0, 0);
                        }
                        else if (chosenWall == 3) // Left
                        {
                            torchPos = cellPos + new Vector3(-offsetToWall, torchHeight, 0);
                            torchRot = Quaternion.Euler(0, 90, 0);
                        }

                        // Apply the required rotation combined with the prefab's default import rotation
                        Instantiate(torchPrefab, torchPos, torchRot * torchPrefab.transform.rotation, parent);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Creates text markers for the start and end of the maze.
    /// </summary>
    void CreateStartAndEndMarkers(Transform parent)
    {
        if (!showStartEndText) return;

        // Start Marker Text at grid position (0, 0)
        CreateTextMarker(new Vector2Int(0, 0), "Start", Color.red, parent);

        // End Marker Text at grid position (width - 1, height - 1)
        CreateTextMarker(new Vector2Int(width - 1, height - 1), "End", Color.green, parent);
    }

    /// <summary>
    /// Instantiates a 3D Text object to mark a specific cell.
    /// </summary>
    void CreateTextMarker(Vector2Int gridPos, string text, Color color, Transform parent)
    {
        GameObject textObj = new GameObject(text + " Text");
        textObj.transform.SetParent(parent);
        
        // Position it floating slightly above the wall height
        textObj.transform.position = new Vector3(gridPos.x * cellSize, wallHeight + 0.5f, gridPos.y * cellSize);
        
        // Add a TextMesh component
        TextMesh textMesh = textObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.color = color;
        
        // High font size combined with low character size keeps the 3D text looking sharp
        textMesh.characterSize = 0.1f;
        textMesh.fontSize = 64; 
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
    }

    /// <summary>
    /// Instantiates and positions a wall segment. Custom prefabs are placed at floor level (Y=0) without forced scaling, 
    /// while primitive cubes are placed exactly in the vertical center.
    /// </summary>
    void CreateWall(Vector3 centerPosition, Quaternion rotation, Vector3 primitiveScale, Transform parent)
    {
        if (wallPrefab != null)
        {
            // Apply the required maze rotation on top of the prefab's natural imported rotation.
            Quaternion finalRotation = rotation * wallPrefab.transform.rotation;
            
            // Use customWallYOffset so you can easily fix floating/sinking walls in the inspector
            Vector3 prefabPosition = new Vector3(centerPosition.x, customWallYOffset, centerPosition.z);
            
            Instantiate(wallPrefab, prefabPosition, finalRotation, parent);
        }
        else
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            
            // Apply the custom material if one is assigned
            if (wallMaterial != null)
            {
                wall.GetComponent<MeshRenderer>().material = wallMaterial;
            }
            
            wall.transform.position = centerPosition;
            wall.transform.rotation = rotation;
            wall.transform.localScale = primitiveScale;
            wall.transform.SetParent(parent);
        }
    }
}