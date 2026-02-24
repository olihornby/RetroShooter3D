# Retro Shooter 3D

A Unity 2022.3 LTS 3D shooter game with 8-bit style graphics.

## Setup Instructions

### Prerequisites
- Unity 2022.3 LTS
- Unity Hub (recommended)

### Opening the Project

1. Open Unity Hub
2. Click "Add" → "Add project from disk"
3. Navigate to this folder (`RetroShooter3D`)
4. Select the folder and click "Open"
5. Unity will import the project

### Creating the Player

Since this is a script-only project, you'll need to set up the scene in Unity:

1. **Create the Player GameObject:**
   - In Hierarchy, right-click → 3D Object → Capsule
   - Rename it to "Player"
   - Position: (0, 1, 0)
   - Add Component → Character Controller
   - Add Component → Player Controller (script)
   - Add Component → Voxel Player Model (script)
   - Add Component → Weapon Controller (script)

2. **Set up the Camera:**
   - Create Empty GameObject as child of Player
   - Name it "CameraHolder"
   - Position: (0, 0.5, 0)
   - Move Main Camera as child of CameraHolder
   - Set Camera local position to (0, 0, 0)

3. **Optional: Use a Real 3D Pistol Model:**
   - Import your pistol model (`.fbx`, `.obj`, etc.) into `Assets/`
   - Drag it into the scene once and create a prefab from it
   - Select Player → `WeaponController`
   - Assign the prefab to **Pistol Model Prefab**
   - Tweak **Pistol Local Position / Rotation / Scale** so it lines up in first-person view

4. **Create Ground:**
   - Create 3D Object → Plane
   - Position: (0, 0, 0)
   - Scale: (5, 1, 5)
   - Set Layer to "Ground" (create if needed)

5. **Configure Ground Layer:**
   - Edit → Project Settings → Tags and Layers
   - Add "Ground" layer if not present
   - In Player Controller inspector, set "Ground Mask" to "Ground" layer

6. **Create a Simple Target:**
   - In Hierarchy, right-click → 3D Object → Cube
   - Rename it to "TargetDummy"
   - Position it in front of player, e.g. (0, 1, 10)
   - Add Component → Damageable Target (script)

7. **Enable Random Map Generation:**
   - In Hierarchy, create Empty GameObject named "MapGenerator"
   - Add Component → Random Map Generator (script)
   - (Optional) disable or remove manually placed map geometry to avoid overlap
   - Press Play: a new layout and random enemies are generated each run

### Controls

- **WASD** - Move
- **Mouse** - Look around
- **Space** - Jump
- **Left Shift** - Sprint
- **Left Mouse Button** - Fire weapon
- **Escape** - Unlock cursor

## Project Structure

```
RetroShooter3D/
├── Assets/
│   ├── Scenes/
│   │   └── MainScene.unity
│   └── Scripts/
│       ├── PlayerController.cs      # First-person movement & camera
│       ├── VoxelPlayerModel.cs      # 8-bit player model generator
│       ├── WeaponController.cs      # Projectile weapon firing
│       ├── Projectile.cs            # Projectile movement + impact effects
│       ├── CrosshairUI.cs           # Center-screen aiming crosshair
│       ├── RandomMapGenerator.cs    # Random arena generation each play
│       ├── EnemyAI.cs               # Vision-based enemy chase behavior
│       └── DamageableTarget.cs      # Basic health/damage receiver
└── README.md
```

## Scripts

### PlayerController.cs
Handles first-person movement, jumping, and camera controls.

**Features:**
- WASD movement
- Mouse look with vertical clamping
- Sprint functionality
- Jump with ground detection
- Gravity simulation

### VoxelPlayerModel.cs
Generates a simple voxel-style 8-bit player model at runtime.

**Features:**
- Procedural voxel generation
- Customizable colors
- Low-poly aesthetic
- Automatic model assembly

### WeaponController.cs
Adds a basic first-person weapon with visible projectile shooting.

**Features:**
- Left-click single-shot firing (`Fire1` on click)
- Fire-rate limit
- 8-round magazine with reload cooldown when empty
- Adjustable projectile speed, lifetime, and damage
- Projectile spawn from gun muzzle point (not camera center)
- Supports imported pistol model prefab with transform offsets
- Fallback procedural 8-bit style weapon model
- Grey projectile visuals

### CrosshairUI.cs
Draws a simple center-screen crosshair for aiming.

**Features:**
- Auto-added to camera by `WeaponController`
- Lightweight UI using `OnGUI`
- Adjustable color, size, thickness, and gap
- Shows ammo (`current/max`) and `Reloading...` status text

### Projectile.cs
Handles projectile movement, collision, damage, and impact VFX.

**Features:**
- Physics-based forward projectile motion
- Grey projectile model with short trail
- Layer-mask-based hit filtering
- Impact particle burst for a more realistic hit feel

### DamageableTarget.cs
Adds health and death behavior for shootable targets.

**Features:**
- Configurable max health
- Public `TakeDamage` method
- Destroy or disable on death

### RandomMapGenerator.cs
Builds a new random map layout each time Play starts.

**Features:**
- Randomized walls and cover blocks
- Spawn-safe clear area around player center
- Boundary walls to contain gameplay space
- Configurable seed, map size, and density
- Random enemy spawn points each run

### EnemyAI.cs
Controls simple enemy behavior using line-of-sight detection.

**Features:**
- Chases player only when inside vision radius
- Requires clear line-of-sight to player
- Stops close to player instead of overlapping
- Works with `DamageableTarget` so enemies can be shot and destroyed

## Next Steps

To expand this further into a full shooter:

1. **Add Enemies:**
   - Create enemy AI script
   - Add health system
   - Implement simple pathfinding

2. **Create Level:**
   - Design arena/level geometry
   - Add obstacles and cover
   - Place spawn points

3. **Add UI:**
   - Health bar
   - Ammo counter
   - Crosshair

4. **Polish:**
   - Add sound effects
   - Particle effects for impacts
   - Menu system

## Customization

### Changing Player Colors
In the `VoxelPlayerModel` component:
- **Primary Color** - Head and arms
- **Secondary Color** - Body and legs
- **Voxel Size** - Scale of individual blocks

### Adjusting Movement
In the `PlayerController` component:
- **Move Speed** - Normal walking speed
- **Sprint Speed** - Speed when holding Shift
- **Jump Height** - Height of jumps
- **Mouse Sensitivity** - Camera rotation speed

## License

Free to use and modify.
