using System;
using OpenTK.Mathematics;

namespace knight.Core;

public sealed class Camera
{
  private Vector2 _position;
  private Vector2 _viewportSize;

  public Camera(Vector2 position, Vector2 viewportSize)
  {
    _position = position;
    _viewportSize = viewportSize;
  }

  public Vector2 Position
  {
    get => _position;
    set => _position = value;
  }

  public Vector2 ViewportSize
  {
    get => _viewportSize;
    set => _viewportSize = value;
  }

  public Matrix4 ViewMatrix
  {
    get
    {
      // Center the camera on its position
      var x = -_position.X + _viewportSize.X / 2f;
      var y = -_position.Y + _viewportSize.Y / 2f;
      return Matrix4.CreateTranslation(x, y, 0f);
    }
  }

  public void FollowTarget(Vector2 targetPosition, float smoothing = 1f)
  {
    if (smoothing <= 0f)
    {
      _position = targetPosition;
    }
    else
    {
      var delta = targetPosition - _position;
      _position += delta * Math.Clamp(smoothing, 0f, 1f);
    }
  }

  public void FollowTargetClamped(Vector2 targetPosition, Box2 worldBounds, float smoothing = 1f)
  {
    FollowTarget(targetPosition, smoothing);

    // Clamp camera so it doesn't show outside world bounds
    var halfWidth = _viewportSize.X / 2f;
    var halfHeight = _viewportSize.Y / 2f;

    var minX = worldBounds.Min.X + halfWidth;
    var maxX = worldBounds.Max.X - halfWidth;
    var minY = worldBounds.Min.Y + halfHeight;
    var maxY = worldBounds.Max.Y - halfHeight;

    // Only clamp if world is larger than viewport
    if (maxX >= minX)
    {
      _position.X = Math.Clamp(_position.X, minX, maxX);
    }
    else
    {
      _position.X = (worldBounds.Min.X + worldBounds.Max.X) / 2f;
    }

    if (maxY >= minY)
    {
      _position.Y = Math.Clamp(_position.Y, minY, maxY);
    }
    else
    {
      _position.Y = (worldBounds.Min.Y + worldBounds.Max.Y) / 2f;
    }
  }

  public Vector2 ScreenToWorld(Vector2 screenPosition)
  {
    return screenPosition + _position - _viewportSize / 2f;
  }

  public Vector2 WorldToScreen(Vector2 worldPosition)
  {
    return worldPosition - _position + _viewportSize / 2f;
  }
}
