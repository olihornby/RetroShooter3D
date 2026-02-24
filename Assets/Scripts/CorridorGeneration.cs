using UnityEngine;

public partial class RandomMapGenerator
{
    private void GenerateCorridor(bool[,] cells, int startX, int startZ, int endX, int endZ, int radius)
    {
        bool horizontalFirst = UnityEngine.Random.value < 0.5f;
        if (horizontalFirst)
        {
            CarveLine(cells, startX, startZ, endX, startZ, radius);
            CarveLine(cells, endX, startZ, endX, endZ, radius);
        }
        else
        {
            CarveLine(cells, startX, startZ, startX, endZ, radius);
            CarveLine(cells, startX, endZ, endX, endZ, radius);
        }
    }
}
