using System;
using knight.Core;
using knight.Graphics;
using knight.Systems;
using OpenTK.Mathematics;

namespace knight.Entities;

public sealed class Projectile : AnimatedEntity
{
  private const float ProjectileSpeed = 400f;
  private Vector2 _direction;
  public int Damage { get; } = 10;
  
  // Events
  public event Action? OnExplode;
  
  public Projectile(Vector2 position, Vector2 direction, string contentRoot, Direction facingDirection, float scale = 3f)
      : base(position, CreateProjectileRenderer(contentRoot))
  {
    setScale(scale);
    _direction = Vector2.Normalize(direction);
    FacingDirection = facingDirection;
    Velocity = _direction * ProjectileSpeed;
    PlayAnimation("Travel");
  }

  public bool IsExploding { get; set; }
  public bool ShouldRemove { get; set; }
  private double _explosionTimer;
  private const double ExplosionDuration = 0.3; // Total explosion animation duration (5 frames * 0.06s)

  public override Vector2 Size => IsExploding ? new Vector2(65f * scale, 70f * scale) : new Vector2(29f * scale, 11f * scale);

  public override void Draw(Shader shader)
  {
    if (shader is null) throw new ArgumentNullException(nameof(shader));

    // Create scale matrix that includes directional flipping
    var scaleX = this.scale * (int)FacingDirection;
    var scaleY = this.scale;
    var scaleMatrix = Matrix4.CreateScale(scaleX, scaleY, 1.0f);
    
    var translation = Matrix4.CreateTranslation(Position.X, Position.Y, 0f);
    shader.SetMatrix4("uModel", scaleMatrix * translation);
    shader.SetVector3("uColorTint", new Vector3(1.0f, 1.0f, 1.0f)); // No tint
    SpriteRenderer.Draw();
  }

  public void UpdateProjectile(double deltaSeconds)
  {
    if (IsExploding)
    {
      _explosionTimer += deltaSeconds;
      
      // Remove projectile after explosion animation completes
      if (SpriteRenderer.IsAnimationFinished() || _explosionTimer >= ExplosionDuration)
      {
        ShouldRemove = true;
      }
      return;
    }

    // Update position
    Position += Velocity * (float)deltaSeconds;
  }

  public void Explode()
  {
    if (!IsExploding)
    {
      IsExploding = true;
      Velocity = Vector2.Zero;
      _explosionTimer = 0;
      PlayAnimation("Explode");
      OnExplode?.Invoke();
    }
  }

  public bool CheckCollisionWith(SceneObject other)
  {
    if (other is null || IsExploding) return false;

    var thisHalfSize = Size * 0.5f;
    var otherHalfSize = other.Size * 0.5f;

    var thisLeft = Position.X - thisHalfSize.X;
    var thisRight = Position.X + thisHalfSize.X;
    var thisBottom = Position.Y - thisHalfSize.Y;
    var thisTop = Position.Y + thisHalfSize.Y;

    var otherLeft = other.Position.X - otherHalfSize.X;
    var otherRight = other.Position.X + otherHalfSize.X;
    var otherBottom = other.Position.Y - otherHalfSize.Y;
    var otherTop = other.Position.Y + otherHalfSize.Y;

    return !(thisRight < otherLeft || thisLeft > otherRight || 
             thisTop < otherBottom || thisBottom > otherTop);
  }

  private static SpriteRenderer CreateProjectileRenderer(string contentRoot)
  {
    var renderer = new SpriteRenderer();

    // Load projectile sprite from Animation/Enemy/SingleCharge.png (no cropping needed)
    var chargePath = AssetHelper.RequireAsset(
      AssetHelper.GetAnimationPath(contentRoot, System.IO.Path.Combine("Enemy", "SingleCharge.png")),
      "Projectile SingleCharge sprite is missing.");

    // Get the full texture size (29x11)
    var textureSize = AssetHelper.GetTextureSize(chargePath);

    // Load the entire image as a single frame (no cropping)
    var travelFrameOrigins = new System.Collections.Generic.List<Vector2i>
    {
      new Vector2i(0, 0)
    };

    // Load animation using full texture dimensions
    renderer.LoadAnimation("Travel", chargePath, frameWidth: textureSize.X, frameHeight: textureSize.Y, travelFrameOrigins, frameDurationSeconds: 1.0, loop: true);

    // Load explosion animation from Charge.png sprite sheet
    var explosionPath = AssetHelper.RequireAsset(
      AssetHelper.GetAnimationPath(contentRoot, System.IO.Path.Combine("Enemy", "Charge.png")),
      "Explosion sprite sheet is missing.");

    // Explosion animation - 5 frames with irregular positions (65x70 each, flips with projectile direction)
    var explosionFrameOrigins = new System.Collections.Generic.List<Vector2i>
    {
      new Vector2i(309, 30),  // Frame 1
      new Vector2i(430, 30),  // Frame 2
      new Vector2i(558, 30),  // Frame 3
      new Vector2i(682, 28),  // Frame 4
      new Vector2i(810, 25)   // Frame 5
    };

    renderer.LoadAnimation("Explode", explosionPath, frameWidth: 65, frameHeight: 70, explosionFrameOrigins, frameDurationSeconds: 0.06, loop: false);

    return renderer;
  }
}
