using UnityEngine;

/// <summary>
/// Simple first-person projectile weapon controller.
/// Attach to the Player object.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private int magazineSize = 8;
    [SerializeField] private float reloadCooldown = 1.5f;
    [SerializeField] private float projectileSpeed = 55f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform muzzleTransform;

    [Header("Model")]
    [SerializeField] private GameObject pistolModelPrefab;
    [SerializeField] private string muzzlePointName = "MuzzlePoint";
    [SerializeField] private Vector3 pistolLocalPosition = new Vector3(0.3f, -0.25f, 0.6f);
    [SerializeField] private Vector3 pistolLocalRotation = new Vector3(5f, 180f, 0f);
    [SerializeField] private Vector3 pistolLocalScale = Vector3.one;
    [SerializeField] private Vector3 defaultMuzzleLocalPosition = new Vector3(0f, 0f, -0.35f);
    [SerializeField] private float muzzleForwardOffset = 0.06f;
    [SerializeField] private Vector3 muzzleLocalOffset = new Vector3(0f, 0.025f, 0f);

    [Header("Visuals")]
    [SerializeField] private bool generateWeaponModel = true;
    [SerializeField] private Color weaponPrimaryColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color weaponSecondaryColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color projectileColor = new Color(0.6f, 0.6f, 0.6f);

    [Header("HUD Fallback")]
    [SerializeField] private bool showFallbackHud = true;
    [SerializeField] private Color hudColor = Color.white;
    [SerializeField] private float hudCrosshairSize = 16f;
    [SerializeField] private float hudCrosshairThickness = 3f;
    [SerializeField] private float hudCrosshairGap = 4f;
    [SerializeField] private int hudFontSize = 18;

    private float nextTimeToFire;
    private float reloadCompleteTime;
    private int currentAmmo;
    private bool isReloading;
    private GameObject projectilePrefab;
    private bool crosshairInitialized;
    private Texture2D hudPixel;
    private GUIStyle hudTextStyle;

    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => Mathf.Max(1, magazineSize);
    public bool IsReloading => isReloading;

    private void Start()
    {
        TryResolveCamera();

        if (generateWeaponModel && cameraTransform != null)
        {
            SetupWeaponModel();
        }

        EnsureCrosshair();
        currentAmmo = Mathf.Max(1, magazineSize);
        CreateHudResources();
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            TryResolveCamera();
            EnsureCrosshair();
        }

        if (cameraTransform == null)
        {
            return;
        }

        if (isReloading)
        {
            if (Time.time >= reloadCompleteTime)
            {
                isReloading = false;
                currentAmmo = Mathf.Max(1, magazineSize);
            }

            return;
        }

        if (Input.GetButtonDown("Fire1"))
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (Time.time < nextTimeToFire)
        {
            return;
        }

        if (currentAmmo <= 0)
        {
            StartReload();
            return;
        }

        nextTimeToFire = Time.time + 1f / fireRate;
        Fire();
        currentAmmo--;

        if (currentAmmo <= 0)
        {
            StartReload();
        }
    }

    private void StartReload()
    {
        isReloading = true;
        reloadCompleteTime = Time.time + reloadCooldown;
    }

    private void Fire()
    {
        if (projectilePrefab == null)
        {
            projectilePrefab = CreateRuntimeProjectilePrefab();
        }

        Vector3 fireDirection = cameraTransform.forward;
        Vector3 spawnPosition = muzzleTransform != null
            ? muzzleTransform.position + muzzleTransform.TransformDirection(muzzleLocalOffset) + fireDirection * muzzleForwardOffset
            : cameraTransform.position + fireDirection * 0.6f;
        Quaternion spawnRotation = Quaternion.LookRotation(fireDirection);
        GameObject projectileInstance = Instantiate(projectilePrefab, spawnPosition, spawnRotation);
        projectileInstance.SetActive(true);

        Projectile projectile = projectileInstance.GetComponent<Projectile>();
        if (projectile != null)
        {
            projectile.Initialize(damage, projectileSpeed, projectileLifetime, hitMask, transform);
        }
        else
        {
            Destroy(projectileInstance);
        }
    }

    private GameObject CreateRuntimeProjectilePrefab()
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "RuntimeProjectile";
        projectile.transform.localScale = Vector3.one * 0.08f;
        projectile.SetActive(false);

        Renderer projectileRenderer = projectile.GetComponent<Renderer>();
        Material material = new Material(Shader.Find("Standard"));
        material.color = projectileColor;
        material.SetFloat("_Glossiness", 0.2f);
        projectileRenderer.material = material;

        SphereCollider sphereCollider = projectile.GetComponent<SphereCollider>();
        sphereCollider.isTrigger = false;

        Rigidbody body = projectile.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        projectile.AddComponent<Projectile>();

        return projectile;
    }

    private void OnDestroy()
    {
        if (projectilePrefab != null)
        {
            Destroy(projectilePrefab);
        }

        if (hudPixel != null)
        {
            Destroy(hudPixel);
        }
    }

    private void OnGUI()
    {
        if (!showFallbackHud)
        {
            return;
        }

        if (hudPixel == null)
        {
            CreateHudResources();
            if (hudPixel == null)
            {
                return;
            }
        }

        GUI.depth = -1000;
        GUI.color = hudColor;

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        float halfSize = hudCrosshairSize * 0.5f;

        DrawHudRect(centerX - halfSize - hudCrosshairGap, centerY - hudCrosshairThickness * 0.5f, hudCrosshairSize, hudCrosshairThickness);
        DrawHudRect(centerX + hudCrosshairGap, centerY - hudCrosshairThickness * 0.5f, hudCrosshairSize, hudCrosshairThickness);
        DrawHudRect(centerX - hudCrosshairThickness * 0.5f, centerY - halfSize - hudCrosshairGap, hudCrosshairThickness, hudCrosshairSize);
        DrawHudRect(centerX - hudCrosshairThickness * 0.5f, centerY + hudCrosshairGap, hudCrosshairThickness, hudCrosshairSize);
        DrawHudRect(centerX - 2f, centerY - 2f, 4f, 4f);

        if (hudTextStyle != null)
        {
            hudTextStyle.fontSize = hudFontSize;
            hudTextStyle.normal.textColor = hudColor;
            string ammoText = isReloading ? "Reloading..." : $"Ammo: {CurrentAmmo}/{MaxAmmo}";
            GUI.Label(new Rect(centerX - 120f, centerY + 30f, 240f, 28f), ammoText, hudTextStyle);
        }
    }

    private void CreateHudResources()
    {
        if (hudPixel == null)
        {
            hudPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            hudPixel.SetPixel(0, 0, Color.white);
            hudPixel.Apply();
        }

        if (hudTextStyle == null)
        {
            hudTextStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperCenter,
                fontSize = hudFontSize,
                normal = { textColor = hudColor }
            };
        }
    }

    private void DrawHudRect(float x, float y, float width, float height)
    {
        GUI.DrawTexture(new Rect(x, y, width, height), hudPixel);
    }

    private void SetupWeaponModel()
    {
        if (cameraTransform == null)
        {
            return;
        }

        Transform existingModel = cameraTransform.Find("WeaponModel");
        if (existingModel != null)
        {
            ResolveMuzzlePoint(existingModel);
            return;
        }

        if (pistolModelPrefab != null)
        {
            GameObject modelInstance = Instantiate(pistolModelPrefab, cameraTransform);
            modelInstance.name = "WeaponModel";
            modelInstance.SetActive(true);
            modelInstance.transform.localPosition = pistolLocalPosition;
            modelInstance.transform.localRotation = Quaternion.Euler(pistolLocalRotation);
            modelInstance.transform.localScale = pistolLocalScale;

            EnsureModelIsVisible(modelInstance.transform);

            Collider[] colliders = modelInstance.GetComponentsInChildren<Collider>(true);
            foreach (Collider currentCollider in colliders)
            {
                Destroy(currentCollider);
            }

            ResolveMuzzlePoint(modelInstance.transform);

            return;
        }

        GenerateWeaponModel();
    }

    private void EnsureModelIsVisible(Transform modelRoot)
    {
        Renderer[] renderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            Debug.LogWarning("Assigned pistol prefab has no Renderer. Falling back to generated weapon model.");
            Destroy(modelRoot.gameObject);
            GenerateWeaponModel();
            return;
        }

        foreach (Renderer currentRenderer in renderers)
        {
            currentRenderer.enabled = true;
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            combinedBounds.Encapsulate(renderers[index].bounds);
        }

        float largestDimension = Mathf.Max(combinedBounds.size.x, combinedBounds.size.y, combinedBounds.size.z);
        if (largestDimension < 0.02f || largestDimension > 2.5f)
        {
            float targetSize = 0.35f;
            float scaleFactor = targetSize / Mathf.Max(largestDimension, 0.0001f);
            modelRoot.localScale *= scaleFactor;
        }
    }

    private void GenerateWeaponModel()
    {
        Transform existingModel = cameraTransform.Find("WeaponModel");
        if (existingModel != null)
        {
            return;
        }

        GameObject modelRoot = new GameObject("WeaponModel");
        modelRoot.transform.SetParent(cameraTransform);
        modelRoot.transform.localPosition = pistolLocalPosition;
        modelRoot.transform.localRotation = Quaternion.Euler(pistolLocalRotation);
        modelRoot.transform.localScale = pistolLocalScale;

        CreateWeaponPart(
            "Body",
            modelRoot.transform,
            new Vector3(0f, 0f, 0f),
            new Vector3(0.22f, 0.14f, 0.45f),
            weaponPrimaryColor);

        CreateWeaponPart(
            "Barrel",
            modelRoot.transform,
            new Vector3(0f, 0.02f, -0.28f),
            new Vector3(0.1f, 0.08f, 0.25f),
            weaponSecondaryColor);

        CreateWeaponPart(
            "Grip",
            modelRoot.transform,
            new Vector3(0f, -0.14f, 0.08f),
            new Vector3(0.1f, 0.22f, 0.14f),
            weaponSecondaryColor);

        ResolveMuzzlePoint(modelRoot.transform);
    }

    private void ResolveMuzzlePoint(Transform weaponModelRoot)
    {
        if (weaponModelRoot == null)
        {
            return;
        }

        Transform existingMuzzle = weaponModelRoot.Find(muzzlePointName);
        if (existingMuzzle != null)
        {
            muzzleTransform = existingMuzzle;
            return;
        }

        GameObject muzzleObject = new GameObject(muzzlePointName);
        muzzleObject.transform.SetParent(weaponModelRoot);
        muzzleObject.transform.localRotation = Quaternion.identity;

        if (!TryPlaceMuzzleFromRenderBounds(weaponModelRoot, muzzleObject.transform))
        {
            muzzleObject.transform.localPosition = defaultMuzzleLocalPosition;
        }

        muzzleTransform = muzzleObject.transform;
    }

    private bool TryPlaceMuzzleFromRenderBounds(Transform weaponModelRoot, Transform muzzle)
    {
        Renderer[] renderers = weaponModelRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            return false;
        }

        Bounds combinedBounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
        {
            combinedBounds.Encapsulate(renderers[index].bounds);
        }

        Vector3 forward = cameraTransform != null ? cameraTransform.forward : weaponModelRoot.forward;
        float reach = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.y, combinedBounds.extents.z);
        Vector3 worldFrontPoint = combinedBounds.center
            + forward * reach
            + (cameraTransform != null ? cameraTransform.up : weaponModelRoot.up) * (combinedBounds.extents.y * 0.2f);

        muzzle.position = worldFrontPoint;
        muzzle.rotation = Quaternion.LookRotation(forward, Vector3.up);
        return true;
    }

    private void EnsureCrosshair()
    {
        if (crosshairInitialized)
        {
            return;
        }

        if (cameraTransform == null)
        {
            return;
        }

        Transform targetTransform = cameraTransform;
        Camera targetCamera = targetTransform.GetComponent<Camera>();
        if (targetCamera == null)
        {
            targetCamera = targetTransform.GetComponentInChildren<Camera>(true);
            if (targetCamera != null)
            {
                targetTransform = targetCamera.transform;
                cameraTransform = targetTransform;
            }
        }

        if (targetCamera == null)
        {
            return;
        }

        CrosshairUI existingCrosshair = targetTransform.GetComponent<CrosshairUI>();
        if (existingCrosshair == null)
        {
            targetTransform.gameObject.AddComponent<CrosshairUI>();
        }

        crosshairInitialized = true;
    }

    private void TryResolveCamera()
    {
        if (cameraTransform != null)
        {
            Camera existingCamera = cameraTransform.GetComponent<Camera>();
            if (existingCamera == null)
            {
                existingCamera = cameraTransform.GetComponentInChildren<Camera>(true);
                if (existingCamera != null)
                {
                    cameraTransform = existingCamera.transform;
                }
            }

            return;
        }

        Transform directChildCamera = transform.Find("Main Camera");
        if (directChildCamera != null && directChildCamera.GetComponent<Camera>() != null)
        {
            cameraTransform = directChildCamera;
            return;
        }

        Transform holderChildCamera = transform.Find("CameraHolder/Main Camera");
        if (holderChildCamera != null && holderChildCamera.GetComponent<Camera>() != null)
        {
            cameraTransform = holderChildCamera;
            return;
        }

        Transform cameraHolder = transform.Find("CameraHolder");
        if (cameraHolder != null)
        {
            Camera holderCamera = cameraHolder.GetComponentInChildren<Camera>(true);
            if (holderCamera != null)
            {
                cameraTransform = holderCamera.transform;
                return;
            }
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
            return;
        }

        Camera anyCamera = FindObjectOfType<Camera>();
        if (anyCamera != null)
        {
            cameraTransform = anyCamera.transform;
        }
    }

    private void CreateWeaponPart(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.localPosition = localPosition;
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = localScale;

        Material material = new Material(Shader.Find("Standard"));
        material.color = color;
        material.SetFloat("_Glossiness", 0f);
        cube.GetComponent<Renderer>().material = material;

        Destroy(cube.GetComponent<Collider>());
    }
}
