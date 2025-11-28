using System;
using System.Collections.Generic;
using knight.Core;
using knight.Entities;
using OpenTK.Mathematics;

namespace knight.Systems;

public static class PhysicsSystem
{
  public const float Gravity = -2000f;
  public const float HorizontalDamping = 12f;

  public static void IntegrateVelocity(SceneObject entity, double deltaSeconds)
  {
    var velocity = entity.Velocity;

    // Apply gravity
    velocity.Y += Gravity * (float)deltaSeconds;

    // Apply horizontal damping
    var dampingFactor = Math.Clamp(HorizontalDamping * (float)deltaSeconds, 0f, 1f);
    velocity.X = MathHelper.Lerp(velocity.X, 0f, dampingFactor);

    entity.Velocity = velocity;
    entity.Position += velocity * (float)deltaSeconds;
  }

  public static void ClampToHorizontalBounds(SceneObject entity, float minX, float maxX)
  {
    var entityHalfWidth = entity.Size.X * 0.5f;
    var clampedMinX = minX + entityHalfWidth;
    var clampedMaxX = maxX - entityHalfWidth;
    
    var clampedX = Math.Clamp(entity.Position.X, clampedMinX, clampedMaxX);
    
    if (clampedX != entity.Position.X)
    {
      entity.Velocity = new Vector2(0f, entity.Velocity.Y);
    }
    
    entity.Position = new Vector2(clampedX, entity.Position.Y);
  }

  public static void ResolveGroundCollisions(Player player, IReadOnlyList<GroundTile> groundTiles)
  {
    player.IsGrounded = false;

    if (groundTiles.Count == 0)
    {
      return;
    }

    var playerHalfSize = player.Size * 0.5f;
    var playerHalfWidth = playerHalfSize.X;
    var playerHalfHeight = playerHalfSize.Y;

    foreach (var tile in groundTiles)
    {
      var tileHalfSize = tile.Size * 0.5f;
      var tileHalfWidth = tileHalfSize.X;
      var tileHalfHeight = tileHalfSize.Y;

      var playerLeft = player.Position.X - playerHalfWidth;
      var playerRight = player.Position.X + playerHalfWidth;
      var tileLeft = tile.Position.X - tileHalfWidth;
      var tileRight = tile.Position.X + tileHalfWidth;

      if (playerRight <= tileLeft || playerLeft >= tileRight)
      {
        continue;
      }

      var playerBottom = player.Position.Y - playerHalfHeight;
      var tileTop = tile.Position.Y + tileHalfHeight;
      var tileBottom = tile.Position.Y - tileHalfHeight;

      if (playerBottom < tileTop && playerBottom >= tileBottom && player.Velocity.Y <= 0f)
      {
        var penetration = tileTop - playerBottom;
        player.Position = new Vector2(player.Position.X, player.Position.Y + penetration);
        player.Velocity = new Vector2(player.Velocity.X, 0f);
        player.IsGrounded = true;
        break;
      }
    }
  }
}
