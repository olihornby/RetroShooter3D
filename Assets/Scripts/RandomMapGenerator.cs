using System;
using UnityEngine;

/// <summary>
/// Generates a simple random shooter arena each time the scene starts.
/// Attach to an empty GameObject in the scene.
/// </summary>
public class RandomMapGenerator : MonoBehaviour
{
    [Header("Map Size")]
    [SerializeField] private int mapWidth = 24;
    [SerializeField] private int mapDepth = 24;
    [SerializeField] private float cellSize = 2f;

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 12345;
    [SerializeField, Range(0f, 0.6f)] private float wallChance = 0.2f;
    [SerializeField, Range(0f, 0.35f)] private float coverChance = 0.12f;
    [SerializeField] private int spawnClearRadius = 2;

    [Header("Block Settings")]
    [SerializeField] private float floorThickness = 0.5f;
    [SerializeField] private float wallHeight = 3f;
    [SerializeField] private float coverHeight = 1.2f;

    [Header("Enemies")]
    [SerializeField] private bool spawnEnemies = true;
    [SerializeField] private int enemyCount = 8;
    [SerializeField] private float enemyMoveSpeed = 2.8f;
    [SerializeField] private float enemyVisionRadius = 16f;
    [SerializeField] private LayerMask enemyVisionMask = ~0;
    [SerializeField] private float enemySpawnPadding = 2f;

    [Header("Colors")]
    [SerializeField] private Color floorColor = new Color(0.22f, 0.22f, 0.22f);
    [SerializeField] private Color wallColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color coverColor = new Color(0.45f, 0.45f, 0.45f);

