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

    // Load ground texture
    var groundSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAssetPath(contentRoot, "ground.png"), "Ground texture is missing.");
    _groundTexturePath = groundSpritePath;
    _groundTextureSize = AssetHelper.GetTextureSize(groundSpritePath);
    
    // Create player (animations loaded in constructor)
    var playerScale = 2f;
    var groundHeight = _groundTextureSize.Y;
    var playerHeight = 40 * playerScale;
    var playerStartY = groundHeight + playerHeight / 2f;
    var playerStart = new Vector2(_viewportSize.X / 2f, playerStartY);
    
    _player = new Player(playerStart, contentRoot, playerScale);

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
}
