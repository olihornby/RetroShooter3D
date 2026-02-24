using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a large enclosed room-based map and room encounters each run.
/// Attach to an empty GameObject in the scene.
/// </summary>
public partial class RandomMapGenerator : MonoBehaviour
{
    private enum RoomArchetype
    {
        SmallEmpty,
        SmallEncounter,
        LargeEmpty,
        LargeEncounter,
        Staircase,
        HallwayParkour,
        VerticalParkour
    }

    [Header("Map Size")]
    [SerializeField] private int mapWidth = 256;
    [SerializeField] private int mapDepth = 256;
    [SerializeField] private float cellSize = 4f;

    [Header("Generation")]
    [SerializeField] private bool generateOnStart = true;
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed = 12345;
    [SerializeField, Range(6, 36)] private int desiredRoomCount = 14;
    [SerializeField, Range(4, 20)] private int minRoomSize = 8;
    [SerializeField, Range(5, 32)] private int maxRoomSize = 26;
    [SerializeField, Range(1, 4)] private int corridorWidth = 1;
    [SerializeField] private int spawnClearRadius = 5;
    [SerializeField, Range(0f, 0.6f)] private float coverChance = 0.07f;
    [SerializeField, Range(0f, 1f)] private float longCorridorChance = 0.65f;
    [SerializeField, Range(0f, 1f)] private float largeRoomBias = 0.72f;
    [SerializeField] private int maxCoverPerRoom = 3;

    [Header("Block Settings")]
    [SerializeField] private float floorThickness = 0.5f;
    [SerializeField] private float wallHeight = 18f;
    [SerializeField] private float ceilingThickness = 0.4f;
    [SerializeField, Range(2, 16)] private int ceilingTileSpanCells = 8;
    [SerializeField, Range(0f, 0.6f)] private float platformVoidChance = 0.22f;
    [SerializeField, Range(2, 8)] private int maxPlatformLevels = 4;
    [SerializeField] private float platformLevelHeight = 1.2f;
    [SerializeField, Range(0.5f, 0.98f)] private float platformFootprintMin = 0.62f;
    [SerializeField, Range(0.5f, 0.98f)] private float platformFootprintMax = 0.86f;
    [SerializeField] private float coverHeight = 1.2f;
    [SerializeField] private float floorBoxHeight = 0.7f;
    [SerializeField, Range(0f, 0.5f)] private float floorBoxChance = 0.08f;

    [Header("Room Features")]
    [SerializeField] private bool enableRoomFeatures = true;
    [SerializeField, Range(0f, 1f)] private float parkourRoomChance = 0.45f;
    [SerializeField, Range(0f, 1f)] private float tallRoomChance = 0.35f;
    [SerializeField] private float parkourStepHeight = 0.45f;
    [SerializeField] private int parkourStepCount = 5;
    [SerializeField] private float tallPlatformHeight = 9f;
    [SerializeField] private int staircaseCount = 4;
    [SerializeField, Range(2, 8)] private int maxFloors = 6;
    [SerializeField] private float floorLevelHeight = 4.2f;
    [SerializeField] private int stairStepsPerFloor = 5;

    [Header("Room Encounters")]
    [SerializeField] private bool spawnEnemies = true;
    [SerializeField] private int minEnemiesPerRoom = 2;
    [SerializeField] private int maxEnemiesPerRoom = 5;
    [SerializeField] private float barrierThickness = 0.45f;

    [Header("Player Spawn")]
    [SerializeField] private bool autoPositionPlayer = true;
    [SerializeField] private float fallbackPlayerSpawnHeight = 1f;

    [Header("Enemy Vision")]
    [SerializeField] private LayerMask enemyVisionMask = ~0;

    [Header("Layer Setup")]
    [SerializeField] private string generatedGroundLayerName = "Ground";

    [Header("Colors")]
    [SerializeField] private Color floorColor = new Color(0.22f, 0.22f, 0.22f);
    [SerializeField] private Color wallColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color coverColor = new Color(0.45f, 0.45f, 0.45f);

    [Header("Enemy Variants")]
    [SerializeField] private List<EnemyVariant> enemyVariants = new List<EnemyVariant>();

    private Transform generatedRoot;
    private Material floorMaterial;
    private Material wallMaterial;
    private Material coverMaterial;
    private int generatedGroundLayer = -1;
    private bool hasPendingPlayerSpawn;
    private Vector3 pendingSpawnBase;
    private int pendingSpawnAttempts;
    private const int MaxSpawnAttempts = 180;
    private HashSet<Vector2Int> staircaseShaftCells = new HashSet<Vector2Int>();
    private bool[,] platformCells;
    private float[,] platformTopHeights;
    private readonly Dictionary<int, RoomArchetype> roomArchetypes = new Dictionary<int, RoomArchetype>();

    [Serializable]
    private class EnemyVariant
    {
        public string name = "Grunt";
        public PrimitiveType shape = PrimitiveType.Capsule;
        public Color color = new Color(0.75f, 0.18f, 0.18f);
        public float spawnWeight = 1f;
        public Vector2 sizeRange = new Vector2(0.9f, 1.1f);
        public float health = 70f;
        public float moveSpeed = 2.8f;
        public float visionRadius = 16f;
        public float contactDamage = 10f;
        public float attackRange = 1.45f;
        public float attackCooldown = 0.9f;
    }

    private struct Room
    {
        public int MinX;
        public int MinZ;
        public int MaxX;
        public int MaxZ;

        public Room(int minX, int minZ, int maxX, int maxZ)
        {
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public int CenterX => (MinX + MaxX) / 2;
        public int CenterZ => (MinZ + MaxZ) / 2;

        public bool Intersects(Room other, int padding)
        {
            return !(MaxX + padding < other.MinX ||
                     MinX - padding > other.MaxX ||
                     MaxZ + padding < other.MinZ ||
                     MinZ - padding > other.MaxZ);
        }

        public bool Contains(int x, int z)
        {
            return x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;
        }

        public int Width => MaxX - MinX + 1;
        public int Depth => MaxZ - MinZ + 1;
    }

    private enum DoorDirection
    {
        North,
        South,
        East,
        West
    }

    private struct Doorway
    {
        public int X;
        public int Z;
        public DoorDirection Direction;

