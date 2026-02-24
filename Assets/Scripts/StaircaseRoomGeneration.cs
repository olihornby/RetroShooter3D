using UnityEngine;

public partial class RandomMapGenerator
{
    private void ApplyStaircaseRoom(Room room, int[,] levels)
    {
        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                levels[x, z] = 0;
            }
        }
    }
}
