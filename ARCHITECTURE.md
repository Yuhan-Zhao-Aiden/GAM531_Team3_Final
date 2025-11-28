# Project Architecture

## Overview
This 2D game engine is built with C# and OpenTK, organized with clean separation of concerns using namespaces and folders. The architecture is designed to scale easily as you add features like enemies, multiple scenes, UI, and other game objects.

## Folder & Namespace Structure

```
knight/
├── Core/                    # knight.Core - Core game systems
│   ├── Camera.cs           # Camera with smooth following
│   └── SceneObject.cs      # Base class for all game entities
│
├── Graphics/                # knight.Graphics - Rendering
│   ├── Shader.cs           # OpenGL shader management
│   └── SpriteRenderer.cs   # Sprite animation and rendering
│
├── Entities/                # knight.Entities - Game objects
│   ├── AnimatedEntity.cs   # Base for animated entities
│   ├── Player.cs           # Player character
│   └── GroundTile.cs       # Static ground tiles
│
├── Scenes/                  # knight.Scenes - Scene management
│   ├── IScene.cs           # Scene interface
│   ├── SceneManager.cs     # Scene loading/switching
│   └── GameScene.cs        # Main gameplay scene
│
├── Systems/                 # knight.Systems - Game logic systems
│   ├── PhysicsSystem.cs    # Physics (gravity, collisions)
│   └── InputSystem.cs      # Input handling
│
├── Game.cs                  # Main game loop (knight namespace)
└── Program.cs               # Entry point (knight namespace)
```

## Namespace Organization

### `knight.Core`
Core game systems used throughout the project.

**Camera.cs** - Manages viewport and follows targets
- `FollowTarget(targetPosition, smoothing)` - Smooth camera following
- `FollowTargetClamped(targetPosition, worldBounds, smoothing)` - Camera with world bounds
- `ViewMatrix` - Transform for rendering
- `ScreenToWorld()` / `WorldToScreen()` - Coordinate conversions

**SceneObject.cs** - Base class for all game entities
- Properties: `Position`, `Velocity`, `Size`, `Bounds`, `FacingDirection`
- Methods: `Update(deltaSeconds)`, `Draw(shader)`, `Dispose()`
- All entities inherit from this

**Direction** - Enum for entity facing (Right = 1, Left = -1)

### `knight.Graphics`
Rendering and visual systems.

**Shader.cs** - OpenGL shader program wrapper
- Compiles and links vertex/fragment shaders
- `SetMatrix4()`, `SetInt()` - Set uniforms
- Manages shader lifecycle

**SpriteRenderer.cs** - Sprite animation system
- `LoadAnimation()` - Load sprite sheets with frame origins
- `SetAnimation()` - Switch between animations
- `Update()` - Advance animation frames
- `Draw()` - Render current frame

### `knight.Entities`
All game objects that appear in the world.

**AnimatedEntity.cs** - Base for animated entities
- Inherits from `SceneObject`
- `PlayAnimation(name)` - Play animation without restart if already playing
- Common animation logic

**Player.cs** - Player character
- Inherits from `AnimatedEntity`
- `IsGrounded` property for jump logic
- Supports Idle, Run, Jump, Fall animations

**GroundTile.cs** - Static ground tiles
- Inherits from `SceneObject`
- Used for platforms and terrain

### `knight.Scenes`
Scene management for different game states.

**IScene** - Interface all scenes implement
```csharp
void Initialize(string contentRoot, Vector2i viewportSize);
void Update(double deltaSeconds);
void Draw(Shader shader, Camera camera);
void OnResize(Vector2i newSize);
void Unload();
```

**SceneManager.cs** - Manages scene transitions
- `LoadScene(scene)` - Queue next scene
- `Update()` - Handles scene transitions
- `Draw()` - Renders current scene

**GameScene.cs** - Main gameplay scene
- Manages player, ground tiles
- Handles physics/input via Systems
- Builds level geometry
- Exposes `Player` property for camera

### `knight.Systems`
Static systems that operate on entities.

**PhysicsSystem.cs** - Physics calculations
- `IntegrateVelocity()` - Apply gravity and damping
- `ClampToHorizontalBounds()` - Keep entities in bounds
- `ResolveGroundCollisions()` - AABB collision detection

**InputSystem.cs** - Input handling
- `HandlePlayerInput()` - Process keyboard input
- `UpdatePlayerAnimation()` - Select animation based on state

### `knight` (Root)

**Game.cs** - Main game window
- OpenGL context, shader, camera, scene manager
- Delegates update/draw to current scene
- ~100 lines (down from 600+)

**Program.cs** - Entry point

## Design Patterns

### Entity-Component Pattern (Simplified)
- `SceneObject` is base entity
- Systems operate on entities
- Composition through inheritance and systems

### Scene Graph
- `SceneManager` manages scene lifecycle
- Scenes own and manage entities
- Easy to add menus, levels, cutscenes

### Separation of Concerns
- **Game.cs**: Window management, OpenGL setup
- **Scenes**: Entity management, level logic
- **Systems**: Reusable game logic
- **Entities**: Entity-specific state/behavior
- **Graphics**: Rendering details

