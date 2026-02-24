using UnityEngine;

/// <summary>
/// Draws an on-screen health bar above an enemy.
/// Attach to enemy root with DamageableTarget.
/// </summary>
public class EnemyHealthBarUI : MonoBehaviour
{
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(60f, 8f);
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
    [SerializeField] private Color fillColor = new Color(0.2f, 0.95f, 0.2f, 0.95f);

    private DamageableTarget target;
    private Camera targetCamera;
    private Texture2D pixel;

    private void Awake()
    {
        target = GetComponent<DamageableTarget>();
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    private void OnGUI()
    {
        if (target == null || pixel == null)
        {
            return;
        }

        if (target.MaxHealth <= 0f)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
            if (targetCamera == null)
            {
                targetCamera = FindObjectOfType<Camera>();
            }

            if (targetCamera == null)
            {
                return;
            }
        }

        Vector3 screenPosition = targetCamera.WorldToScreenPoint(transform.position + worldOffset);
        if (screenPosition.z <= 0f)
        {
            return;
        }

        float normalized = Mathf.Clamp01(target.CurrentHealth / target.MaxHealth);
        float x = screenPosition.x - barSize.x * 0.5f;
        float y = Screen.height - screenPosition.y;

        GUI.color = backgroundColor;
        GUI.DrawTexture(new Rect(x, y, barSize.x, barSize.y), pixel);

        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(x + 1f, y + 1f, (barSize.x - 2f) * normalized, barSize.y - 2f), pixel);

        GUI.color = Color.white;
    }

    private void OnDestroy()
    {
        if (pixel != null)
        {
            Destroy(pixel);
        }
    }
}
