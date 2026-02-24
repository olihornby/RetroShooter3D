using UnityEngine;

/// <summary>
/// First-person player controller with 8-bit style movement
/// Attach to player GameObject with Camera as child
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 14f;
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    
    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 320f;
    [SerializeField] private float arrowLookHorizontalSpeed = 90f;
    [SerializeField] private float arrowLookVerticalSpeed = 65f;
    [SerializeField] private float lookSmoothing = 12f;
    [SerializeField] private Transform cameraTransform;
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;

    [Header("Controller")]
    [SerializeField] private float slopeLimit = 80f;
    [SerializeField] private float stepOffset = 0.6f;
    
    private CharacterController controller;
    private Vector3 velocity;
    private Vector3 planarVelocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private float smoothedLookX;
    private float smoothedLookY;
    
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = Mathf.Clamp(slopeLimit, 30f, 89f);
        controller.stepOffset = Mathf.Max(0f, stepOffset);

        if (GetComponent<PlayerHealth>() == null)
        {
            gameObject.AddComponent<PlayerHealth>();
        }

        if (GetComponent<PlayerHealthUI>() == null)
        {
            gameObject.AddComponent<PlayerHealthUI>();
        }
        
        // Lock cursor to center of screen
        Cursor.lockState = CursorLockMode.Locked;
        
        // Find or create camera holder
        if (cameraTransform == null)
        {
            // Look for camera as direct child first
            Transform childCamera = transform.Find("Main Camera");
            if (childCamera == null)
            {
                // Look for CameraHolder
                Transform cameraHolder = transform.Find("CameraHolder");
                if (cameraHolder != null)
                {
                    childCamera = cameraHolder.GetComponentInChildren<Camera>()?.transform;
                }
            }
            
            // If still null, create a camera holder setup
            if (childCamera == null)
            {
                GameObject cameraHolderObj = new GameObject("CameraHolder");
                cameraHolderObj.transform.SetParent(transform);
                cameraHolderObj.transform.localPosition = new Vector3(0, 0.5f, 0);
                cameraHolderObj.transform.localRotation = Quaternion.identity;
                
                // Move main camera under holder
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    mainCam.transform.SetParent(cameraHolderObj.transform);
                    mainCam.transform.localPosition = Vector3.zero;
                    mainCam.transform.localRotation = Quaternion.identity;
                    childCamera = mainCam.transform;
                }
            }
            
            cameraTransform = childCamera;
        }
        
        // Create ground check if doesn't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }
        
    }
    
    private void Update()
    {
        HandleGroundCheck();
        HandleMovement();
        HandleMouseLook();
        
        // Unlock cursor with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
    }
    
    private void HandleGroundCheck()
    {
        bool hasGroundMask = groundMask.value != 0;
        bool maskGrounded = hasGroundMask && Physics.CheckSphere(groundCheck.position, groundDistance, groundMask, QueryTriggerInteraction.Ignore);

        isGrounded = maskGrounded || controller.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
    }
    
    private void HandleMovement()
    {
        float x = 0f;
        float z = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            x -= 1f;
        }
        if (Input.GetKey(KeyCode.D))
        {
            x += 1f;
        }
        if (Input.GetKey(KeyCode.S))
        {
            z -= 1f;
        }
        if (Input.GetKey(KeyCode.W))
        {
            z += 1f;
        }
        
        // Calculate movement direction relative to player rotation
        Vector3 move = (transform.right * x + transform.forward * z).normalized;
        
        // Apply speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        Vector3 targetPlanarVelocity = move * currentSpeed;
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, acceleration * Time.deltaTime);
        controller.Move(planarVelocity * Time.deltaTime);
        
        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    private void HandleMouseLook()
    {
        float keyboardYawInput = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            keyboardYawInput -= 1f;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            keyboardYawInput += 1f;
        }

        float keyboardPitchInput = 0f;
        if (Input.GetKey(KeyCode.UpArrow))
        {
            keyboardPitchInput += 1f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            keyboardPitchInput -= 1f;
        }

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime
            + keyboardYawInput * arrowLookHorizontalSpeed * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime
            + keyboardPitchInput * arrowLookVerticalSpeed * Time.deltaTime;

        float smoothingFactor = 1f - Mathf.Exp(-lookSmoothing * Time.deltaTime);
        smoothedLookX = Mathf.Lerp(smoothedLookX, mouseX, smoothingFactor);
        smoothedLookY = Mathf.Lerp(smoothedLookY, mouseY, smoothingFactor);
        
        // Rotate camera up/down
        xRotation -= smoothedLookY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Rotate player left/right
        transform.Rotate(Vector3.up * smoothedLookX);
    }
}
