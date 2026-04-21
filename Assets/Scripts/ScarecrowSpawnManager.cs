using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ScarecrowSpawnManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject scarecrowDecoyPrefab;
    public GameObject scarecrowEnemyPrefab;

    [Header("References")]
    public Transform player;
    public Camera playerCamera;
    public Transform spawnCenter;
    public Transform spawnedParent;

    [Header("Spawn Area")]
    public int decoyCount = 14;
    public float spawnRadius = 85f;
    public float minDistanceFromPlayer = 14f;
    public float minDistanceBetweenScarecrows = 9f;
    public float navMeshSampleDistance = 10f;
    public float groundRaycastHeight = 80f;
    public LayerMask groundMask = ~0;
    public float spawnHeightOffset = 0f;
    public int placementAttempts = 350;

    [Header("Rotation")]
    public bool randomizeYRotation = true;

    [Header("Corn Avoidance")]
    public bool avoidCorn = true;
    public string[] cornNameKeywords = { "corn" };
    public float cornClearanceRadius = 5f;
    public LayerMask cornCollisionMask = ~0;

    private readonly List<GameObject> spawnedDecoys = new List<GameObject>();
    private readonly List<Renderer> cornRenderers = new List<Renderer>();
    private readonly List<TerrainCornDetailLayer> cornDetailLayers = new List<TerrainCornDetailLayer>();
    private readonly List<Vector3> cornTerrainTreePositions = new List<Vector3>();
    private ScarecrowEnemy spawnedEnemy;

    private struct TerrainCornDetailLayer
    {
        public Terrain terrain;
        public int layerIndex;
    }

    void Start()
    {
        ResolveReferences();
        CacheCornAvoidanceSources();
        SpawnScarecrows();
    }

    public void SpawnScarecrows()
    {
        if (scarecrowDecoyPrefab == null || scarecrowEnemyPrefab == null)
        {
            Debug.LogWarning("ScarecrowSpawnManager needs both scarecrow prefabs assigned.", this);
            return;
        }

        if (spawnedParent == null)
        {
            GameObject parentObject = new GameObject("Spawned Scarecrows");
            spawnedParent = parentObject.transform;
        }

        List<Vector3> spawnPositions = GenerateSpawnPositions();
        foreach (Vector3 position in spawnPositions)
        {
            Quaternion rotation = GetSpawnRotation();
            GameObject decoy = Instantiate(scarecrowDecoyPrefab, position, rotation, spawnedParent);
            spawnedDecoys.Add(decoy);
        }

        if (spawnedDecoys.Count == 0)
        {
            Debug.LogWarning("No valid scarecrow spawn points were found.", this);
            return;
        }

        GameObject startingDecoy = spawnedDecoys[Random.Range(0, spawnedDecoys.Count)];
        GameObject enemyObject = Instantiate(
            scarecrowEnemyPrefab,
            startingDecoy.transform.position,
            startingDecoy.transform.rotation,
            spawnedParent);

        spawnedEnemy = enemyObject.GetComponent<ScarecrowEnemy>();
        if (spawnedEnemy == null)
            spawnedEnemy = enemyObject.GetComponentInChildren<ScarecrowEnemy>();

        if (spawnedEnemy == null)
        {
            Debug.LogWarning("The enemy prefab does not contain a ScarecrowEnemy script.", this);
            return;
        }

        spawnedEnemy.player = player;
        spawnedEnemy.playerCamera = playerCamera;
        spawnedEnemy.ConfigureSwitchingTargets(spawnedDecoys, player, playerCamera);
        spawnedEnemy.MoveIntoDecoy(startingDecoy);
    }

    List<Vector3> GenerateSpawnPositions()
    {
        List<Vector3> positions = new List<Vector3>();

        for (int attempt = 0; attempt < placementAttempts && positions.Count < decoyCount; attempt++)
        {
            if (!TryGetRandomSpawnPoint(out Vector3 position))
                continue;

            if (!IsFarEnoughFromPlayer(position))
                continue;

            if (!IsFarEnoughFromOtherScarecrows(position, positions))
                continue;

            if (!IsClearOfCorn(position))
                continue;

            positions.Add(position);
        }

        return positions;
    }

    bool TryGetRandomSpawnPoint(out Vector3 position)
    {
        Vector3 center = spawnCenter != null ? spawnCenter.position : transform.position;
        Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
        Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
        {
            position = navHit.position + Vector3.up * spawnHeightOffset;
            return true;
        }

        Vector3 rayStart = randomPoint + Vector3.up * groundRaycastHeight;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit groundHit, groundRaycastHeight * 2f, groundMask))
        {
            position = groundHit.point + Vector3.up * spawnHeightOffset;
            return true;
        }

        position = Vector3.zero;
        return false;
    }

    bool IsClearOfCorn(Vector3 position)
    {
        if (!avoidCorn)
            return true;

        return !IsNearCornCollider(position) &&
               !IsInsideCornRendererBounds(position) &&
               !IsOnCornTerrainTree(position) &&
               !IsOnCornTerrainDetail(position);
    }

    bool IsNearCornCollider(Vector3 position)
    {
        Collider[] nearbyColliders = Physics.OverlapSphere(
            position + Vector3.up,
            cornClearanceRadius,
            cornCollisionMask,
            QueryTriggerInteraction.Collide);

        foreach (Collider nearbyCollider in nearbyColliders)
        {
            if (nearbyCollider != null && IsCornObject(nearbyCollider.transform))
                return true;
        }

        return false;
    }

    bool IsInsideCornRendererBounds(Vector3 position)
    {
        foreach (Renderer cornRenderer in cornRenderers)
        {
            if (cornRenderer == null)
                continue;

            Bounds bounds = cornRenderer.bounds;
            bounds.Expand(new Vector3(cornClearanceRadius * 2f, 0f, cornClearanceRadius * 2f));

            Vector3 boundsHeightPosition = new Vector3(position.x, bounds.center.y, position.z);
            if (bounds.Contains(boundsHeightPosition))
                return true;
        }

        return false;
    }

    bool IsOnCornTerrainDetail(Vector3 position)
    {
        foreach (TerrainCornDetailLayer cornDetailLayer in cornDetailLayers)
        {
            Terrain terrain = cornDetailLayer.terrain;
            if (terrain == null || terrain.terrainData == null)
                continue;

            TerrainData terrainData = terrain.terrainData;
            Vector3 localPosition = position - terrain.transform.position;

            if (localPosition.x < 0f || localPosition.z < 0f ||
                localPosition.x > terrainData.size.x || localPosition.z > terrainData.size.z)
                continue;

            int detailX = Mathf.FloorToInt((localPosition.x / terrainData.size.x) * terrainData.detailWidth);
            int detailZ = Mathf.FloorToInt((localPosition.z / terrainData.size.z) * terrainData.detailHeight);
            int cellRadius = Mathf.Max(
                1,
                Mathf.CeilToInt((cornClearanceRadius / terrainData.size.x) * terrainData.detailWidth));

            int xBase = Mathf.Clamp(detailX - cellRadius, 0, terrainData.detailWidth - 1);
            int zBase = Mathf.Clamp(detailZ - cellRadius, 0, terrainData.detailHeight - 1);
            int width = Mathf.Clamp((cellRadius * 2) + 1, 1, terrainData.detailWidth - xBase);
            int height = Mathf.Clamp((cellRadius * 2) + 1, 1, terrainData.detailHeight - zBase);

            int[,] details = terrainData.GetDetailLayer(xBase, zBase, width, height, cornDetailLayer.layerIndex);
            foreach (int detailDensity in details)
            {
                if (detailDensity > 0)
                    return true;
            }
        }

        return false;
    }

    bool IsOnCornTerrainTree(Vector3 position)
    {
        float clearanceSqr = cornClearanceRadius * cornClearanceRadius;
        foreach (Vector3 cornPosition in cornTerrainTreePositions)
        {
            Vector2 position2D = new Vector2(position.x, position.z);
            Vector2 cornPosition2D = new Vector2(cornPosition.x, cornPosition.z);
            if ((position2D - cornPosition2D).sqrMagnitude <= clearanceSqr)
                return true;
        }

        return false;
    }

    bool IsFarEnoughFromPlayer(Vector3 position)
    {
        if (player == null)
            return true;

        return Vector3.Distance(player.position, position) >= minDistanceFromPlayer;
    }

    bool IsFarEnoughFromOtherScarecrows(Vector3 position, List<Vector3> existingPositions)
    {
        foreach (Vector3 existingPosition in existingPositions)
        {
            if (Vector3.Distance(existingPosition, position) < minDistanceBetweenScarecrows)
                return false;
        }

        return true;
    }

    Quaternion GetSpawnRotation()
    {
        if (!randomizeYRotation)
            return Quaternion.identity;

        return Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
    }

    void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    void CacheCornAvoidanceSources()
    {
        cornRenderers.Clear();
        cornDetailLayers.Clear();
        cornTerrainTreePositions.Clear();

        if (!avoidCorn)
            return;

        foreach (Renderer sceneRenderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (sceneRenderer != null && IsCornObject(sceneRenderer.transform))
                cornRenderers.Add(sceneRenderer);
        }

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain == null || terrain.terrainData == null)
                continue;

            DetailPrototype[] detailPrototypes = terrain.terrainData.detailPrototypes;
            for (int i = 0; i < detailPrototypes.Length; i++)
            {
                DetailPrototype detailPrototype = detailPrototypes[i];
                if (IsCornName(detailPrototype.prototype != null ? detailPrototype.prototype.name : null) ||
                    IsCornName(detailPrototype.prototypeTexture != null ? detailPrototype.prototypeTexture.name : null))
                {
                    cornDetailLayers.Add(new TerrainCornDetailLayer
                    {
                        terrain = terrain,
                        layerIndex = i
                    });
                }
            }

            CacheCornTerrainTrees(terrain);
        }
    }

    void CacheCornTerrainTrees(Terrain terrain)
    {
        TerrainData terrainData = terrain.terrainData;
        TreePrototype[] treePrototypes = terrainData.treePrototypes;
        if (treePrototypes == null || treePrototypes.Length == 0)
            return;

        HashSet<int> cornPrototypeIndexes = new HashSet<int>();
        for (int i = 0; i < treePrototypes.Length; i++)
        {
            GameObject treePrefab = treePrototypes[i].prefab;
            if (treePrefab != null && IsCornName(treePrefab.name))
                cornPrototypeIndexes.Add(i);
        }

        if (cornPrototypeIndexes.Count == 0)
            return;

        foreach (TreeInstance treeInstance in terrainData.treeInstances)
        {
            if (!cornPrototypeIndexes.Contains(treeInstance.prototypeIndex))
                continue;

            Vector3 terrainSpacePosition = Vector3.Scale(treeInstance.position, terrainData.size);
            cornTerrainTreePositions.Add(terrain.transform.position + terrainSpacePosition);
        }
    }

    bool IsCornObject(Transform objectTransform)
    {
        Transform current = objectTransform;
        while (current != null)
        {
            if (IsCornName(current.name))
                return true;

            current = current.parent;
        }

        return false;
    }

    bool IsCornName(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || cornNameKeywords == null)
            return false;

        foreach (string keyword in cornNameKeywords)
        {
            if (!string.IsNullOrEmpty(keyword) &&
                objectName.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }
}