        public Doorway(int x, int z, DoorDirection direction)
        {
            X = x;
            Z = z;
            Direction = direction;
        }
    }

    private void Start()
    {
        if (generateOnStart)
        {
            GenerateMap();
        }
    }

    private void Update()
    {
        if (!hasPendingPlayerSpawn)
        {
            return;
        }

        if (TryPositionPlayerAtSpawn(pendingSpawnBase))
        {
            hasPendingPlayerSpawn = false;
            return;
        }

        pendingSpawnAttempts++;
        if (pendingSpawnAttempts >= MaxSpawnAttempts)
        {
            hasPendingPlayerSpawn = false;
            Debug.LogWarning("Auto player spawn timed out: PlayerController was not found in time.");
        }
    }

    public void GenerateMap()
    {
        staircaseShaftCells.Clear();

        PrepareRandom();
        PrepareMaterials();
        ResolveLayers();
        EnsureDefaultVariants();
        ClearOldMap();

        List<Room> rooms;
        bool[,] wallCells = CreateRoomBasedWallLayout(out rooms);
        Room spawnRoom = rooms[0];
        BuildRoomArchetypePlan(rooms, spawnRoom);
        bool[,] coverCells = CreateCoverLayout(wallCells, spawnRoom, rooms);

        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);

        BuildPlatformFloor(wallCells, rooms, spawnRoom);
        BuildWalls(wallCells);
        BuildParkourRoomWalls(rooms, wallCells);
        BuildCover(coverCells, wallCells);

        if (enableRoomFeatures)
        {
            BuildRoomFeatures(rooms, spawnRoom, wallCells, coverCells);
        }

        BuildCeiling(width, depth);

        if (autoPositionPlayer)
        {
            QueuePlayerSpawn(spawnRoom, width, depth);
        }

        if (spawnEnemies)
        {
            CreateRoomEncounters(rooms, wallCells, coverCells, spawnRoom);
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
    }

    private Material CreateRuntimeMaterial(Color color)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Glossiness", 0f);
        return material;
    }

    private void ResolveLayers()
    {
        generatedGroundLayer = LayerMask.NameToLayer(generatedGroundLayerName);
    }

    private void EnsureDefaultVariants()
    {
        if (enemyVariants != null && enemyVariants.Count > 0)
        {
            return;
        }

        enemyVariants = new List<EnemyVariant>
        {
            new EnemyVariant
            {
                name = "Grunt",
                shape = PrimitiveType.Capsule,
                color = new Color(0.75f, 0.22f, 0.22f),
                spawnWeight = 1.6f,
                sizeRange = new Vector2(0.9f, 1.1f),
                health = 70f,
                moveSpeed = 2.8f,
                visionRadius = 16f,
                contactDamage = 10f,
                attackRange = 1.4f,
                attackCooldown = 0.9f
            },
            new EnemyVariant
            {
                name = "Runner",
                shape = PrimitiveType.Sphere,
                color = new Color(0.9f, 0.65f, 0.2f),
                spawnWeight = 1.0f,
                sizeRange = new Vector2(0.65f, 0.9f),
                health = 45f,
                moveSpeed = 4.2f,
                visionRadius = 18f,
                contactDamage = 7f,
                attackRange = 1.25f,
                attackCooldown = 0.55f
            },
            new EnemyVariant
            {
                name = "Brute",
                shape = PrimitiveType.Cube,
                color = new Color(0.45f, 0.18f, 0.75f),
                spawnWeight = 0.7f,
                sizeRange = new Vector2(1.2f, 1.5f),
                health = 140f,
                moveSpeed = 1.9f,
                visionRadius = 14f,
                contactDamage = 18f,
                attackRange = 1.8f,
                attackCooldown = 1.25f
            }
        };
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

    private bool[,] CreateRoomBasedWallLayout(out List<Room> rooms)
    {
        int width = Mathf.Max(96, mapWidth);
        int depth = Mathf.Max(96, mapDepth);
        bool[,] cells = new bool[width, depth];
        rooms = new List<Room>();

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                cells[x, z] = true;
            }
        }

        int safeMaxRoomSize = Mathf.Max(minRoomSize, maxRoomSize);
        int spawnSize = Mathf.Clamp(Mathf.Max(spawnClearRadius * 2, minRoomSize + 1), minRoomSize, safeMaxRoomSize + 2);
        int centerX = width / 2;
        int centerZ = depth / 2;
        Room spawnRoom = CreateRoomAround(centerX, centerZ, spawnSize, spawnSize, width, depth);
        CarveRoom(cells, spawnRoom);
        rooms.Add(spawnRoom);

        int targetRooms = Mathf.Clamp(Mathf.Max(14, desiredRoomCount), 14, 48);
        int attempts = targetRooms * 80;

        for (int attempt = 0; attempt < attempts && rooms.Count < targetRooms; attempt++)
        {
            int roomWidth = GetBiasedRoomSize(minRoomSize, safeMaxRoomSize);
            int roomDepth = GetBiasedRoomSize(minRoomSize, safeMaxRoomSize);

            int minX = UnityEngine.Random.Range(1, width - roomWidth - 1);
            int minZ = UnityEngine.Random.Range(1, depth - roomDepth - 1);
            Room candidate = new Room(minX, minZ, minX + roomWidth - 1, minZ + roomDepth - 1);

            bool overlaps = false;
            for (int index = 0; index < rooms.Count; index++)
            {
                if (candidate.Intersects(rooms[index], 1))
                {
                    overlaps = true;
                    break;
                }
            }

            if (overlaps)
            {
                continue;
            }

            Room previous = rooms[rooms.Count - 1];
            CarveRoom(cells, candidate);
            CarveCorridor(cells, previous.CenterX, previous.CenterZ, candidate.CenterX, candidate.CenterZ, corridorWidth);
            rooms.Add(candidate);
        }

