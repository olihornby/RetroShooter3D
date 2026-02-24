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
    [SerializeField] private float projectileSpeed = 55f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Model")]
    [SerializeField] private GameObject pistolModelPrefab;
    [SerializeField] private Vector3 pistolLocalPosition = new Vector3(0.3f, -0.25f, 0.6f);
    [SerializeField] private Vector3 pistolLocalRotation = new Vector3(5f, 180f, 0f);
    [SerializeField] private Vector3 pistolLocalScale = Vector3.one;

    [Header("Visuals")]
    [SerializeField] private bool generateWeaponModel = true;
    [SerializeField] private Color weaponPrimaryColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color weaponSecondaryColor = new Color(0.35f, 0.35f, 0.35f);
    [SerializeField] private Color projectileColor = new Color(0.6f, 0.6f, 0.6f);

    private float nextTimeToFire;
    private GameObject projectilePrefab;

    private void Start()
    {
        if (cameraTransform == null)
        {
            if (Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }
        }

        if (generateWeaponModel && cameraTransform != null)
        {
            SetupWeaponModel();
        }
    }

    private void Update()
    {
        if (cameraTransform == null)
        {
            return;
        }

        if (Input.GetButton("Fire1") && Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + 1f / fireRate;
            Fire();
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null)
        {
            projectilePrefab = CreateRuntimeProjectilePrefab();
        }

        Vector3 spawnPosition = cameraTransform.position + cameraTransform.forward * 0.6f;
        Quaternion spawnRotation = Quaternion.LookRotation(cameraTransform.forward);
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
