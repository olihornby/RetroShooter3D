using UnityEngine;

public partial class RandomMapGenerator
{
    private void ApplyHallwayParkourRoom(Room room, int[,] levels)
    {
        bool alongX = room.Width >= room.Depth;
        int laneA = alongX ? room.CenterZ : room.CenterX;
        int laneB = alongX ? Mathf.Clamp(room.CenterZ + 1, room.MinZ + 1, room.MaxZ - 1) : Mathf.Clamp(room.CenterX + 1, room.MinX + 1, room.MaxX - 1);
        int high = Mathf.Clamp(maxPlatformLevels / 2 + 1, 1, maxPlatformLevels);

        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                levels[x, z] = -1;
            }
        }

        int from = alongX ? room.MinX + 1 : room.MinZ + 1;
        int to = alongX ? room.MaxX - 1 : room.MaxZ - 1;
        for (int p = from; p <= to; p++)
        {
            bool alternate = p % 2 == 0;
            int xA = alongX ? p : laneA;
            int zA = alongX ? laneA : p;
            int xB = alongX ? p : laneB;
            int zB = alongX ? laneB : p;

            levels[xA, zA] = alternate ? high : 0;
            levels[xB, zB] = alternate ? 0 : high;
        }
    }
}
