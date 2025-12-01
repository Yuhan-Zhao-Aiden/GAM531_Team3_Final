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

    // Handle roll input (K key)
    if (keyboard.IsKeyPressed(Keys.K) && player.IsGrounded && !player.IsRolling && !player.IsAttacking)
    {
      player.IsRolling = true;
      var rollSpeed = 600f * (int)player.FacingDirection;
      player.Velocity = new Vector2(rollSpeed, player.Velocity.Y);
      return; 
    }

    // Handle attack input
    if (keyboard.IsKeyPressed(Keys.J) && player.IsGrounded && !player.IsAttacking)
    {
      player.IsAttacking = true;
      player.AttackComboCount = 1;
      player.Attack2Ready = false;
      player.Velocity = Vector2.Zero; // Stop all movement
      return; // Don't process other inputs during attack
    }

    // Check for attack combo (Attack2) - can queue it up while Attack is playing
    if (keyboard.IsKeyPressed(Keys.J) && player.IsAttacking && player.AttackComboCount == 1)
    {
      player.Attack2Ready = true; // Queue up Attack2
      return;
    }

    // Don't allow movement during attack - keep velocity at zero
    if (player.IsAttacking)
    {
      player.Velocity = new Vector2(0, player.Velocity.Y); // Allow gravity but no horizontal movement
      return;
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

  public static void UpdatePlayerAnimation(Player player, KeyboardState keyboard)
  {
    const float idleSpeedThreshold = 5f;

    // Handle roll animation
    if (player.IsRolling)
    {
      player.PlayAnimation("Roll");
      if (player.SpriteRenderer.IsAnimationFinished())
      {
        player.IsRolling = false;
      }
      return;
    }

    // Handle attack animations
    if (player.IsAttacking)
    {
      if (player.AttackComboCount == 1)
      {
        player.PlayAnimation("Attack");
        // Check if attack finished
        if (player.SpriteRenderer.IsAnimationFinished())
        {
          // Transition to Attack2 if queued, otherwise end attack
          if (player.Attack2Ready)
          {
            player.AttackComboCount = 2;
            player.Attack2Ready = false;
            player.Velocity = Vector2.Zero;
          }
          else
          {
            player.IsAttacking = false;
            player.AttackComboCount = 0;
          }
        }
      }
      else if (player.AttackComboCount == 2)
      {
        player.PlayAnimation("Attack2");
        if (player.SpriteRenderer.IsAnimationFinished())
        {
          player.IsAttacking = false;
          player.AttackComboCount = 0;
          player.Attack2Ready = false;
        }
      }
      return;
    }

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
