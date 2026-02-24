using UnityEngine;

/// <summary>
/// Simple enemy that chases the player only when the player is visible
/// within a specified vision radius and line-of-sight.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.8f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float stopDistance = 1.4f;

    [Header("Vision")]
    [SerializeField] private float visionRadius = 16f;
    [SerializeField] private float eyeHeight = 1.1f;
    [SerializeField] private LayerMask visionMask = ~0;

    private CharacterController controller;
    private Transform playerTransform;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Start()
    {
        ResolvePlayer();
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            ResolvePlayer();
            ApplyGravityOnly();
            return;
        }

        if (CanSeePlayer())
        {
            MoveTowardsPlayer();
        }
        else
        {
            ApplyGravityOnly();
        }
    }

    public void Configure(float newMoveSpeed, float newVisionRadius, LayerMask newVisionMask)
    {
        moveSpeed = newMoveSpeed;
        visionRadius = newVisionRadius;
        visionMask = newVisionMask;
    }

    private bool CanSeePlayer()
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 playerAimPoint = playerTransform.position + Vector3.up * 0.9f;
        Vector3 toPlayer = playerAimPoint - eyePosition;
        float distance = toPlayer.magnitude;

        if (distance > visionRadius)
        {
            return false;
        }

        if (distance <= 0.001f)
        {
            return true;
        }

        Vector3 direction = toPlayer / distance;
        if (Physics.Raycast(eyePosition, direction, out RaycastHit hit, distance, visionMask, QueryTriggerInteraction.Ignore))
        {
            return hit.transform == playerTransform || hit.transform.IsChildOf(playerTransform);
        }

        return false;
    }

    private void MoveTowardsPlayer()
    {
        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;

        float distance = toPlayer.magnitude;
        if (distance > stopDistance)
        {
            Vector3 direction = toPlayer.normalized;
            controller.Move(direction * moveSpeed * Time.deltaTime);

            if (direction.sqrMagnitude > 0.0001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 8f);
            }
        }

        ApplyGravityOnly();
    }

    private void ApplyGravityOnly()
    {
        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(new Vector3(0f, velocity.y, 0f) * Time.deltaTime);
    }

    private void ResolvePlayer()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
            return;
        }

        Camera main = Camera.main;
        if (main != null)
        {
            playerTransform = main.transform.root;
        }
    }
}
