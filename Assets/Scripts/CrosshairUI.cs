using UnityEngine;

/// <summary>
/// Simple center-screen crosshair for first-person aiming.
/// Attach to camera (auto-added by WeaponController).
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private Color crosshairColor = Color.white;
    [SerializeField] private float size = 14f;
    [SerializeField] private float thickness = 2f;
    [SerializeField] private float gap = 4f;

    private Texture2D pixel;

    private void Awake()
    {
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();
    }

    private void OnGUI()
    {
        if (pixel == null)
        {
            return;
        }

        GUI.color = crosshairColor;

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float halfSize = size * 0.5f;

        DrawRect(centerX - halfSize - gap, centerY - thickness * 0.5f, size, thickness);
        DrawRect(centerX + gap, centerY - thickness * 0.5f, size, thickness);
        DrawRect(centerX - thickness * 0.5f, centerY - halfSize - gap, thickness, size);
        DrawRect(centerX - thickness * 0.5f, centerY + gap, thickness, size);
    }

    private void DrawRect(float x, float y, float width, float height)
    {
        GUI.DrawTexture(new Rect(x, y, width, height), pixel);
    }

    private void OnDestroy()
    {
        if (pixel != null)
        {
            Destroy(pixel);
        }
    }
}
