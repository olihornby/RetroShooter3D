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
    [SerializeField] private Vector2 ammoTextOffset = new Vector2(0f, 28f);
    [SerializeField] private int ammoFontSize = 16;

    private Texture2D pixel;
    private GUIStyle ammoStyle;
    private WeaponController weaponController;

    private void Awake()
    {
        pixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();

        ammoStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = ammoFontSize,
            normal = { textColor = crosshairColor }
        };

        weaponController = GetComponentInParent<WeaponController>();
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

        DrawAmmo(centerX, centerY);
    }

    private void DrawAmmo(float centerX, float centerY)
    {
        if (weaponController == null)
        {
            return;
        }

        ammoStyle.normal.textColor = crosshairColor;
        ammoStyle.fontSize = ammoFontSize;

        string ammoText = weaponController.IsReloading
            ? "Reloading..."
            : $"Ammo: {weaponController.CurrentAmmo}/{weaponController.MaxAmmo}";

        Rect textRect = new Rect(
            centerX - 110f + ammoTextOffset.x,
            centerY + ammoTextOffset.y,
            220f,
            28f);
        GUI.Label(textRect, ammoText, ammoStyle);
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
