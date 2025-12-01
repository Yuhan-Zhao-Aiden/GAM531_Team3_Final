using System;
using System.Collections.Generic;
using knight.Core;
using knight.Entities;
using knight.Graphics;
using knight.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace knight.Scenes;

public sealed class GameScene : IScene
{
    private Player? _player;
    private Enemy? _enemy;
    private readonly List<GroundTile> _platforms = new();
    private readonly List<Projectile> _projectiles = new();
    private string? _groundTexturePath;
    private Vector2i _groundTextureSize;

    // Four different platform textures
    private string? _platform1TexturePath;
    private Vector2i _platform1TextureSize;
    private string? _platform2TexturePath;
    private Vector2i _platform2TextureSize;
    private string? _platform3TexturePath;
    private Vector2i _platform3TextureSize;
    private string? _platform4TexturePath;
    private Vector2i _platform4TextureSize;

    private Vector2i _viewportSize;
    private KeyboardState? _keyboard;

    // Background fields
    private SpriteRenderer? _backgroundRenderer;
    private GroundTile? _background;

    private const double MaxDeltaSeconds = 1d / 60d;

    public void SetKeyboardState(KeyboardState keyboard)
    {
        _keyboard = keyboard;
    }

    public void Initialize(string contentRoot, Vector2i viewportSize)
    {
        _viewportSize = viewportSize;

        // Load background texture
        var backgroundPath = AssetHelper.GetAssetPath(contentRoot, "background.png");
        if (System.IO.File.Exists(backgroundPath))
        {
            var bgSize = AssetHelper.GetTextureSize(backgroundPath);
            _backgroundRenderer = new SpriteRenderer();
            _backgroundRenderer.LoadAnimation("Static", backgroundPath, bgSize.X, bgSize.Y, frameDurationSeconds: 0, loop: true);
            _backgroundRenderer.SetAnimation("Static", true);

            var bgPosition = new Vector2(viewportSize.X / 2f, viewportSize.Y / 2f);
            _background = new GroundTile(bgPosition, _backgroundRenderer);

            var scaleX = (float)viewportSize.X / bgSize.X;
            var scaleY = (float)viewportSize.Y / bgSize.Y;
            var scale = Math.Max(scaleX, scaleY);
            _background.setScale(scale);
        }

        // Load ground texture (for the bottom floor)
        var groundSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAssetPath(contentRoot, "ground.png"), "Ground texture is missing.");
        _groundTexturePath = groundSpritePath;
        _groundTextureSize = AssetHelper.GetTextureSize(groundSpritePath);

        // Load platform textures (for floating platforms)
        // You can name these files whatever you want: platform1.png, platform2.png, etc.
        LoadPlatformTexture(contentRoot, "platform1.png", ref _platform1TexturePath, ref _platform1TextureSize);
        LoadPlatformTexture(contentRoot, "platform2.png", ref _platform2TexturePath, ref _platform2TextureSize);
        LoadPlatformTexture(contentRoot, "platform3.png", ref _platform3TexturePath, ref _platform3TextureSize);
        LoadPlatformTexture(contentRoot, "platform4.png", ref _platform4TexturePath, ref _platform4TextureSize);

        // Create player
        var playerScale = 2f;
        var groundHeight = _groundTextureSize.Y;
        var playerHeight = 40 * playerScale;
        var playerStartY = groundHeight + playerHeight / 2f;
        var playerStart = new Vector2(_viewportSize.X / 2f, playerStartY);

        _player = new Player(playerStart, contentRoot, playerScale);

        // Create enemy on the right side of the screen
        var enemyScale = 2f;
        var enemyHeight = 90 * enemyScale;
        var enemyStartY = groundHeight + enemyHeight / 2f;
        var enemyStart = new Vector2(_viewportSize.X - 300f, enemyStartY);
        _enemy = new Enemy(enemyStart, contentRoot, _player, enemyScale);
        _enemy.OnProjectileFired += projectile =>
        {
            _projectiles.Add(projectile);
        };

