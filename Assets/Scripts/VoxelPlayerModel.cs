using UnityEngine;

/// <summary>
/// Generates a simple voxel/8-bit style player model
/// Attach to player GameObject to create visual representation
/// </summary>
public class VoxelPlayerModel : MonoBehaviour
{
    [Header("Model Settings")]
    [SerializeField] private Color primaryColor = new Color(0.2f, 0.5f, 0.8f); // Blue
    [SerializeField] private Color secondaryColor = new Color(0.7f, 0.7f, 0.7f); // Gray
    [SerializeField] private float voxelSize = 0.2f;
    [SerializeField] private bool generateOnStart = true;
    
    private void Start()
    {
        if (generateOnStart)
        {
            GeneratePlayerModel();
        }
    }
    
    /// <summary>
    /// Generates a simple blocky player model (8-bit style)
    /// </summary>
    public void GeneratePlayerModel()
    {
        // Create parent for model parts
        GameObject modelParent = new GameObject("PlayerModel");
        modelParent.transform.SetParent(transform);
        modelParent.transform.localPosition = Vector3.zero;
        
        // Head (2x2x2 voxels)
        CreateVoxelCube("Head", modelParent.transform, 
            new Vector3(0, 0.7f, 0), 
            new Vector3(voxelSize * 2, voxelSize * 2, voxelSize * 2), 
            primaryColor);
        
        // Body (2x3x1 voxels)
        CreateVoxelCube("Body", modelParent.transform, 
            new Vector3(0, 0.1f, 0), 
            new Vector3(voxelSize * 2, voxelSize * 3, voxelSize), 
            secondaryColor);
        
        // Left Arm
        CreateVoxelCube("LeftArm", modelParent.transform, 
            new Vector3(-voxelSize * 1.5f, 0.1f, 0), 
            new Vector3(voxelSize, voxelSize * 2.5f, voxelSize), 
            primaryColor);
        
        // Right Arm
        CreateVoxelCube("RightArm", modelParent.transform, 
            new Vector3(voxelSize * 1.5f, 0.1f, 0), 
            new Vector3(voxelSize, voxelSize * 2.5f, voxelSize), 
            primaryColor);
        
        // Left Leg
        CreateVoxelCube("LeftLeg", modelParent.transform, 
            new Vector3(-voxelSize * 0.5f, -0.5f, 0), 
            new Vector3(voxelSize, voxelSize * 2, voxelSize), 
            secondaryColor);
        
        // Right Leg
        CreateVoxelCube("RightLeg", modelParent.transform, 
            new Vector3(voxelSize * 0.5f, -0.5f, 0), 
            new Vector3(voxelSize, voxelSize * 2, voxelSize), 
            secondaryColor);
    }
    
    /// <summary>
    /// Creates a single voxel cube with specified properties
    /// </summary>
    private void CreateVoxelCube(string name, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        
        // Apply color with unlit material for 8-bit look
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.SetFloat("_Glossiness", 0f); // No shine for retro look
        cube.GetComponent<Renderer>().material = mat;
        
        // Remove collider (we'll use CharacterController on parent)
        Destroy(cube.GetComponent<Collider>());
    }
}
