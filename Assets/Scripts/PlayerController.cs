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
    [SerializeField] private float jumpHeight = 2f;
    [SerializeField] private float gravity = -9.81f;
    
    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 2500f;
    [SerializeField] private Transform cameraTransform;
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        
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
        
        Debug.Log($"Player forward: {transform.forward}, Camera: {cameraTransform?.name}");
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
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
    }
    
    private void HandleMovement()
    {
        // Get input
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        // Debug movement input
        if (x != 0 || z != 0)
        {
            Debug.Log($"Input - X: {x}, Z: {z} | Forward: {transform.forward} | Right: {transform.right}");
        }
        
        // Calculate movement direction relative to player rotation
        Vector3 move = transform.right * x + transform.forward * z;
        
        // Apply speed
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
        controller.Move(move * currentSpeed * Time.deltaTime);
        
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
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        
        // Rotate camera up/down
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Rotate player left/right
        transform.Rotate(Vector3.up * mouseX);
    }
}