    private Transform generatedRoot;
    private Material floorMaterial;
    private Material wallMaterial;
    private Material coverMaterial;
    private Material enemyMaterial;

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateMap();
        }
    }

    public void GenerateMap()
    {
        PrepareRandom();
        PrepareMaterials();
        ClearOldMap();

        bool[,] wallCells = CreateWallLayout();
        bool[,] coverCells = CreateCoverLayout(wallCells);

        BuildFloor();
        BuildWalls(wallCells);
        BuildCover(coverCells, wallCells);

        if (spawnEnemies)
        {
            SpawnEnemies(wallCells, coverCells);
        }
    }

    private void PrepareRandom()
    {
        if (useRandomSeed)
        {
            seed = Environment.TickCount;
        }

        UnityEngine.Random.InitState(seed);
    }

    private void PrepareMaterials()
    {
        floorMaterial = CreateRuntimeMaterial(floorColor);
        wallMaterial = CreateRuntimeMaterial(wallColor);
        coverMaterial = CreateRuntimeMaterial(coverColor);
        enemyMaterial = CreateRuntimeMaterial(new Color(0.75f, 0.18f, 0.18f));
    }

    private Material CreateRuntimeMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Glossiness", 0f);
        return material;
    }

    private void ClearOldMap()
    {
        Transform existing = transform.Find("GeneratedMap");
        if (existing != null)
        {
            Destroy(existing.gameObject);
        }

        GameObject root = new GameObject("GeneratedMap");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        generatedRoot = root.transform;
    }

    private bool[,] CreateWallLayout()
    {
        int width = Mathf.Max(8, mapWidth);
        int depth = Mathf.Max(8, mapDepth);

        bool[,] cells = new bool[width, depth];
        int centerX = width / 2;
        int centerZ = depth / 2;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                bool isBoundary = x == 0 || z == 0 || x == width - 1 || z == depth - 1;
                bool isSpawnZone = Mathf.Abs(x - centerX) <= spawnClearRadius && Mathf.Abs(z - centerZ) <= spawnClearRadius;

                if (isBoundary)
                {
                    cells[x, z] = true;
                    continue;
                }

                if (isSpawnZone)
                {
                    cells[x, z] = false;
                    continue;
                }

                cells[x, z] = UnityEngine.Random.value < wallChance;
            }
        }

        CarvePrimaryPaths(cells, centerX, centerZ);
        return cells;
    }

    private void CarvePrimaryPaths(bool[,] cells, int centerX, int centerZ)
    {
        int width = cells.GetLength(0);
        int depth = cells.GetLength(1);

        for (int x = 1; x < width - 1; x++)
        {
            cells[x, centerZ] = false;
        }

        for (int z = 1; z < depth - 1; z++)
        {
            cells[centerX, z] = false;
        }

        int walkerX = centerX;
        int walkerZ = centerZ;
        int steps = width + depth;

        for (int i = 0; i < steps; i++)
        {
            int direction = UnityEngine.Random.Range(0, 4);
            if (direction == 0) walkerX++;
            if (direction == 1) walkerX--;
            if (direction == 2) walkerZ++;
            if (direction == 3) walkerZ--;

            walkerX = Mathf.Clamp(walkerX, 1, width - 2);
            walkerZ = Mathf.Clamp(walkerZ, 1, depth - 2);
            cells[walkerX, walkerZ] = false;
        }
    }

    private bool[,] CreateCoverLayout(bool[,] wallCells)
    {
        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);
        int centerX = width / 2;
        int centerZ = depth / 2;

        bool[,] coverCells = new bool[width, depth];

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < depth - 1; z++)
            {
                if (wallCells[x, z])
                {
                    continue;
                }

                bool isSpawnZone = Mathf.Abs(x - centerX) <= spawnClearRadius && Mathf.Abs(z - centerZ) <= spawnClearRadius;
                if (isSpawnZone)
                {
                    continue;
                }

                coverCells[x, z] = UnityEngine.Random.value < coverChance;
            }
        }

        return coverCells;
    }

    private void BuildFloor()
    {
        float widthWorld = mapWidth * cellSize;
        float depthWorld = mapDepth * cellSize;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Floor";
        floor.transform.SetParent(generatedRoot);
        floor.transform.position = transform.position + new Vector3(0f, -floorThickness * 0.5f, 0f);
        floor.transform.localScale = new Vector3(widthWorld, floorThickness, depthWorld);

        floor.GetComponent<Renderer>().material = floorMaterial;
    }

    private void BuildWalls(bool[,] wallCells)
    {
        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                if (!wallCells[x, z])
                {
                    continue;
                }

                Vector3 worldPosition = CellToWorld(x, z, wallHeight * 0.5f);
                CreateBlock($"Wall_{x}_{z}", worldPosition, new Vector3(cellSize, wallHeight, cellSize), wallMaterial);
            }
        }
    }

    private void BuildCover(bool[,] coverCells, bool[,] wallCells)
    {
        int width = coverCells.GetLength(0);
        int depth = coverCells.GetLength(1);

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < depth - 1; z++)
            {
                if (!coverCells[x, z] || wallCells[x, z])
                {
                    continue;
                }

                Vector3 worldPosition = CellToWorld(x, z, coverHeight * 0.5f);
                Vector3 scale = new Vector3(cellSize * 0.85f, coverHeight, cellSize * 0.85f);
                CreateBlock($"Cover_{x}_{z}", worldPosition, scale, coverMaterial);
            }
        }
    }

    private void SpawnEnemies(bool[,] wallCells, bool[,] coverCells)
    {
        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);
        int centerX = width / 2;
        int centerZ = depth / 2;
        int spawnTotal = Mathf.Max(0, enemyCount);

        int maxAttempts = spawnTotal * 20;
        int spawned = 0;

        for (int attempt = 0; attempt < maxAttempts && spawned < spawnTotal; attempt++)
        {
            int x = UnityEngine.Random.Range(1, width - 1);
            int z = UnityEngine.Random.Range(1, depth - 1);

            if (wallCells[x, z] || coverCells[x, z])
            {
                continue;
            }

            if (Mathf.Abs(x - centerX) <= spawnClearRadius + 1 && Mathf.Abs(z - centerZ) <= spawnClearRadius + 1)
            {
                continue;
            }

            Vector3 worldPosition = CellToWorld(x, z, 0f);
            if (Vector3.Distance(worldPosition, transform.position) < enemySpawnPadding)
            {
                continue;
            }

            CreateEnemy($"Enemy_{spawned}", worldPosition);
            spawned++;
        }
    }

    private void CreateEnemy(string objectName, Vector3 worldPosition)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = objectName;
        enemy.transform.SetParent(generatedRoot);
        enemy.transform.position = worldPosition + Vector3.up;
        enemy.transform.localScale = new Vector3(1f, 1f, 1f);

        Renderer renderer = enemy.GetComponent<Renderer>();
        renderer.material = enemyMaterial;

        CharacterController controller = enemy.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.radius = 0.5f;
        controller.center = new Vector3(0f, 1f, 0f);

        Collider primitiveCollider = enemy.GetComponent<Collider>();
        if (primitiveCollider != null)
        {
            Destroy(primitiveCollider);
        }

        enemy.AddComponent<DamageableTarget>();
        EnemyAI enemyAi = enemy.AddComponent<EnemyAI>();
        enemyAi.Configure(enemyMoveSpeed, enemyVisionRadius, enemyVisionMask);
    }

    private void CreateBlock(string objectName, Vector3 worldPosition, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(generatedRoot);
        block.transform.position = worldPosition;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().material = material;
    }

    private Vector3 CellToWorld(int x, int z, float y)
    {
        float xStart = -((mapWidth - 1) * cellSize) * 0.5f;
        float zStart = -((mapDepth - 1) * cellSize) * 0.5f;

        return transform.position + new Vector3(
            xStart + x * cellSize,
            y,
            zStart + z * cellSize);
    }
}
