using UnityEngine;

public partial class RandomMapGenerator
{
    private void ApplyStaircaseRoom(Room room, int[,] levels)
    {
        int span = Mathf.Max(room.Width, room.Depth) - 2;
        int stepCount = Mathf.Clamp(span, 3, 18);
        int maxLevel = Mathf.Clamp(maxPlatformLevels, 2, 8);

        for (int x = room.MinX + 1; x < room.MaxX; x++)
        {
            for (int z = room.MinZ + 1; z < room.MaxZ; z++)
            {
                levels[x, z] = 0;
            }
        }

        bool alongX = room.Width >= room.Depth;
        int lane = alongX ? room.CenterZ : room.CenterX;
        for (int i = 0; i < stepCount; i++)
        {
            float t = (i + 1f) / stepCount;
            int level = Mathf.Clamp(Mathf.RoundToInt(t * maxLevel), 1, maxLevel);
            int x = alongX ? Mathf.RoundToInt(Mathf.Lerp(room.MinX + 1, room.MaxX - 1, t)) : lane;
            int z = alongX ? lane : Mathf.RoundToInt(Mathf.Lerp(room.MinZ + 1, room.MaxZ - 1, t));
            levels[x, z] = level;
        }
    }
}
