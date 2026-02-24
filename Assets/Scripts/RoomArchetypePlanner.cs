using System.Collections.Generic;
using UnityEngine;

public partial class RandomMapGenerator
{
    private void BuildRoomArchetypePlan(List<Room> rooms, Room spawnRoom)
    {
        roomArchetypes.Clear();

        List<int> smallRooms = new List<int>();
        List<int> largeRooms = new List<int>();

        for (int i = 1; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            int area = room.Width * room.Depth;
            if (area >= 170)
            {
                largeRooms.Add(i);
            }
            else
            {
                smallRooms.Add(i);
            }
        }

        HashSet<int> used = new HashSet<int>();

        int staircaseTargets = Mathf.Min(Mathf.Max(1, staircaseCount), largeRooms.Count > 0 ? largeRooms.Count : rooms.Count - 1);
        AssignRandomArchetype(largeRooms.Count > 0 ? largeRooms : smallRooms, used, RoomArchetype.Staircase, staircaseTargets);

        int hallwayParkourTargets = Mathf.Clamp((rooms.Count - 1) / 8, 1, 3);
        AssignRandomArchetype(largeRooms, used, RoomArchetype.HallwayParkour, hallwayParkourTargets);

        int verticalParkourTargets = Mathf.Clamp((rooms.Count - 1) / 10, 1, 2);
        AssignRandomArchetype(largeRooms, used, RoomArchetype.VerticalParkour, verticalParkourTargets);

        for (int i = 1; i < rooms.Count; i++)
        {
            if (used.Contains(i))
            {
                continue;
            }

            Room room = rooms[i];
            bool isLarge = room.Width * room.Depth >= 170;
            bool encounter = UnityEngine.Random.value < (isLarge ? 0.7f : 0.6f);

            if (isLarge)
            {
                roomArchetypes[i] = encounter ? RoomArchetype.LargeEncounter : RoomArchetype.LargeEmpty;
            }
            else
            {
                roomArchetypes[i] = encounter ? RoomArchetype.SmallEncounter : RoomArchetype.SmallEmpty;
            }
        }
    }

    private void AssignRandomArchetype(List<int> candidates, HashSet<int> used, RoomArchetype archetype, int targetCount)
    {
        if (targetCount <= 0)
        {
            return;
        }

        List<int> available = new List<int>();
        for (int i = 0; i < candidates.Count; i++)
        {
            if (!used.Contains(candidates[i]))
            {
                available.Add(candidates[i]);
            }
        }

        for (int placed = 0; placed < targetCount && available.Count > 0; placed++)
        {
            int pick = UnityEngine.Random.Range(0, available.Count);
            int index = available[pick];
            available.RemoveAt(pick);
            used.Add(index);
            roomArchetypes[index] = archetype;
        }
    }
}
