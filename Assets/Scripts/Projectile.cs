using UnityEngine;

/// <summary>
/// Runtime projectile with collision damage and impact particles.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private Color particleColor = new Color(0.65f, 0.65f, 0.65f);

    private float damage;
    private float speed;
    private float lifeTime;
    private LayerMask hitMask;
    private Transform owner;
    private Rigidbody body;
    private bool initialized;
    private bool hasCollided;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        CreateTrailRenderer();
    }

    public void Initialize(float newDamage, float newSpeed, float newLifeTime, LayerMask newHitMask, Transform newOwner)
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }

        if (body == null)
        {
            Destroy(gameObject);
            return;
        }

        damage = newDamage;
        speed = newSpeed;
        lifeTime = newLifeTime;
        hitMask = newHitMask;
        owner = newOwner;
        initialized = true;

        IgnoreOwnerColliders();
        body.velocity = transform.forward * speed;
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!initialized || hasCollided)
        {
            return;
        }

        if ((hitMask.value & (1 << collision.gameObject.layer)) == 0)
        {
            return;
        }

        hasCollided = true;

        DamageableTarget target = collision.collider.GetComponentInParent<DamageableTarget>();
        if (target != null)
        {
            target.TakeDamage(damage);
        }

        CreateImpactParticles(collision.GetContact(0).point, collision.GetContact(0).normal);
        Destroy(gameObject);
    }

    private void IgnoreOwnerColliders()
    {
        if (owner == null)
        {
            return;
        }

        Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
        Collider projectileCollider = GetComponent<Collider>();

        foreach (Collider ownerCollider in ownerColliders)
        {
            Physics.IgnoreCollision(projectileCollider, ownerCollider, true);
        }
    }

    private void CreateTrailRenderer()
    {
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.08f;
        trail.startWidth = 0.04f;
        trail.endWidth = 0.005f;
        trail.minVertexDistance = 0.02f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        Material trailMaterial = new Material(Shader.Find("Sprites/Default"));
        trailMaterial.color = new Color(0.75f, 0.75f, 0.75f, 0.8f);
        trail.material = trailMaterial;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.85f, 0.85f, 0.85f), 0f),
                new GradientColorKey(new Color(0.45f, 0.45f, 0.45f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        trail.colorGradient = gradient;
    }

    private void CreateImpactParticles(Vector3 position, Vector3 normal)
    {
        GameObject impact = new GameObject("ProjectileImpact");
        impact.transform.position = position;
        impact.transform.rotation = Quaternion.LookRotation(normal);

        ParticleSystem particles = impact.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(particleColor.r, particleColor.g, particleColor.b, 0.85f),
            new Color(0.25f, 0.25f, 0.25f, 0.65f));
        main.gravityModifier = 0.8f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18) });

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 20f;
        shape.radius = 0.02f;

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.z = new ParticleSystem.MinMaxCurve(0.5f, 1.8f);

        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient colorGradient = new Gradient();
        colorGradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.75f, 0.75f, 0.75f), 0f),
                new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = colorGradient;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(1f, 0.4f)));

        particles.Play();
        Destroy(impact, 1f);
    }
}
