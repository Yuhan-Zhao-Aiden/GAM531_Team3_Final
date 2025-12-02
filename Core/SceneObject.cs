using System;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Core;

public enum Direction
{
  Right = 1,
  Left = -1
}

public abstract class SceneObject : IDisposable
{
  protected SceneObject(Vector2 position, SpriteRenderer spriteRenderer)
  {
    Position = position;
    SpriteRenderer = spriteRenderer ?? throw new ArgumentNullException(nameof(spriteRenderer));
  }

  public Vector2 Position { get; set; }
  public Vector2 Velocity { get; set; }
  public SpriteRenderer SpriteRenderer { get; }
  public float scale = 1f;
  public Direction FacingDirection { get; set; } = Direction.Right;

  public virtual Vector2 Size => SpriteRenderer.CurrentFrameSize * scale;
  public Box2 Bounds => new(Position - Size * 0.5f, Position + Size * 0.5f);
  public void setScale(float newScale) => this.scale = newScale;

  public virtual void Update(double deltaSeconds)
    => SpriteRenderer.Update(deltaSeconds);

  public virtual void Draw(Shader shader)
  {
    if (shader is null) throw new ArgumentNullException(nameof(shader));

    // Create scale matrix that includes directional flipping
    var scaleX = this.scale * (int)FacingDirection; // Flip horizontally if facing left
    var scaleY = this.scale;
    var scaleMatrix = Matrix4.CreateScale(scaleX, scaleY, 1.0f);
    
    var translation = Matrix4.CreateTranslation(Position.X, Position.Y, 0f);
    shader.SetMatrix4("uModel", scaleMatrix * translation);
    shader.SetVector3("uColorTint", new Vector3(1.0f, 1.0f, 1.0f)); // No tint
    SpriteRenderer.Draw();
  }

  public virtual void Dispose()
    => SpriteRenderer.Dispose();
}
