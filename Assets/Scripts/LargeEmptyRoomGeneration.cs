using UnityEngine;

public partial class RandomMapGenerator
{
    private void ApplyLargeEmptyRoom(Room room, int[,] levels)
    {
        int maxStep = Mathf.Clamp(maxPlatformLevels / 2, 1, maxPlatformLevels);
        int centerX = room.CenterX;
        int centerZ = room.CenterZ;

        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                int distance = Mathf.Abs(x - centerX) + Mathf.Abs(z - centerZ);
                int ring = Mathf.Clamp(distance / 4, 0, maxStep);
                levels[x, z] = Mathf.Max(0, maxStep - ring);
            }
        }
    }
}
