# Quick Reference

## Folder Structure
```
knight/
├── Core/           - Camera, SceneObject (base entity)
├── Graphics/       - Shader, SpriteRenderer
├── Entities/       - AnimatedEntity, Player, GroundTile
├── Scenes/         - IScene, SceneManager, GameScene
├── Systems/        - PhysicsSystem, InputSystem
├── Game.cs         - Main game window
└── Program.cs      - Entry point
```

## Namespaces
- `knight.Core` - Core systems (Camera, SceneObject)
- `knight.Graphics` - Rendering (Shader, SpriteRenderer)
- `knight.Entities` - Game objects (Player, GroundTile, etc.)
- `knight.Scenes` - Scene management
- `knight.Systems` - Game logic systems
- `knight` - Game, Program

## Adding Features Quick Guide

### New Entity
1. Create in `Entities/`
2. Inherit from `AnimatedEntity` or `SceneObject`
3. Add to scene's entity list
4. Update/Draw in scene

### New Scene
1. Create in `Scenes/`
2. Implement `IScene`
3. Load via `sceneManager.LoadScene(new YourScene())`

### New System
1. Create in `Systems/`
2. Static class with static methods
3. Call from scene's Update()

## Important Files to Check
- `ARCHITECTURE.md` - Full documentation
- `Game.cs` - See how camera/scenes work together
- `GameScene.cs` - Example of scene implementation
- `Systems/` - Example of system pattern

## Build Commands
```bash
dotnet build    # Build
dotnet run      # Run
dotnet clean    # Clean
```
