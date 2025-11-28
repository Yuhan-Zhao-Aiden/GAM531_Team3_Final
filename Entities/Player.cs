using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Entities;

public sealed class Player : AnimatedEntity
{
  public Player(Vector2 position, SpriteRenderer spriteRenderer)
      : base(position, spriteRenderer) {}

  public bool IsGrounded { get; set; }
}
