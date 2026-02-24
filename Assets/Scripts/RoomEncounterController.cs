using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles a room encounter: activates enemies, locks exits, and unlocks when room is cleared.
/// </summary>
public class RoomEncounterController : MonoBehaviour
{
    [SerializeField] private bool startLockedOnEnter = true;

    private readonly List<GameObject> enemies = new List<GameObject>();
    private readonly List<GameObject> barriers = new List<GameObject>();

    private bool activated;
    private bool completed;

    public void AddEnemy(GameObject enemy)
    {
        if (enemy != null)
        {
            enemies.Add(enemy);
            enemy.SetActive(false);
        }
    }

    public void AddBarrier(GameObject barrier)
    {
        if (barrier != null)
        {
            barriers.Add(barrier);
            barrier.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (completed || activated)
        {
            return;
        }

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null)
        {
            return;
        }

        ActivateEncounter();
    }

    private void Update()
    {
        if (!activated || completed)
        {
            return;
        }

        bool anyAlive = false;
        for (int i = 0; i < enemies.Count; i++)
        {
            GameObject enemy = enemies[i];
            if (enemy != null)
            {
                anyAlive = true;
                break;
            }
        }

        if (!anyAlive)
        {
            CompleteEncounter();
        }
    }

    private void ActivateEncounter()
    {
        activated = true;

        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i] != null)
            {
                enemies[i].SetActive(true);
            }
        }

        if (startLockedOnEnter)
        {
            for (int i = 0; i < barriers.Count; i++)
            {
                if (barriers[i] != null)
                {
                    barriers[i].SetActive(true);
                }
            }
        }
    }

    private void CompleteEncounter()
    {
        completed = true;

        for (int i = 0; i < barriers.Count; i++)
        {
            if (barriers[i] != null)
            {
                Destroy(barriers[i]);
            }
        }

        barriers.Clear();
    }
}
