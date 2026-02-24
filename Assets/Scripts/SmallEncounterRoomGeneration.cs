using UnityEngine;

public partial class RandomMapGenerator
{
    private void ApplySmallEncounterRoom(Room room, int[,] levels)
    {
        int centerX = room.CenterX;
        int centerZ = room.CenterZ;

        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                levels[x, z] = 0;
            }
        }

        int bumpLevel = Mathf.Clamp(maxPlatformLevels / 2, 1, maxPlatformLevels);
        if (centerX > room.MinX && centerX < room.MaxX && centerZ > room.MinZ && centerZ < room.MaxZ)
        {
            levels[centerX, centerZ] = bumpLevel;
        }
    }
}
