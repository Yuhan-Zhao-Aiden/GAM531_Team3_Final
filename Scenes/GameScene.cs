using System;
using System.Collections.Generic;
using System.IO;
using knight.Core;
using knight.Entities;
using knight.Graphics;
using knight.Systems;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SixLabors.ImageSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace knight.Scenes;

public sealed class GameScene : IScene
{
  private Player? _player;
  private readonly List<GroundTile> _groundTiles = new();
  private string? _groundTexturePath;
  private Vector2i _groundTextureSize;
  private Vector2i _viewportSize;
  private KeyboardState? _keyboard;

  private const double MaxDeltaSeconds = 1d / 60d;

  public void SetKeyboardState(KeyboardState keyboard)
  {
    _keyboard = keyboard;
  }

  public void Initialize(string contentRoot, Vector2i viewportSize)
  {
    _viewportSize = viewportSize;

    var playerRenderer = new SpriteRenderer();

    var idleSpritePath = RequireAsset(Path.Combine(contentRoot, "Animation", "_Idle.png"), "Idle sprite sheet is missing.");
    var jumpSpritePath = RequireAsset(Path.Combine(contentRoot, "Animation", "_Jump.png"), "Jump sprite sheet is missing.");
    var fallSpritePath = RequireAsset(Path.Combine(contentRoot, "Animation", "_Fall.png"), "Fall sprite sheet is missing.");
    var runSpritePath = RequireAsset(Path.Combine(contentRoot, "Animation", "_Run.png"), "Run sprite sheet is missing");
    var attackSpritePath = RequireAsset(Path.Combine(contentRoot, "Animation", "_Attack.png"), "Attack sprite sheet is missing.");
    var attack2SpritePath = RequireAsset(Path.Combine(contentRoot, "Animation", "_Attack2.png"), "Attack2 sprite sheet is missing.");

    var idleFrameOrigins = new List<Vector2i>(10);
    var runFrameOrigins = new List<Vector2i>(10);
    for (var i = 0; i < 10; i++)
    {
      idleFrameOrigins.Add(new Vector2i(40 + i * 120, 0));
      runFrameOrigins.Add(new Vector2i(40 + i * 120, 0));
    }

    var jumpFrameOrigins = new List<Vector2i>(3);
    var fallFrameOrigins = new List<Vector2i>(3);
    for (var i = 0; i < 3; i++)
    {
      var x = 40 + i * 120;
      jumpFrameOrigins.Add(new Vector2i(x, 0));
      fallFrameOrigins.Add(new Vector2i(x, 0));
    }

    // Attack animation - irregular frame positions
    // x ranges: [35, 75] | [175, 237] | [292, 358] | [412, 472]
    // Sprite content starts at y=35 in paint, but we want to capture from y=0 (the empty space above gets included)
    // Actually, let's start from y=0 to capture the full 80px height
    var attackFrameOrigins = new List<Vector2i>
    {
      new Vector2i(35, 0),   // Frame 1: x=35
      new Vector2i(175, 0),  // Frame 2: x=175
      new Vector2i(292, 0),  // Frame 3: x=292
      new Vector2i(412, 0)   // Frame 4: x=412
    };

    // Attack2 animation - irregular frame positions
    // x ranges: [40, 80] | [157, 200] | [272, 350] | [390, 455] | [510, 556] | [630, 674]
    var attack2FrameOrigins = new List<Vector2i>
    {
      new Vector2i(40, 0),   // Frame 1: x=40
      new Vector2i(157, 0),  // Frame 2: x=157
      new Vector2i(272, 0),  // Frame 3: x=272
      new Vector2i(390, 0),  // Frame 4: x=390
      new Vector2i(510, 0),  // Frame 5: x=510
      new Vector2i(630, 0)   // Frame 6: x=630
    };

    playerRenderer.LoadAnimation("Idle", idleSpritePath, frameWidth: 25, frameHeight: 40, idleFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    playerRenderer.LoadAnimation("Jump", jumpSpritePath, frameWidth: 30, frameHeight: 40, jumpFrameOrigins, frameDurationSeconds: 0.1, loop: false);
    playerRenderer.LoadAnimation("Fall", fallSpritePath, frameWidth: 30, frameHeight: 40, fallFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    playerRenderer.LoadAnimation("Run", runSpritePath, frameWidth: 25, frameHeight: 40, runFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    // Attack sprites: Use full 80px height, starting from y=0
    playerRenderer.LoadAnimation("Attack", attackSpritePath, frameWidth: 66, frameHeight: 80, attackFrameOrigins, frameDurationSeconds: 0.08, loop: false);
    playerRenderer.LoadAnimation("Attack2", attack2SpritePath, frameWidth: 78, frameHeight: 80, attack2FrameOrigins, frameDurationSeconds: 0.08, loop: false);

    var groundSpritePath = RequireAsset(Path.Combine(contentRoot, "Assets", "ground.png"), "Ground texture is missing.");
    _groundTexturePath = groundSpritePath;
    _groundTextureSize = IdentifyTextureSize(groundSpritePath);
    
    // Position player on the ground properly
    var playerScale = 2f;
    var groundHeight = _groundTextureSize.Y;
    var playerHeight = 40 * playerScale;
    var playerStartY = groundHeight + playerHeight / 2f;
    var playerStart = new Vector2(_viewportSize.X / 2f, playerStartY);
    
    _player = new Player(playerStart, playerRenderer);
    _player.setScale(playerScale);
    _player.PlayAnimation("Idle");

    BuildGroundTiles();
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
    PhysicsSystem.ResolveGroundCollisions(_player, _groundTiles);
    
    _player.Update(dt);

    foreach (var tile in _groundTiles)
    {
      tile.Update(dt);
    }
  }

  public void Draw(Shader shader, Camera camera)
  {
    if (_player is null || shader is null)
    {
      return;
    }

    shader.Use();
    shader.SetMatrix4("uView", camera.ViewMatrix);

    foreach (var tile in _groundTiles)
    {
      tile.Draw(shader);
    }

    _player.Draw(shader);
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

    BuildGroundTiles();
  }

  public void Unload()
  {
    _player?.Dispose();

    foreach (var tile in _groundTiles)
    {
      tile.Dispose();
    }

    _groundTiles.Clear();
  }

  public Player? Player => _player;

  private void BuildGroundTiles()
  {
    foreach (var tile in _groundTiles)
    {
      tile.Dispose();
    }
    _groundTiles.Clear();

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

    var tilesNeeded = (int)Math.Ceiling(_viewportSize.X / (float)tileWidth) + 1;

    for (var i = 0; i < tilesNeeded; i++)
    {
      var renderer = new SpriteRenderer();
      renderer.LoadAnimation("Static", _groundTexturePath, tileWidth, tileHeight, frameDurationSeconds: 0, loop: true);
      renderer.SetAnimation("Static", true);

      var x = i * tileWidth + tileWidth / 2f;
      var position = new Vector2(x, tileHeight / 2f);
      var tile = new GroundTile(position, renderer);
      _groundTiles.Add(tile);
    }

    // mountain
    for (var i = 0; i < 4; i++) 
    {
      var renderer = new SpriteRenderer();
      renderer.LoadAnimation("Static", _groundTexturePath, tileWidth, tileHeight, frameDurationSeconds: 0, loop: true);
      renderer.SetAnimation("Static", true);
      var y = (3.2f/2 + i) * tileHeight;
      for (var j = 4 - i; j > 0; j--) 
      {
        var x = 500 - j * tileWidth;
        var tile = new GroundTile(new Vector2(x, y), renderer);
        _groundTiles.Add(tile);
      }
    }
  }

  private static string RequireAsset(string path, string errorMessage)
  {
    if (File.Exists(path))
    {
      return path;
    }

    throw new FileNotFoundException(errorMessage, path);
  }

  private static Vector2i IdentifyTextureSize(string filePath)
  {
    var info = ImageSharpImage.Identify(filePath);
    if (info is null)
    {
      throw new InvalidOperationException($"Unable to identify texture dimensions for '{filePath}'.");
    }

    return new Vector2i(info.Width, info.Height);
  }
}
