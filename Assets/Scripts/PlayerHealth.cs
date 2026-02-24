using UnityEngine;

/// <summary>
/// Basic player health component for enemy melee damage.
/// Attach to the Player object.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (GetComponent<PlayerHealthUI>() == null)
        {
            gameObject.AddComponent<PlayerHealthUI>();
        }
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || IsDead)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);

        if (currentHealth <= 0f)
        {
            Debug.Log("Player died.");
        }
    }
}
