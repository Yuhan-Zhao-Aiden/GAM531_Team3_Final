using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Entities;

public sealed class Player : AnimatedEntity
{
  // Fixed collision box matches Idle/Run/Jump animations (25x40 unscaled)
  private const float FixedCollisionHeight = 40f;
  private const float FixedCollisionWidth = 25f;

  public Player(Vector2 position, SpriteRenderer spriteRenderer)
      : base(position, spriteRenderer) {}

  public bool IsGrounded { get; set; }
  public bool IsAttacking { get; set; }
  public int AttackComboCount { get; set; }
  public bool Attack2Ready { get; set; }

  // Override Size to use fixed collision box regardless of sprite size
  // This prevents position shifts when switching between different sized animations
  public override Vector2 Size => new Vector2(FixedCollisionWidth * scale, FixedCollisionHeight * scale);

  public override void Draw(Shader shader)
  {
    if (shader is null) throw new ArgumentNullException(nameof(shader));

    // Calculate rendering offset to keep sprite bottom aligned with collision box bottom
    // Attack sprites are 80px tall, normal sprites are 40px tall
    var spriteHeight = SpriteRenderer.CurrentFrameSize.Y;
    var collisionHeight = FixedCollisionHeight;
    var renderOffsetY = (spriteHeight - collisionHeight) * scale / 2f;

    // Create scale matrix that includes directional flipping
    var scaleX = this.scale * (int)FacingDirection;
    var scaleY = this.scale;
    var scaleMatrix = Matrix4.CreateScale(scaleX, scaleY, 1.0f);
    
    // Apply render offset to Y position to keep bottom grounded
    var translation = Matrix4.CreateTranslation(Position.X, Position.Y + renderOffsetY, 0f);
    shader.SetMatrix4("uModel", scaleMatrix * translation);
    SpriteRenderer.Draw();
  }
}
