using System;
using knight.Core;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Entities;

public sealed class Projectile : AnimatedEntity
{
  private const float ProjectileSpeed = 800f;
  private Vector2 _direction;
  
  public Projectile(Vector2 position, Vector2 direction, string contentRoot, Direction facingDirection, float scale = 6f)
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

  public override Vector2 Size => new Vector2(1024f * scale, 128f * scale); // Using full texture size temporarily

  public override void Draw(Shader shader)
  {
    if (shader is null) throw new ArgumentNullException(nameof(shader));

    Console.WriteLine($"[PROJECTILE] Drawing at {Position}, IsExploding={IsExploding}, ShouldRemove={ShouldRemove}");

    // Create scale matrix that includes directional flipping
    var scaleX = this.scale * (int)FacingDirection;
    var scaleY = this.scale;
    var scaleMatrix = Matrix4.CreateScale(scaleX, scaleY, 1.0f);
    
    var translation = Matrix4.CreateTranslation(Position.X, Position.Y, 0f);
    shader.SetMatrix4("uModel", scaleMatrix * translation);
    SpriteRenderer.Draw();
  }

  public void UpdateProjectile(double deltaSeconds)
  {
    if (IsExploding)
    {
      // TODO: Play explosion animation when implemented
      // For now, just mark for removal
      ShouldRemove = true;
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
      // TODO: PlayAnimation("Explode") when explosion animation is added
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

    // Load projectile sprite sheet from Animation/Enemy/Charge.png
    var chargePath = AssetHelper.RequireAsset(
      AssetHelper.GetAnimationPath(contentRoot, System.IO.Path.Combine("Enemy", "Charge.png")),
      "Projectile Charge sprite sheet is missing.");

    Console.WriteLine($"[PROJECTILE] Loading sprite from: {chargePath}");
    Console.WriteLine($"[PROJECTILE] File exists: {System.IO.File.Exists(chargePath)}");

    // Try loading the entire sprite sheet as a single frame (no cropping)
    // This will show us if the texture loads at all
    var textureSize = AssetHelper.GetTextureSize(chargePath);
    Console.WriteLine($"[PROJECTILE] Texture size: {textureSize.X}x{textureSize.Y}");

    var travelFrameOrigins = new System.Collections.Generic.List<Vector2i>
    {
      new Vector2i(0, 0)
    };

    // Use the full texture as one frame to test if texture loading works
    renderer.LoadAnimation("Travel", chargePath, frameWidth: textureSize.X, frameHeight: textureSize.Y, travelFrameOrigins, frameDurationSeconds: 1.0, loop: true);
    Console.WriteLine($"[PROJECTILE] Animation loaded successfully");

    return renderer;
  }
}