        BuildLevel();
    }

    public void Update(double deltaSeconds)
    {
        if (_player is null)
        {
            return;
        }

        var dt = Math.Min(deltaSeconds, MaxDeltaSeconds);

        if (_keyboard is not null)
        {
            InputSystem.HandlePlayerInput(_player, _keyboard);
            InputSystem.UpdatePlayerAnimation(_player, _keyboard);
        }

        PhysicsSystem.IntegrateVelocity(_player, dt);
        PhysicsSystem.ClampToHorizontalBounds(_player, 0, _viewportSize.X);
        PhysicsSystem.ResolveGroundCollisions(_player, _platforms);
        _player.UpdateCooldowns(dt);
        _player.Update(dt);

        // Update enemy
        if (_enemy is not null)
        {
            _enemy.UpdateAI(dt);
            
            // Only apply physics if not attacking (attacking enemy should stay completely stationary)
            if (!_enemy.IsAttacking)
            {
                PhysicsSystem.IntegrateVelocity(_enemy, dt);
            }
            
            PhysicsSystem.ClampToHorizontalBounds(_enemy, 0, _viewportSize.X);
            PhysicsSystem.ResolveGroundCollisionsForEnemy(_enemy, _platforms);
            _enemy.Update(dt);

            // Check if player attack hits enemy (with cooldown to prevent multi-hit)
            if (_player.CanDealDamage)
            {
                var (attackPos, attackSize) = _player.GetAttackHitbox();
                if (attackSize != Vector2.Zero && CollisionSystem.CheckAABBCollision(attackPos, attackSize, _enemy.Position, _enemy.Size))
                {
                    _enemy.TakeDamage(15); // 15 damage per attack hit
                    _player.ResetAttackCooldown(); // Prevent multi-hit during same attack
                }
            }
        }

        // Update projectiles
        for (var i = _projectiles.Count - 1; i >= 0; i--)
        {
            var projectile = _projectiles[i];
            projectile.UpdateProjectile(dt);
            projectile.Update(dt);

            // Check collision with player (ignore if rolling - invulnerable)
            if (_player is not null && !_player.IsInvulnerable && projectile.CheckCollisionWith(_player))
            {
                _player.TakeDamage(projectile.Damage);
                projectile.Explode();
            }

            // Remove projectiles that are off-screen or exploded
            if (projectile.ShouldRemove || 
                projectile.Position.X < -100 || projectile.Position.X > _viewportSize.X + 100 ||
                projectile.Position.Y < -100 || projectile.Position.Y > _viewportSize.Y + 100)
            {
                projectile.Dispose();
                _projectiles.RemoveAt(i);
            }
        }

        foreach (var platform in _platforms)
        {
            platform.Update(dt);
        }

        _background?.Update(dt);
    }

    public void Draw(Shader shader, Camera camera)
    {
        if (_player is null || shader is null)
        {
            return;
        }

        shader.Use();
        shader.SetMatrix4("uView", camera.ViewMatrix);

        // Draw background first (behind everything)
        _background?.Draw(shader);

        // Draw platforms
        foreach (var platform in _platforms)
        {
            platform.Draw(shader);
        }

        // Draw player
        _player.Draw(shader);
        
        // Draw enemy
        _enemy?.Draw(shader);

        // Draw projectiles
        foreach (var projectile in _projectiles)
        {
            projectile.Draw(shader);
        }
    }

    public void OnResize(Vector2i newSize)
    {
        _viewportSize = newSize;

        if (_player is not null)
        {
            var groundHeight = _groundTextureSize.Y;
            var playerHeight = _player.Size.Y;
            var playerY = groundHeight + playerHeight / 2f;
            _player.Position = new Vector2(newSize.X / 2f, playerY);
        }

        // Update background size and position on resize
        if (_background is not null)
        {
            var bgTexSize = AssetHelper.GetTextureSize(AssetHelper.GetAssetPath("", "background.png"));
            if (bgTexSize != Vector2i.Zero)
            {
                var scaleX = (float)newSize.X / bgTexSize.X;
                var scaleY = (float)newSize.Y / bgTexSize.Y;
                var scale = Math.Max(scaleX, scaleY);
                _background.setScale(scale);
            }
            _background.Position = new Vector2(newSize.X / 2f, newSize.Y / 2f);
        }

        BuildLevel();
    }

    public void Unload()
    {
        _player?.Dispose();
        _enemy?.Dispose();

        foreach (var platform in _platforms)
        {
            platform.Dispose();
        }

        _platforms.Clear();

        foreach (var projectile in _projectiles)
        {
            projectile.Dispose();
        }

        _projectiles.Clear();

        _background?.Dispose();
    }

    public Player? Player => _player;

    // Enum to identify which platform texture to use
    private enum PlatformType
    {
        Ground,
        Platform1,
        Platform2,
        Platform3,
        Platform4
    }

    // Helper to load a platform texture with fallback to ground
    private void LoadPlatformTexture(string contentRoot, string filename, ref string? texturePath, ref Vector2i textureSize)
    {
        var path = AssetHelper.GetAssetPath(contentRoot, filename);
        if (System.IO.File.Exists(path))
        {
            texturePath = path;
            textureSize = AssetHelper.GetTextureSize(path);
        }
        else
        {
            // Fallback to ground texture if not found
            texturePath = _groundTexturePath;
            textureSize = _groundTextureSize;
        }
    }

    // Get the width of a platform type
    private float GetPlatformWidth(PlatformType type)
    {
        return type switch
        {
            PlatformType.Ground => _groundTextureSize.X,
            PlatformType.Platform1 => _platform1TextureSize.X,
            PlatformType.Platform2 => _platform2TextureSize.X,
            PlatformType.Platform3 => _platform3TextureSize.X,
            PlatformType.Platform4 => _platform4TextureSize.X,
            _ => _groundTextureSize.X
        };
    }

    // Get the height of a platform type
    private float GetPlatformHeight(PlatformType type)
    {
        return type switch
        {
            PlatformType.Ground => _groundTextureSize.Y,
            PlatformType.Platform1 => _platform1TextureSize.Y,
            PlatformType.Platform2 => _platform2TextureSize.Y,
            PlatformType.Platform3 => _platform3TextureSize.Y,
            PlatformType.Platform4 => _platform4TextureSize.Y,
            _ => _groundTextureSize.Y
        };
    }

    private void BuildLevel()
    {
        foreach (var platform in _platforms)
        {
            platform.Dispose();
        }
        _platforms.Clear();

        if (_groundTexturePath is null || _groundTextureSize == Vector2i.Zero)
        {
            return;
        }

        var tileWidth = _groundTextureSize.X;
        var tileHeight = _groundTextureSize.Y;

        if (tileWidth <= 0 || tileHeight <= 0)
        {
            return;
        }

        // GROUND FLOOR - Use ground texture
        var groundTilesNeeded = (int)Math.Ceiling(_viewportSize.X / (float)tileWidth) + 1;
        for (var i = 0; i < groundTilesNeeded; i++)
        {
            var x = i * tileWidth + tileWidth / 2f;
            var position = new Vector2(x, tileHeight / 2f);
            _platforms.Add(CreateGroundTile(position));  // Use ground texture
        }

        // FLOATING PLATFORMS - Use different platform textures

        // Low platform on the left - Mixed textures
        CreateMixedHorizontalPlatform(100f, tileHeight * 3f, new[] { PlatformType.Platform1, PlatformType.Platform2, PlatformType.Platform3 });

        // Mid-level platform in center - Pattern example
        CreateMixedHorizontalPlatform(_viewportSize.X / 2f - GetPlatformWidth(PlatformType.Platform2), tileHeight * 5f,
            new[] { PlatformType.Platform1, PlatformType.Platform2, PlatformType.Platform1, PlatformType.Platform3, PlatformType.Platform2 });

        // High platform on the right - Alternating pattern
        CreateMixedHorizontalPlatform(_viewportSize.X - 200f, tileHeight * 7f, new[] { PlatformType.Platform4, PlatformType.Platform3, PlatformType.Platform4 });

        // Small stepping stones going up on left side - Each one different
        CreateMixedHorizontalPlatform(150f, tileHeight * 6f, new[] { PlatformType.Platform1, PlatformType.Platform2 });
        CreateMixedHorizontalPlatform(250f, tileHeight * 8f, new[] { PlatformType.Platform3, PlatformType.Platform4 });
        CreateMixedHorizontalPlatform(350f, tileHeight * 10f, new[] { PlatformType.Platform2, PlatformType.Platform1 });

        // Floating island in upper middle - Rainbow pattern
        CreateMixedHorizontalPlatform(_viewportSize.X / 2f, tileHeight * 9f,
            new[] { PlatformType.Platform1, PlatformType.Platform2, PlatformType.Platform3, PlatformType.Platform4, PlatformType.Platform1 });

        // Small scattered platforms for challenge
        _platforms.Add(CreatePlatformTile(new Vector2(400f, tileHeight * 4.5f), PlatformType.Platform2));
        _platforms.Add(CreatePlatformTile(new Vector2(_viewportSize.X - 300f, tileHeight * 6.5f), PlatformType.Platform3));
    }

    // Create a tile with ground texture (for bottom floor)
    private GroundTile CreateGroundTile(Vector2 position)
    {
        var renderer = new SpriteRenderer();
        renderer.LoadAnimation("Static", _groundTexturePath!, _groundTextureSize.X, _groundTextureSize.Y, frameDurationSeconds: 0, loop: true);
        renderer.SetAnimation("Static", true);
        return new GroundTile(position, renderer);
    }

    // Create a tile with specified platform texture
    private GroundTile CreatePlatformTile(Vector2 position, PlatformType type)
    {
        string? texturePath = null;
        Vector2i textureSize = Vector2i.Zero;

        switch (type)
        {
            case PlatformType.Ground:
                texturePath = _groundTexturePath;
                textureSize = _groundTextureSize;
                break;
            case PlatformType.Platform1:
                texturePath = _platform1TexturePath;
                textureSize = _platform1TextureSize;
                break;
            case PlatformType.Platform2:
                texturePath = _platform2TexturePath;
                textureSize = _platform2TextureSize;
                break;
            case PlatformType.Platform3:
                texturePath = _platform3TexturePath;
                textureSize = _platform3TextureSize;
                break;
            case PlatformType.Platform4:
                texturePath = _platform4TexturePath;
                textureSize = _platform4TextureSize;
                break;
        }

        if (texturePath == null)
        {
            // Fallback to ground
            texturePath = _groundTexturePath;
            textureSize = _groundTextureSize;
        }

        var renderer = new SpriteRenderer();
        renderer.LoadAnimation("Static", texturePath!, textureSize.X, textureSize.Y, frameDurationSeconds: 0, loop: true);
        renderer.SetAnimation("Static", true);
        return new GroundTile(position, renderer);
    }

    private void CreateHorizontalPlatform(float startX, float y, int length, PlatformType type)
    {
        var tileWidth = GetPlatformWidth(type);
        for (var i = 0; i < length; i++)
        {
            var x = startX + i * tileWidth;
            _platforms.Add(CreatePlatformTile(new Vector2(x, y), type));
        }
    }

    // NEW: Create a horizontal platform with mixed textures
    private void CreateMixedHorizontalPlatform(float startX, float y, PlatformType[] types)
    {
        var currentX = startX;
        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            _platforms.Add(CreatePlatformTile(new Vector2(currentX, y), type));
            currentX += GetPlatformWidth(type); // Move by this tile's width
        }
    }

    private void CreateVerticalPlatform(float x, float startY, int height, PlatformType type)
    {
        var tileHeight = GetPlatformHeight(type);
        for (var i = 0; i < height; i++)
        {
            var y = startY + i * tileHeight;
            _platforms.Add(CreatePlatformTile(new Vector2(x, y), type));
        }
    }

    // NEW: Create a vertical platform with mixed textures
    private void CreateMixedVerticalPlatform(float x, float startY, PlatformType[] types)
    {
        var currentY = startY;
        for (var i = 0; i < types.Length; i++)
        {
            var type = types[i];
            _platforms.Add(CreatePlatformTile(new Vector2(x, currentY), type));
            currentY += GetPlatformHeight(type); // Move by this tile's height
        }
    }
}