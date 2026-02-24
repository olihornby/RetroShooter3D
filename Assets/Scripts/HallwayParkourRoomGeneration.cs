using UnityEngine;

public partial class RandomMapGenerator
{
    private void ApplyHallwayParkourRoom(Room room, int[,] levels)
    {
        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                levels[x, z] = -1;
            }
        }

        bool alongX = room.Width >= room.Depth;
        int platformCount = UnityEngine.Random.Range(3, 7);
        int maxLevel = Mathf.Clamp(Mathf.Max(6, maxPlatformLevels), 3, 8);

        System.Collections.Generic.HashSet<int> usedHeights = new System.Collections.Generic.HashSet<int>();
        System.Collections.Generic.HashSet<Vector2Int> usedCells = new System.Collections.Generic.HashSet<Vector2Int>();

        for (int i = 0; i < platformCount; i++)
        {
            int heightLevel = i + 1;
            if (heightLevel > maxLevel)
            {
                heightLevel = maxLevel;
            }

            if (!usedHeights.Contains(heightLevel))
            {
                usedHeights.Add(heightLevel);
            }

            bool placed = false;
            for (int attempt = 0; attempt < 40 && !placed; attempt++)
            {
                float t = (i + 1f) / (platformCount + 1f);
                int primary = alongX
                    ? Mathf.RoundToInt(Mathf.Lerp(room.MinX + 1, room.MaxX - 1, t))
                    : Mathf.RoundToInt(Mathf.Lerp(room.MinZ + 1, room.MaxZ - 1, t));

                int lateral = alongX
                    ? UnityEngine.Random.Range(room.MinZ + 1, room.MaxZ)
                    : UnityEngine.Random.Range(room.MinX + 1, room.MaxX);

                int x = alongX ? primary : lateral;
                int z = alongX ? lateral : primary;
                Vector2Int cell = new Vector2Int(x, z);

                if (usedCells.Contains(cell))
                {
                    continue;
                }

                levels[x, z] = heightLevel;
                usedCells.Add(cell);
                placed = true;
            }
        }

        levels[room.CenterX, room.CenterZ] = 1;
    }
}
