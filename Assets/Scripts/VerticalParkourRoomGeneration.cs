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

        int cx = room.CenterX;
        int cz = room.CenterZ;
        int maxLevel = Mathf.Clamp(maxPlatformLevels, 2, 8);

        for (int i = 0; i < maxLevel + 2; i++)
        {
            int offsetX = (i % 2 == 0 ? -1 : 1) * (i / 2);
            int offsetZ = (i % 2 == 0 ? 1 : -1) * (i / 2);
            int x = Mathf.Clamp(cx + offsetX, room.MinX + 1, room.MaxX - 1);
            int z = Mathf.Clamp(cz + offsetZ, room.MinZ + 1, room.MaxZ - 1);
            int level = Mathf.Clamp(i, 0, maxLevel);
            levels[x, z] = level;

            if (x + 1 < room.MaxX)
            {
                levels[x + 1, z] = Mathf.Max(0, level - 1);
            }
        }

        levels[cx, cz] = 0;
    }
}