        if (rooms.Count < targetRooms)
        {
            int fallbackAttempts = (targetRooms - rooms.Count) * 140;
            for (int attempt = 0; attempt < fallbackAttempts && rooms.Count < targetRooms; attempt++)
            {
                int roomWidth = UnityEngine.Random.Range(Mathf.Max(6, minRoomSize - 2), Mathf.Max(minRoomSize, maxRoomSize - 2) + 1);
                int roomDepth = UnityEngine.Random.Range(Mathf.Max(6, minRoomSize - 2), Mathf.Max(minRoomSize, maxRoomSize - 2) + 1);

                int minX = UnityEngine.Random.Range(1, width - roomWidth - 1);
                int minZ = UnityEngine.Random.Range(1, depth - roomDepth - 1);
                Room candidate = new Room(minX, minZ, minX + roomWidth - 1, minZ + roomDepth - 1);

                bool overlaps = false;
                for (int index = 0; index < rooms.Count; index++)
                {
                    if (candidate.Intersects(rooms[index], 0))
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (overlaps)
                {
                    continue;
                }

                int nearestIndex = 0;
                int nearestDistance = int.MaxValue;
                for (int index = 0; index < rooms.Count; index++)
                {
                    Room existing = rooms[index];
                    int distance = Mathf.Abs(existing.CenterX - candidate.CenterX) + Mathf.Abs(existing.CenterZ - candidate.CenterZ);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestIndex = index;
                    }
                }

                Room nearest = rooms[nearestIndex];
                CarveRoom(cells, candidate);
                CarveCorridor(cells, nearest.CenterX, nearest.CenterZ, candidate.CenterX, candidate.CenterZ, corridorWidth);
                rooms.Add(candidate);
            }
        }

        for (int x = 0; x < width; x++)
        {
            cells[x, 0] = true;
            cells[x, depth - 1] = true;
        }

        for (int z = 0; z < depth; z++)
        {
            cells[0, z] = true;
            cells[width - 1, z] = true;
        }

