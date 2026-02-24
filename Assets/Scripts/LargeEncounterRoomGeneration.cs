using UnityEngine;

public partial class RandomMapGenerator
{
    private void ApplyLargeEncounterRoom(Room room, int[,] levels)
    {
        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                levels[x, z] = 0;
            }
        }

        int highLevel = Mathf.Clamp(maxPlatformLevels - 1, 1, maxPlatformLevels);
        int midZ = room.CenterZ;
        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            if (x % 3 == 0)
            {
                levels[x, midZ] = highLevel;
            }
        }
    }
}
