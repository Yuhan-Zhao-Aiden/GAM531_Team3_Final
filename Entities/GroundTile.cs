using knight.Core;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Entities;

public sealed class GroundTile : SceneObject
{
  public GroundTile(Vector2 position, SpriteRenderer spriteRenderer)
      : base(position, spriteRenderer)
  {
    Velocity = Vector2.Zero;
  }
}
