using System;
using knight.Core;
using knight.Entities;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace knight.Systems;

public static class InputSystem
{
  public const float JumpImpulse = 750f;

  public static void HandlePlayerInput(Player player, KeyboardState keyboard)
  {
    if (player is null)
    {
      throw new ArgumentNullException(nameof(player));
    }

    if (keyboard is null)
    {
      throw new ArgumentNullException(nameof(keyboard));
    }

    var wantsJump = keyboard.IsKeyPressed(Keys.Space) || keyboard.IsKeyPressed(Keys.W) || keyboard.IsKeyPressed(Keys.Up);
    if (wantsJump && player.IsGrounded)
    {
      player.Velocity = new Vector2(player.Velocity.X, JumpImpulse);
      player.IsGrounded = false;
    }

    // Run and set direction
    if (keyboard.IsKeyDown(Keys.D))
    {
      player.Velocity = new Vector2(500f, player.Velocity.Y);
      player.FacingDirection = Direction.Right;
    }
    if (keyboard.IsKeyDown(Keys.A)) 
    {
      player.Velocity = new Vector2(-500f, player.Velocity.Y);
      player.FacingDirection = Direction.Left;
    }
  }

  public static void UpdatePlayerAnimation(Player player)
  {
    const float idleSpeedThreshold = 5f;

    if (!player.IsGrounded)
    {
      player.PlayAnimation(player.Velocity.Y >= 0 ? "Jump" : "Fall");
      return;
    }

    if (MathF.Abs(player.Velocity.X) > idleSpeedThreshold)
    {
      player.PlayAnimation("Run");
      return;
    }

    player.PlayAnimation("Idle");
  }
}
