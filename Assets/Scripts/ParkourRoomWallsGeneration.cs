using UnityEngine;

public partial class RandomMapGenerator
{
    private void BuildParkourRoomWalls(System.Collections.Generic.List<Room> rooms, bool[,] wallCells)
    {
        int width = wallCells.GetLength(0);
        int depth = wallCells.GetLength(1);
        float extensionHeight = wallHeight * 2f;
        float extensionCenterY = wallHeight + extensionHeight * 0.5f;

        for (int index = 1; index < rooms.Count; index++)
        {
            if (!roomArchetypes.TryGetValue(index, out RoomArchetype archetype))
            {
                continue;
            }

            bool isParkourRoom = archetype == RoomArchetype.HallwayParkour || archetype == RoomArchetype.VerticalParkour;
            if (!isParkourRoom)
            {
                continue;
            }

            Room room = rooms[index];

            for (int x = room.MinX; x <= room.MaxX; x++)
            {
                TryBuildWallExtension(x, room.MinZ, width, depth, wallCells, extensionCenterY, extensionHeight);
                TryBuildWallExtension(x, room.MaxZ, width, depth, wallCells, extensionCenterY, extensionHeight);
            }

            for (int z = room.MinZ; z <= room.MaxZ; z++)
            {
                TryBuildWallExtension(room.MinX, z, width, depth, wallCells, extensionCenterY, extensionHeight);
                TryBuildWallExtension(room.MaxX, z, width, depth, wallCells, extensionCenterY, extensionHeight);
            }
        }
    }

    private void TryBuildWallExtension(int x, int z, int width, int depth, bool[,] wallCells, float centerY, float extensionHeight)
    {
        if (x < 0 || z < 0 || x >= width || z >= depth)
        {
            return;
        }

        if (!wallCells[x, z])
        {
            return;
        }

        Vector3 worldPosition = CellToWorld(x, z, centerY, width, depth);
        Vector3 scale = new Vector3(cellSize, extensionHeight, cellSize);
        CreateBlock($"ParkourWallExt_{x}_{z}", worldPosition, scale, wallMaterial);
    }
}
