using System;
using knight.Core;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Entities;

public abstract class AnimatedEntity : SceneObject
{
  private string _activeAnimation = string.Empty;

  protected AnimatedEntity(Vector2 position, SpriteRenderer spriteRenderer)
      : base(position, spriteRenderer) {}

  public void PlayAnimation(string animationName)
  {
    if (string.Equals(_activeAnimation, animationName, StringComparison.Ordinal))
    {
      return;
    }

    SpriteRenderer.SetAnimation(animationName, restart: true);
    _activeAnimation = animationName;
  }

  public string ActiveAnimation => _activeAnimation;
}
