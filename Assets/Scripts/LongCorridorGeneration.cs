using UnityEngine;

public partial class RandomMapGenerator
{
    private void GenerateLongCorridor(bool[,] cells, int startX, int startZ, int endX, int endZ, int radius)
    {
        int mapWidth = cells.GetLength(0);
        int mapDepth = cells.GetLength(1);
        bool routeHorizontalFirst = UnityEngine.Random.value < 0.5f;

        if (routeHorizontalFirst)
        {
            int midZ = UnityEngine.Random.Range(2, mapDepth - 2);
            CarveLine(cells, startX, startZ, startX, midZ, radius);
            CarveLine(cells, startX, midZ, endX, midZ, radius);
            CarveLine(cells, endX, midZ, endX, endZ, radius);
        }
        else
        {
            int midX = UnityEngine.Random.Range(2, mapWidth - 2);
            CarveLine(cells, startX, startZ, midX, startZ, radius);
            CarveLine(cells, midX, startZ, midX, endZ, radius);
            CarveLine(cells, midX, endZ, endX, endZ, radius);
        }
    }
}
