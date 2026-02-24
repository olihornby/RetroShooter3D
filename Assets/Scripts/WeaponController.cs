using UnityEngine;

/// <summary>
/// Simple first-person hitscan weapon controller.
/// Attach to the Player object.
/// </summary>
public class WeaponController : MonoBehaviour
{
    [Header("Weapon Stats")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 60f;
    [SerializeField] private float fireRate = 5f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Visuals")]
    [SerializeField] private bool generateWeaponModel = true;
    [SerializeField] private Color weaponPrimaryColor = new Color(0.15f, 0.15f, 0.15f);
    [SerializeField] private Color weaponSecondaryColor = new Color(0.35f, 0.35f, 0.35f);

    private float nextTimeToFire;

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
            GenerateWeaponModel();
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
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask))
        {
            DamageableTarget target = hit.collider.GetComponentInParent<DamageableTarget>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
        }

        Debug.DrawRay(ray.origin, ray.direction * range, Color.red, 0.1f);
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
        modelRoot.transform.localPosition = new Vector3(0.3f, -0.25f, 0.6f);
        modelRoot.transform.localRotation = Quaternion.Euler(5f, 180f, 0f);

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