### Namespace Organization
- Related classes grouped by purpose
- Clear import paths
- Prevents naming conflicts
- Intuitive navigation

## Adding New Features

### Adding an Enemy

1. **Create Enemy.cs** in `Entities/`:
```csharp
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Entities;

public sealed class Enemy : AnimatedEntity
{
  public Enemy(Vector2 position, SpriteRenderer spriteRenderer)
      : base(position, spriteRenderer) {}

  public EnemyState State { get; set; } = EnemyState.Patrol;
}

public enum EnemyState { Patrol, Chase, Attack }
```

2. **Add AI logic** in `Systems/AISystem.cs`:
```csharp
using knight.Entities;
using OpenTK.Mathematics;

namespace knight.Systems;

public static class AISystem
{
  public static void UpdateEnemy(Enemy enemy, Player player, double deltaSeconds)
  {
    var distance = Vector2.Distance(enemy.Position, player.Position);
    
    if (distance < 200f)
    {
      enemy.State = EnemyState.Chase;
      // Move toward player
    }
    else
    {
      enemy.State = EnemyState.Patrol;
      // Patrol logic
    }
  }
}
```

3. **Update GameScene.cs**:
```csharp
private readonly List<Enemy> _enemies = new();

public void Initialize(...)
{
  var enemyRenderer = new SpriteRenderer();
  enemyRenderer.LoadAnimation("Walk", enemyPath, ...);
  
  var enemy = new Enemy(position, enemyRenderer);
  _enemies.Add(enemy);
}

public void Update(double deltaSeconds)
{
  foreach (var enemy in _enemies)
  {
    AISystem.UpdateEnemy(enemy, _player, deltaSeconds);
    enemy.Update(deltaSeconds);
  }
}

public void Draw(Shader shader, Camera camera)
{
  foreach (var enemy in _enemies)
  {
    enemy.Draw(shader);
  }
}
```

### Adding a New Scene (Main Menu)

1. **Create MenuScene.cs** in `Scenes/`:
```csharp
using knight.Core;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Scenes;

public sealed class MenuScene : IScene
{
  public void Initialize(string contentRoot, Vector2i viewportSize)
  {
    // Load menu assets
  }

  public void Update(double deltaSeconds)
  {
    // Handle menu input
    // If start: sceneManager.LoadScene(new GameScene());
  }

  public void Draw(Shader shader, Camera camera)
  {
    // Render menu
  }

  public void OnResize(Vector2i newSize) { }
  public void Unload() { }
}
```

2. **Start with MenuScene** in `Game.cs`:
```csharp
protected override void OnLoad()
{
  // ... existing setup ...
  
  _sceneManager = new SceneManager(_contentRoot, ClientSize);
  var menuScene = new MenuScene();
  _sceneManager.LoadScene(menuScene);
}
```

### Adding a New System

Create static class in `Systems/`:
```csharp
using knight.Entities;

namespace knight.Systems;

public static class CombatSystem
{
  public static bool CheckAttackHit(Player player, Enemy enemy)
  {
    return player.Bounds.Intersects(enemy.Bounds);
  }

  public static void DealDamage(Enemy enemy, int damage)
  {
    enemy.Health -= damage;
  }
}
```

## Camera System

Camera follows player with smooth interpolation:

```csharp
// In Game.cs OnUpdateFrame
if (_gameScene?.Player is not null)
{
  _camera.FollowTarget(_gameScene.Player.Position, smoothing: 0.1f);
}
```

**Smoothing values:**
- `0.1f` - Very smooth, delayed
- `0.5f` - Moderate
- `1.0f` - Instant, no smoothing

**For bounded levels:**
```csharp
var worldBounds = new Box2(0, 0, levelWidth, levelHeight);
_camera.FollowTargetClamped(player.Position, worldBounds, smoothing: 0.1f);
```

## Build & Run

```bash
dotnet build    # Build project
dotnet run      # Run game
dotnet clean    # Clean build
```

## Best Practices

1. **New entities** → Inherit from `SceneObject` or `AnimatedEntity`
2. **Reusable logic** → Add to `Systems/` as static methods
3. **Game states** → Create new `IScene` implementations
4. **Rendering** → Keep in `Graphics/` namespace
5. **Core utilities** → Add to `Core/` namespace
6. **Follow namespaces** → Keep files in folders

## File Naming

- Classes: `PascalCase.cs` (e.g., `Enemy.cs`)
- Interfaces: `IPascalCase.cs` (e.g., `IScene.cs`)
- Systems: `SystemNameSystem.cs` (e.g., `PhysicsSystem.cs`)
- One class per file
- File name matches class name

## Extending the Architecture

- **Multiplayer?** → Create `NetworkSystem.cs` in Systems/
- **UI?** → Create `UI/` folder with `knight.UI` namespace
- **Audio?** → Create `Audio/` folder with `knight.Audio` namespace
- **Editor?** → Create `Tools/` folder

The architecture scales! 🚀
