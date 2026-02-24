using UnityEngine;

public partial class RandomMapGenerator
{
    private void ApplyVerticalParkourRoom(Room room, int[,] levels)
    {
        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                levels[x, z] = -1;
            }
        }

        int platformCount = UnityEngine.Random.Range(3, 7);
        int maxLevel = Mathf.Clamp(Mathf.Max(6, maxPlatformLevels), 3, 8);
        int cx = room.CenterX;
        int cz = room.CenterZ;

        System.Collections.Generic.HashSet<Vector2Int> usedCells = new System.Collections.Generic.HashSet<Vector2Int>();

        for (int i = 0; i < platformCount; i++)
        {
            bool placed = false;
            int heightLevel = Mathf.Clamp(i + 1, 1, maxLevel);

            for (int attempt = 0; attempt < 60 && !placed; attempt++)
            {
                int radius = Mathf.Max(1, i + 1);
                int offsetX = UnityEngine.Random.Range(-radius, radius + 1);
                int offsetZ = UnityEngine.Random.Range(-radius, radius + 1);
                int x = Mathf.Clamp(cx + offsetX, room.MinX + 1, room.MaxX - 1);
                int z = Mathf.Clamp(cz + offsetZ, room.MinZ + 1, room.MaxZ - 1);
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

        levels[cx, cz] = 1;
    }
}
