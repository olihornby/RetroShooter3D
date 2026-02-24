using UnityEngine;

/// <summary>
/// Displays player health bar at top of screen.
/// Attach to player object.
/// </summary>
public class PlayerHealthUI : MonoBehaviour
{
    [SerializeField] private Vector2 barSize = new Vector2(360f, 24f);
    [SerializeField] private float topMargin = 10f;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.85f, 1f, 0.95f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 16;

    private PlayerHealth playerHealth;
    private Texture2D pixel;
    private GUIStyle labelStyle;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = fontSize,
            normal = { textColor = textColor }
        };
    }

    private void OnGUI()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (playerHealth == null || pixel == null)
        {
            return;
        }

        GUI.depth = -1200;

        float normalized = playerHealth.MaxHealth > 0f
            ? Mathf.Clamp01(playerHealth.CurrentHealth / playerHealth.MaxHealth)
            : 0f;

        float x = (Screen.width - barSize.x) * 0.5f;
        float y = topMargin;

        GUI.color = Color.black;
        GUI.DrawTexture(new Rect(x - 2f, y - 2f, barSize.x + 4f, barSize.y + 4f), pixel);

        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(x, y, barSize.x, barSize.y), pixel);

        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x + 2f, y + 2f, (barSize.x - 4f) * normalized, barSize.y - 4f), pixel);

        GUI.color = Color.white;
        labelStyle.fontSize = fontSize;
        labelStyle.normal.textColor = textColor;
        GUI.Label(new Rect(x, y, barSize.x, barSize.y), $"HP {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {Mathf.CeilToInt(playerHealth.MaxHealth)}", labelStyle);
    }

    private void OnDestroy()
    {
        if (pixel != null)
        {
            Destroy(pixel);
        }
    }
}