        return cells;
    }

    private int GetBiasedRoomSize(int minSize, int maxSize)
    {
        int clampedMin = Mathf.Max(4, minSize);
        int clampedMax = Mathf.Max(clampedMin + 1, maxSize);
        int split = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(clampedMin, clampedMax, 0.55f)), clampedMin, clampedMax);

        if (UnityEngine.Random.value < largeRoomBias)
        {
            return UnityEngine.Random.Range(split, clampedMax + 1);
        }

        return UnityEngine.Random.Range(clampedMin, split + 1);
    }

    private Room CreateRoomAround(int centerX, int centerZ, int width, int depth, int mapCellWidth, int mapCellDepth)
    {
        int halfWidth = width / 2;
        int halfDepth = depth / 2;
        int minX = Mathf.Clamp(centerX - halfWidth, 1, mapCellWidth - 2 - width);
        int minZ = Mathf.Clamp(centerZ - halfDepth, 1, mapCellDepth - 2 - depth);
        int maxX = minX + width - 1;
        int maxZ = minZ + depth - 1;
        return new Room(minX, minZ, maxX, maxZ);
    }

    private void CarveRoom(bool[,] cells, Room room)
    {
        for (int x = room.MinX; x <= room.MaxX; x++)
        {
            for (int z = room.MinZ; z <= room.MaxZ; z++)
            {
                cells[x, z] = false;
            }
        }
    }

    private void CarveCorridor(bool[,] cells, int startX, int startZ, int endX, int endZ, int width)
    {
        int radius = Mathf.Max(0, width - 1);

        if (UnityEngine.Random.value < longCorridorChance)
        {
            GenerateLongCorridor(cells, startX, startZ, endX, endZ, radius);
            return;
        }

        GenerateCorridor(cells, startX, startZ, endX, endZ, radius);
    }

    private void CarveLine(bool[,] cells, int startX, int startZ, int endX, int endZ, int radius)
    {
        int x = startX;
        int z = startZ;

        while (x != endX)
        {
            CarveCircle(cells, x, z, radius);
            x += x < endX ? 1 : -1;
        }

        while (z != endZ)
        {
            CarveCircle(cells, x, z, radius);
            z += z < endZ ? 1 : -1;
        }

        CarveCircle(cells, endX, endZ, radius);
    }

    private void CarveCircle(bool[,] cells, int centerX, int centerZ, int radius)
    {
        int width = cells.GetLength(0);
        int depth = cells.GetLength(1);

        for (int x = centerX - radius; x <= centerX + radius; x++)
        {
            for (int z = centerZ - radius; z <= centerZ + radius; z++)
            {
                if (x < 1 || z < 1 || x >= width - 1 || z >= depth - 1)
                {
                    continue;
                }

                cells[x, z] = false;
            }
        }
    }

    private bool[,] CreateCoverLayout(bool[,] wallCells, Room spawnRoom, List<Room> rooms)
    {
        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);
        bool[,] coverCells = new bool[width, depth];

        return coverCells;
    }

    private void ApplyCoverLimitPerRoom(bool[,] coverCells, List<Room> rooms, Room spawnRoom)
    {
        int limit = Mathf.Max(0, maxCoverPerRoom);
        for (int roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
        {
            Room room = rooms[roomIndex];
            if (room.Intersects(spawnRoom, 0))
            {
                continue;
            }

            List<Vector2Int> roomCoverCells = new List<Vector2Int>();
            for (int x = room.MinX + 1; x < room.MaxX; x++)
            {
                for (int z = room.MinZ + 1; z < room.MaxZ; z++)
                {
                    if (coverCells[x, z])
                    {
                        roomCoverCells.Add(new Vector2Int(x, z));
                    }
                }
            }

            if (roomCoverCells.Count <= limit)
            {
                continue;
            }

            for (int i = roomCoverCells.Count - 1; i > 0; i--)
            {
                int randomIndex = UnityEngine.Random.Range(0, i + 1);
                Vector2Int temp = roomCoverCells[i];
                roomCoverCells[i] = roomCoverCells[randomIndex];
                roomCoverCells[randomIndex] = temp;
            }

            for (int i = limit; i < roomCoverCells.Count; i++)
            {
                Vector2Int cell = roomCoverCells[i];
                coverCells[cell.x, cell.y] = false;
            }
        }
    }

    private void BuildPlatformFloor(bool[,] wallCells, List<Room> rooms, Room spawnRoom)
    {
        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);

        platformCells = new bool[width, depth];
        platformTopHeights = new float[width, depth];

        int[,] levels = new int[width, depth];
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                levels[x, z] = -1;
            }
        }

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < depth - 1; z++)
            {
                if (wallCells[x, z])
                {
                    continue;
                }
                levels[x, z] = 0;
            }
        }

        for (int roomIndex = 1; roomIndex < rooms.Count; roomIndex++)
        {
            Room room = rooms[roomIndex];
            if (!roomArchetypes.TryGetValue(roomIndex, out RoomArchetype archetype))
            {
                continue;
            }

            switch (archetype)
            {
                case RoomArchetype.SmallEmpty:
                    ApplySmallEmptyRoom(room, levels);
                    break;
                case RoomArchetype.SmallEncounter:
                    ApplySmallEncounterRoom(room, levels);
                    break;
                case RoomArchetype.LargeEmpty:
                    ApplyLargeEmptyRoom(room, levels);
                    break;
                case RoomArchetype.LargeEncounter:
                    ApplyLargeEncounterRoom(room, levels);
                    break;
                case RoomArchetype.Staircase:
                    ApplyStaircaseRoom(room, levels);
                    break;
                case RoomArchetype.HallwayParkour:
                    ApplyHallwayParkourRoom(room, levels);
                    break;
                case RoomArchetype.VerticalParkour:
                    ApplyVerticalParkourRoom(room, levels);
                    break;
            }
        }

        for (int pass = 0; pass < 3; pass++)
        {
            for (int x = 1; x < width - 1; x++)
            {
                for (int z = 1; z < depth - 1; z++)
                {
                    if (levels[x, z] < 0)
                    {
                        continue;
                    }

                    int minNeighbor = int.MaxValue;
                    bool hasNeighbor = false;
                    for (int direction = 0; direction < 4; direction++)
                    {
                        int nx = x + (direction == 0 ? 1 : direction == 1 ? -1 : 0);
                        int nz = z + (direction == 2 ? 1 : direction == 3 ? -1 : 0);
                        if (levels[nx, nz] >= 0)
                        {
                            minNeighbor = Mathf.Min(minNeighbor, levels[nx, nz]);
                            hasNeighbor = true;
                        }
                    }

                    if (hasNeighbor && levels[x, z] > minNeighbor + 1)
                    {
                        levels[x, z] = minNeighbor + 1;
                    }
                }
            }
        }

        float minFootprint = Mathf.Clamp(Mathf.Min(platformFootprintMin, platformFootprintMax), 0.5f, 0.98f);
        float maxFootprint = Mathf.Clamp(Mathf.Max(platformFootprintMin, platformFootprintMax), minFootprint, 0.98f);

        for (int x = 1; x < width - 1; x++)
        {
            for (int z = 1; z < depth - 1; z++)
            {
                int level = levels[x, z];
                if (level < 0)
                {
                    continue;
                }

                float topY = level * platformLevelHeight;
                float centerY = topY - floorThickness * 0.5f;
                float footprint = cellSize * UnityEngine.Random.Range(minFootprint, maxFootprint);

                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"Platform_{x}_{z}";
                tile.transform.SetParent(generatedRoot);
                tile.transform.position = CellToWorld(x, z, centerY, width, depth);
                tile.transform.localScale = new Vector3(footprint, floorThickness, footprint);
                ApplyGroundLayer(tile);
                tile.GetComponent<Renderer>().material = floorMaterial;

                platformCells[x, z] = true;
                platformTopHeights[x, z] = topY;
            }
        }

        EnsureSpawnPlatforms(width, depth, spawnRoom);
    }

    private void EnsureSpawnPlatforms(int width, int depth, Room spawnRoom)
    {
        int minX = Mathf.Clamp(spawnRoom.CenterX - 1, 1, width - 2);
        int maxX = Mathf.Clamp(spawnRoom.CenterX + 1, 1, width - 2);
        int minZ = Mathf.Clamp(spawnRoom.CenterZ - 1, 1, depth - 2);
        int maxZ = Mathf.Clamp(spawnRoom.CenterZ + 1, 1, depth - 2);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (platformCells[x, z])
                {
                    continue;
                }

                float centerY = -floorThickness * 0.5f;
                GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tile.name = $"SpawnPlatform_{x}_{z}";
                tile.transform.SetParent(generatedRoot);
                tile.transform.position = CellToWorld(x, z, centerY, width, depth);
                tile.transform.localScale = new Vector3(cellSize * 0.9f, floorThickness, cellSize * 0.9f);
                ApplyGroundLayer(tile);
                tile.GetComponent<Renderer>().material = floorMaterial;

                platformCells[x, z] = true;
                platformTopHeights[x, z] = 0f;
            }
        }
    }

    private void BuildCeiling(int width, int depth)
    {
        int tileSpan = Mathf.Clamp(ceilingTileSpanCells, 2, 16);
        float topY = wallHeight + ceilingThickness * 0.5f;

        for (int startX = 0; startX < width; startX += tileSpan)
        {
            for (int startZ = 0; startZ < depth; startZ += tileSpan)
            {
                int endX = Mathf.Min(width - 1, startX + tileSpan - 1);
                int endZ = Mathf.Min(depth - 1, startZ + tileSpan - 1);

                if (TileContainsStairShaft(startX, endX, startZ, endZ))
                {
                    continue;
                }

                int spanX = endX - startX + 1;
                int spanZ = endZ - startZ + 1;

                Vector3 tileCenter = CellToWorld(startX + spanX / 2, startZ + spanZ / 2, topY, width, depth);
                Vector3 tileScale = new Vector3(spanX * cellSize, ceilingThickness, spanZ * cellSize);

                GameObject ceilingTile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ceilingTile.name = $"Ceiling_{startX}_{startZ}";
                ceilingTile.transform.SetParent(generatedRoot);
                ceilingTile.transform.position = tileCenter;
                ceilingTile.transform.localScale = tileScale;
                ApplyGroundLayer(ceilingTile);
                ceilingTile.GetComponent<Renderer>().material = wallMaterial;
            }
        }
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

                Vector3 worldPosition = CellToWorld(x, z, wallHeight * 0.5f, width, depth);
                CreateBlock($"Wall_{x}_{z}", worldPosition, new Vector3(cellSize, wallHeight, cellSize), wallMaterial);
            }
        }
    }

    private void BuildCover(bool[,] coverCells, bool[,] wallCells)
    {
        // Intentionally disabled for now (user-requested no floor boxes/cover).
    }

    private void BuildRoomFeatures(List<Room> rooms, Room spawnRoom, bool[,] wallCells, bool[,] coverCells)
    {
        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);

        for (int index = 1; index < rooms.Count; index++)
        {
            Room room = rooms[index];
            if (room.Width < 6 || room.Depth < 6)
            {
                continue;
            }

            if (!roomArchetypes.TryGetValue(index, out RoomArchetype archetype))
            {
                continue;
            }

            if (archetype == RoomArchetype.Staircase)
            {
                BuildStaircaseRoom(room, wallCells, coverCells, width, depth);
                continue;
            }

            if (archetype == RoomArchetype.HallwayParkour)
            {
                BuildParkourRoom(room, wallCells, coverCells, width, depth);
            }
            else if (archetype == RoomArchetype.VerticalParkour)
            {
                BuildTallRoom(room, wallCells, coverCells, width, depth);
            }
        }
    }

    private void BuildStaircaseRoom(Room room, bool[,] wallCells, bool[,] coverCells, int width, int depth)
    {
        bool alongX = room.Width >= room.Depth;

        int laneCapacity = alongX ? room.Depth - 2 : room.Width - 2;
        if (laneCapacity < 2)
        {
            return;
        }

        int floors = Mathf.Clamp(maxFloors, 3, 8);
        floors = Mathf.Clamp(Mathf.Min(floors, laneCapacity), 2, 8);

        int primaryRun = alongX ? room.Width - 2 : room.Depth - 2;
        int stepsPerFloor = Mathf.Clamp(stairStepsPerFloor, 4, 8);
        stepsPerFloor = Mathf.Clamp(Mathf.Min(stepsPerFloor, primaryRun), 3, 8);

        int minPrimary = alongX ? room.MinX + 1 : room.MinZ + 1;
        int maxPrimary = alongX ? room.MaxX - 1 : room.MaxZ - 1;
        int minLane = alongX ? room.MinZ + 1 : room.MinX + 1;

        int laneDirection = UnityEngine.Random.value < 0.5f ? 1 : -1;
        int laneStart = laneDirection > 0 ? minLane : minLane + floors - 1;

        for (int floor = 1; floor < floors; floor++)
        {
            float levelHeight = floorLevelHeight * floor;
            int lane = laneStart + (floor - 1) * laneDirection;
            bool forward = floor % 2 == 1;

            int lastX = alongX ? (forward ? maxPrimary : minPrimary) : lane;
            int lastZ = alongX ? lane : (forward ? maxPrimary : minPrimary);

            for (int step = 0; step < stepsPerFloor; step++)
            {
                float t = (step + 1f) / stepsPerFloor;
                int primary = forward
                    ? Mathf.RoundToInt(Mathf.Lerp(minPrimary, maxPrimary, t))
                    : Mathf.RoundToInt(Mathf.Lerp(maxPrimary, minPrimary, t));

                int x = alongX ? primary : lane;
                int z = alongX ? lane : primary;

                if (x <= room.MinX || x >= room.MaxX || z <= room.MinZ || z >= room.MaxZ)
                {
                    continue;
                }

                if (wallCells[x, z])
                {
                    continue;
                }

                float stepHeight = Mathf.Lerp(levelHeight - floorLevelHeight + 0.6f, levelHeight, t);
                stepHeight = Mathf.Clamp(stepHeight, 0.6f, wallHeight - 1f);

                Vector3 worldPosition = CellToWorld(x, z, stepHeight * 0.5f, width, depth);
                Vector3 scale = new Vector3(cellSize * 0.72f, stepHeight, cellSize * 0.72f);
                CreateBlock($"Staircase_{floor}_{x}_{z}", worldPosition, scale, coverMaterial);
                coverCells[x, z] = true;
                RegisterPlatformCellHeight(x, z, stepHeight);
                MarkStairShaftCell(x, z, width, depth);

                lastX = x;
                lastZ = z;
            }

            int nextLane = lane + laneDirection;
            if (nextLane > minLane - 1 && nextLane < minLane + laneCapacity)
            {
                int landingX = alongX ? lastX : nextLane;
                int landingZ = alongX ? nextLane : lastZ;

                if (landingX > room.MinX && landingX < room.MaxX && landingZ > room.MinZ && landingZ < room.MaxZ && !wallCells[landingX, landingZ])
                {
                    float platformHeight = Mathf.Clamp(levelHeight, 0.8f, wallHeight - 0.8f);
                    Vector3 worldPosition = CellToWorld(landingX, landingZ, platformHeight * 0.5f, width, depth);
                    Vector3 scale = new Vector3(cellSize * 0.8f, platformHeight, cellSize * 0.8f);
                    CreateBlock($"Landing_{floor}_{landingX}_{landingZ}", worldPosition, scale, coverMaterial);
                    coverCells[landingX, landingZ] = true;
                    RegisterPlatformCellHeight(landingX, landingZ, platformHeight);
                    MarkStairShaftCell(landingX, landingZ, width, depth);
                }
            }
        }
    }

    private void RegisterPlatformCellHeight(int x, int z, float topY)
    {
        if (platformCells == null || platformTopHeights == null)
        {
            return;
        }

        int width = platformCells.GetLength(0);
        int depth = platformCells.GetLength(1);
        if (x < 0 || z < 0 || x >= width || z >= depth)
        {
            return;
        }

        platformCells[x, z] = true;
        platformTopHeights[x, z] = Mathf.Max(platformTopHeights[x, z], topY);
    }

    private void MarkStairShaftCell(int x, int z, int width, int depth)
    {
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                int cellX = Mathf.Clamp(x + offsetX, 0, width - 1);
                int cellZ = Mathf.Clamp(z + offsetZ, 0, depth - 1);
                staircaseShaftCells.Add(new Vector2Int(cellX, cellZ));
            }
        }
    }

    private bool TileContainsStairShaft(int startX, int endX, int startZ, int endZ)
    {
        foreach (Vector2Int cell in staircaseShaftCells)
        {
            if (cell.x >= startX && cell.x <= endX && cell.y >= startZ && cell.y <= endZ)
            {
                return true;
            }
        }

        return false;
    }

    private void BuildParkourRoom(Room room, bool[,] wallCells, bool[,] coverCells, int width, int depth)
    {
        bool alongX = room.Width >= room.Depth;
        int steps = Mathf.Clamp(parkourStepCount, 3, 8);

        int startX = alongX ? room.MinX + 1 : room.CenterX;
        int startZ = alongX ? room.CenterZ : room.MinZ + 1;
        int direction = UnityEngine.Random.value < 0.5f ? 1 : -1;

        for (int step = 0; step < steps; step++)
        {
            int x = alongX ? startX + step * direction : startX;
            int z = alongX ? startZ : startZ + step * direction;

            if (x <= room.MinX || x >= room.MaxX || z <= room.MinZ || z >= room.MaxZ)
            {
                break;
            }

            if (wallCells[x, z] || coverCells[x, z])
            {
                continue;
            }

            float stepHeight = Mathf.Clamp(coverHeight + step * parkourStepHeight, coverHeight, wallHeight - 0.8f);
            Vector3 worldPosition = CellToWorld(x, z, stepHeight * 0.5f, width, depth);
            Vector3 scale = new Vector3(cellSize * 0.7f, stepHeight, cellSize * 0.7f);
            CreateBlock($"ParkourStep_{x}_{z}", worldPosition, scale, coverMaterial);
            coverCells[x, z] = true;
            RegisterPlatformCellHeight(x, z, stepHeight);
        }
    }

    private void BuildTallRoom(Room room, bool[,] wallCells, bool[,] coverCells, int width, int depth)
    {
        int centerX = room.CenterX;
        int centerZ = room.CenterZ;

        for (int x = centerX - 1; x <= centerX + 1; x++)
        {
            for (int z = centerZ - 1; z <= centerZ + 1; z++)
            {
                if (x <= room.MinX || x >= room.MaxX || z <= room.MinZ || z >= room.MaxZ)
                {
                    continue;
                }

                if (wallCells[x, z] || coverCells[x, z])
                {
                    continue;
                }

                Vector3 worldPosition = CellToWorld(x, z, tallPlatformHeight * 0.5f, width, depth);
                Vector3 scale = new Vector3(cellSize * 0.85f, tallPlatformHeight, cellSize * 0.85f);
                CreateBlock($"TallPlatform_{x}_{z}", worldPosition, scale, coverMaterial);
                coverCells[x, z] = true;
                RegisterPlatformCellHeight(x, z, tallPlatformHeight);
            }
        }

        int stairStartX = room.MinX + 1;
        int stairZ = room.CenterZ;
        int stairSteps = Mathf.Min(4, room.Width - 2);
        for (int step = 0; step < stairSteps; step++)
        {
            int x = stairStartX + step;
            if (x >= room.MaxX)
            {
                break;
            }

            if (wallCells[x, stairZ] || coverCells[x, stairZ])
            {
                continue;
            }

            float stepHeight = Mathf.Lerp(0.6f, tallPlatformHeight, (step + 1f) / stairSteps);
            Vector3 worldPosition = CellToWorld(x, stairZ, stepHeight * 0.5f, width, depth);
            Vector3 scale = new Vector3(cellSize * 0.7f, stepHeight, cellSize * 0.7f);
            CreateBlock($"TallStair_{x}_{stairZ}", worldPosition, scale, coverMaterial);
            coverCells[x, stairZ] = true;
            RegisterPlatformCellHeight(x, stairZ, stepHeight);
        }
    }

    private void BuildVariedCoverRoom(Room room, Room spawnRoom, bool[,] wallCells, bool[,] coverCells, int width, int depth)
    {
        if (room.Intersects(spawnRoom, 0))
        {
            return;
        }

        int localCoverCount = UnityEngine.Random.Range(0, Mathf.Max(1, maxCoverPerRoom) + 1);
        for (int i = 0; i < localCoverCount; i++)
        {
            int x = UnityEngine.Random.Range(room.MinX + 1, room.MaxX);
            int z = UnityEngine.Random.Range(room.MinZ + 1, room.MaxZ);

            if (wallCells[x, z] || coverCells[x, z])
            {
                continue;
            }

            float blockHeight = UnityEngine.Random.value < 0.4f ? floorBoxHeight : coverHeight;
            float footprint = blockHeight <= floorBoxHeight ? 0.65f : 0.8f;
            Vector3 worldPosition = CellToWorld(x, z, blockHeight * 0.5f, width, depth);
            Vector3 scale = new Vector3(cellSize * footprint, blockHeight, cellSize * footprint);
            CreateBlock($"RoomCover_{x}_{z}", worldPosition, scale, coverMaterial);
            coverCells[x, z] = true;
        }
    }

    private void CreateRoomEncounters(List<Room> rooms, bool[,] wallCells, bool[,] coverCells, Room spawnRoom)
    {
        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);

        for (int index = 1; index < rooms.Count; index++)
        {
            if (!roomArchetypes.TryGetValue(index, out RoomArchetype archetype))
            {
                continue;
            }

            bool isEncounterRoom = archetype == RoomArchetype.SmallEncounter || archetype == RoomArchetype.LargeEncounter;
            if (!isEncounterRoom)
            {
                continue;
            }

            Room room = rooms[index];

            GameObject encounterObj = new GameObject($"Encounter_{index}");
            encounterObj.transform.SetParent(generatedRoot);

            Vector3 roomCenter = CellToWorld(room.CenterX, room.CenterZ, wallHeight * 0.5f, width, depth);
            encounterObj.transform.position = roomCenter;

            BoxCollider trigger = encounterObj.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(room.Width * cellSize, wallHeight, room.Depth * cellSize);
            trigger.center = Vector3.zero;

            RoomEncounterController encounter = encounterObj.AddComponent<RoomEncounterController>();

            List<Doorway> doorways = CollectDoorways(room, wallCells);
            for (int d = 0; d < doorways.Count; d++)
            {
                GameObject barrier = CreateDoorBarrier(doorways[d], width, depth, index, d);
                encounter.AddBarrier(barrier);
            }

            int enemiesToSpawn = DetermineEnemyCountForRoom(room, spawnRoom);
            SpawnRoomEnemies(encounter, room, wallCells, coverCells, enemiesToSpawn, width, depth);
        }
    }

    private int DetermineEnemyCountForRoom(Room room, Room spawnRoom)
    {
        int minCount = Mathf.Max(1, minEnemiesPerRoom);
        int maxCount = Mathf.Max(minCount, maxEnemiesPerRoom);

        int baseCount = UnityEngine.Random.Range(minCount, maxCount + 1);
        int area = room.Width * room.Depth;
        if (area > spawnRoom.Width * spawnRoom.Depth)
        {
            baseCount += 1;
        }

        return baseCount;
    }

    private void SpawnRoomEnemies(RoomEncounterController encounter, Room room, bool[,] wallCells, bool[,] coverCells, int count, int width, int depth)
    {
        int spawned = 0;
        int attempts = count * 20;

        for (int attempt = 0; attempt < attempts && spawned < count; attempt++)
        {
            int x = UnityEngine.Random.Range(room.MinX + 1, room.MaxX);
            int z = UnityEngine.Random.Range(room.MinZ + 1, room.MaxZ);

            if (wallCells[x, z] || coverCells[x, z] || !HasPlatformCell(x, z))
            {
                continue;
            }

            Vector3 worldPosition = CellToWorld(x, z, GetPlatformTopY(x, z), width, depth);
            EnemyVariant variant = PickVariant();
            GameObject enemy = CreateEnemy(variant, worldPosition, spawned, false);
            encounter.AddEnemy(enemy);
            spawned++;
        }
    }

    private EnemyVariant PickVariant()
    {
        if (enemyVariants == null || enemyVariants.Count == 0)
        {
            return new EnemyVariant();
        }

        float totalWeight = 0f;
        for (int i = 0; i < enemyVariants.Count; i++)
        {
            totalWeight += Mathf.Max(0.01f, enemyVariants[i].spawnWeight);
        }

        float roll = UnityEngine.Random.value * totalWeight;
        float cumulative = 0f;

        for (int i = 0; i < enemyVariants.Count; i++)
        {
            cumulative += Mathf.Max(0.01f, enemyVariants[i].spawnWeight);
            if (roll <= cumulative)
            {
                return enemyVariants[i];
            }
        }

        return enemyVariants[enemyVariants.Count - 1];
    }

    private GameObject CreateEnemy(EnemyVariant variant, Vector3 worldPosition, int index, bool isActive)
    {
        GameObject enemyRoot = new GameObject($"Enemy_{variant.name}_{index}");
        enemyRoot.transform.SetParent(generatedRoot);

        float scale = UnityEngine.Random.Range(
            Mathf.Min(variant.sizeRange.x, variant.sizeRange.y),
            Mathf.Max(variant.sizeRange.x, variant.sizeRange.y));

        float controllerHeight = Mathf.Clamp(1.6f * scale, 1f, 3f);
        float controllerRadius = Mathf.Clamp(0.32f * scale, 0.2f, 0.8f);

        enemyRoot.transform.position = worldPosition;

        CharacterController controller = enemyRoot.AddComponent<CharacterController>();
        controller.height = controllerHeight;
        controller.radius = controllerRadius;
        controller.center = new Vector3(0f, controllerHeight * 0.5f, 0f);

        GameObject visual = GameObject.CreatePrimitive(variant.shape);
        visual.name = "Visual";
        visual.transform.SetParent(enemyRoot.transform);
        visual.transform.localPosition = new Vector3(0f, controllerHeight * 0.5f, 0f);
        visual.transform.localRotation = Quaternion.identity;

        Vector3 visualScale;
        if (variant.shape == PrimitiveType.Capsule)
        {
            visualScale = new Vector3(controllerRadius * 2f, controllerHeight * 0.5f, controllerRadius * 2f);
        }
        else if (variant.shape == PrimitiveType.Sphere)
        {
            float diameter = controllerRadius * 2.2f;
            visualScale = new Vector3(diameter, diameter, diameter);
            visual.transform.localPosition = new Vector3(0f, diameter * 0.5f, 0f);
        }
        else
        {
            visualScale = new Vector3(controllerRadius * 2.2f, controllerHeight * 0.9f, controllerRadius * 2.2f);
        }

        visual.transform.localScale = visualScale;

        Renderer visualRenderer = visual.GetComponent<Renderer>();
        Material enemyMaterial = CreateRuntimeMaterial(variant.color);
        visualRenderer.material = enemyMaterial;

        Collider visualCollider = visual.GetComponent<Collider>();
        if (visualCollider != null)
        {
            Destroy(visualCollider);
        }

        GameObject hurtbox = new GameObject("Hurtbox");
        hurtbox.transform.SetParent(enemyRoot.transform);
        hurtbox.transform.localPosition = new Vector3(0f, controllerHeight * 0.5f, 0f);
        hurtbox.transform.localRotation = Quaternion.identity;
        hurtbox.layer = enemyRoot.layer;

        CapsuleCollider hurtboxCollider = hurtbox.AddComponent<CapsuleCollider>();
        hurtboxCollider.isTrigger = true;
        hurtboxCollider.radius = controllerRadius * 1.05f;
        hurtboxCollider.height = controllerHeight;
        hurtboxCollider.center = Vector3.zero;

        DamageableTarget damageable = enemyRoot.AddComponent<DamageableTarget>();
        damageable.ConfigureHealth(variant.health);

        EnemyAI enemyAi = enemyRoot.AddComponent<EnemyAI>();
        enemyAi.Configure(
            variant.moveSpeed,
            variant.visionRadius,
            enemyVisionMask,
            variant.contactDamage,
            variant.attackRange,
            variant.attackCooldown);

        enemyRoot.AddComponent<EnemyHealthBarUI>();

        enemyRoot.SetActive(isActive);
        return enemyRoot;
    }

    private List<Doorway> CollectDoorways(Room room, bool[,] wallCells)
    {
        List<Doorway> results = new List<Doorway>();

        for (int x = room.MinX; x <= room.MaxX; x++)
        {
            if (!wallCells[x, room.MaxZ] && !wallCells[x, room.MaxZ + 1])
            {
                TryAddDoorway(results, new Doorway(x, room.MaxZ, DoorDirection.North));
            }

            if (!wallCells[x, room.MinZ] && !wallCells[x, room.MinZ - 1])
            {
                TryAddDoorway(results, new Doorway(x, room.MinZ, DoorDirection.South));
            }
        }

        for (int z = room.MinZ; z <= room.MaxZ; z++)
        {
            if (!wallCells[room.MaxX, z] && !wallCells[room.MaxX + 1, z])
            {
                TryAddDoorway(results, new Doorway(room.MaxX, z, DoorDirection.East));
            }

            if (!wallCells[room.MinX, z] && !wallCells[room.MinX - 1, z])
            {
                TryAddDoorway(results, new Doorway(room.MinX, z, DoorDirection.West));
            }
        }

        return results;
    }

    private void TryAddDoorway(List<Doorway> doorways, Doorway doorway)
    {
        for (int i = 0; i < doorways.Count; i++)
        {
            Doorway existing = doorways[i];
            if (existing.Direction == doorway.Direction)
            {
                int dx = Mathf.Abs(existing.X - doorway.X);
                int dz = Mathf.Abs(existing.Z - doorway.Z);
                if (dx + dz <= corridorWidth)
                {
                    return;
                }
            }
        }

        doorways.Add(doorway);
    }

    private GameObject CreateDoorBarrier(Doorway doorway, int width, int depth, int roomIndex, int doorwayIndex)
    {
        Vector3 position = CellToWorld(doorway.X, doorway.Z, wallHeight * 0.5f, width, depth);

        GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = $"Barrier_{roomIndex}_{doorwayIndex}";
        barrier.transform.SetParent(generatedRoot);

        Vector3 scale = new Vector3(cellSize, wallHeight, cellSize);
        if (doorway.Direction == DoorDirection.North || doorway.Direction == DoorDirection.South)
        {
            scale.z = barrierThickness;
            position += (doorway.Direction == DoorDirection.North ? Vector3.forward : Vector3.back) * (cellSize * 0.5f);
        }
        else
        {
            scale.x = barrierThickness;
            position += (doorway.Direction == DoorDirection.East ? Vector3.right : Vector3.left) * (cellSize * 0.5f);
        }

        barrier.transform.position = position;
        barrier.transform.localScale = scale;
        barrier.GetComponent<Renderer>().material = wallMaterial;
        ApplyGroundLayer(barrier);
        barrier.SetActive(false);
        return barrier;
    }

    private void CreateBlock(string objectName, Vector3 worldPosition, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = objectName;
        block.transform.SetParent(generatedRoot);
        block.transform.position = worldPosition;
        block.transform.localScale = scale;
        ApplyGroundLayer(block);
        block.GetComponent<Renderer>().material = material;
    }

    private void ApplyGroundLayer(GameObject gameObject)
    {
        if (generatedGroundLayer >= 0)
        {
            gameObject.layer = generatedGroundLayer;
        }
    }

    private void QueuePlayerSpawn(Room spawnRoom, int width, int depth)
    {
        int spawnX = spawnRoom.CenterX;
        int spawnZ = spawnRoom.CenterZ;
        if (HasPlatformCell(spawnX, spawnZ))
        {
            Vector3 centerBase = CellToWorld(spawnX, spawnZ, GetPlatformTopY(spawnX, spawnZ), width, depth);
            pendingSpawnBase = centerBase;
        }
        else if (TryFindPlatformInRoom(spawnRoom, out int foundX, out int foundZ))
        {
            pendingSpawnBase = CellToWorld(foundX, foundZ, GetPlatformTopY(foundX, foundZ), width, depth);
        }
        else
        {
            pendingSpawnBase = CellToWorld(spawnRoom.CenterX, spawnRoom.CenterZ, transform.position.y, width, depth);
        }

        pendingSpawnAttempts = 0;
        hasPendingPlayerSpawn = !TryPositionPlayerAtSpawn(pendingSpawnBase);
    }

    private bool HasPlatformCell(int x, int z)
    {
        if (platformCells == null)
        {
            return true;
        }

        int width = platformCells.GetLength(0);
        int depth = platformCells.GetLength(1);
        if (x < 0 || z < 0 || x >= width || z >= depth)
        {
            return false;
        }

        return platformCells[x, z];
    }

    private float GetPlatformTopY(int x, int z)
    {
        if (platformTopHeights == null)
        {
            return transform.position.y;
        }

        int width = platformTopHeights.GetLength(0);
        int depth = platformTopHeights.GetLength(1);
        if (x < 0 || z < 0 || x >= width || z >= depth)
        {
            return transform.position.y;
        }

        return platformTopHeights[x, z];
    }

    private bool TryFindPlatformInRoom(Room room, out int foundX, out int foundZ)
    {
        int bestDistance = int.MaxValue;
        foundX = room.CenterX;
        foundZ = room.CenterZ;

        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                if (!HasPlatformCell(x, z))
                {
                    continue;
                }

                int distance = Mathf.Abs(x - room.CenterX) + Mathf.Abs(z - room.CenterZ);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    foundX = x;
                    foundZ = z;
                }
            }
        }

        return bestDistance != int.MaxValue;
    }

    private bool TryPositionPlayerAtSpawn(Vector3 spawnBase)
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            return false;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool wasControllerEnabled = false;
        if (controller != null)
        {
            wasControllerEnabled = controller.enabled;
            controller.enabled = false;
        }

        float spawnY = fallbackPlayerSpawnHeight;

        if (controller != null)
        {
            float floorY = ResolveGeneratedFloorY(spawnBase);
            float bottomOffset = controller.center.y - controller.height * 0.5f;
            spawnY = floorY - bottomOffset + 0.05f;
        }
        else
        {
            spawnY = ResolveGeneratedFloorY(spawnBase) + fallbackPlayerSpawnHeight;
        }

        player.transform.position = new Vector3(spawnBase.x, spawnY, spawnBase.z);

        if (controller != null)
        {
            controller.enabled = wasControllerEnabled;
        }

        return true;
    }

    private float ResolveGeneratedFloorY(Vector3 spawnBase)
    {
        Vector3 rayOrigin = spawnBase + Vector3.up * Mathf.Max(25f, wallHeight + 10f);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (generatedRoot != null && hit.transform != null && hit.transform.IsChildOf(generatedRoot))
            {
                return hit.point.y;
            }
        }

        return transform.position.y;
    }

    private Vector3 CellToWorld(int x, int z, float y, int width, int depth)
    {
        float xStart = -((width - 1) * cellSize) * 0.5f;
        float zStart = -((depth - 1) * cellSize) * 0.5f;

        return transform.position + new Vector3(
            xStart + x * cellSize,
            y,
            zStart + z * cellSize);
    }
}
