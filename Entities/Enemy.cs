using System;
using System.Collections.Generic;
using knight.Core;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Entities;

public sealed class Enemy : AnimatedEntity
{
  private const float FixedCollisionHeight = 90f;
  private const float FixedCollisionWidth = 50f;
  
  // AI behavior constants
  private const float IdleDistance = 900f;      // Distance beyond which enemy idles
  private const float OptimalDistance = 400f;   // Distance enemy tries to maintain
  private const float AttackRange = 400f;       // Distance within which enemy attacks
  private const float MoveSpeed = 400f;         // Slower than player (player runs at 500)
  
  private Player? _targetPlayer;
  private double _attackCooldown;
  private int _attackRepeatCount;
  private readonly string _contentRoot;
  private bool _projectileFiredThisAttack;
  private double _damageFlashTimer;
  private const double DamageFlashDuration = 0.5; // 0.5 seconds red flash

  public Enemy(Vector2 position, string contentRoot, Player targetPlayer, float scale = 2f)
      : base(position, CreateEnemyRenderer(contentRoot))
  {
    setScale(scale);
    PlayAnimation("Idle");
    _targetPlayer = targetPlayer;
    _contentRoot = contentRoot;
  }

  public bool IsGrounded { get; set; }
  public bool IsAttacking { get; set; }
  public bool IsDead { get; private set; }

  // Health system
  public int MaxHealth { get; } = 100;
  public int CurrentHealth { get; private set; } = 100;

  public void TakeDamage(int damage)
  {
    if (IsDead || CurrentHealth <= 0) return;
    
    CurrentHealth = Math.Max(0, CurrentHealth - damage);
    _damageFlashTimer = DamageFlashDuration; // Trigger red flash
    Console.WriteLine($"[ENEMY] Health: {CurrentHealth}/{MaxHealth}");
    
    if (CurrentHealth <= 0)
    {
      IsDead = true;
      IsAttacking = false;
      Velocity = Vector2.Zero;
      PlayAnimation("Dead");
      Console.WriteLine("[ENEMY] DEFEATED!");
    }
  }

  public event Action<Projectile>? OnProjectileFired;

  // Override Size to use fixed collision box
  public override Vector2 Size => new Vector2(FixedCollisionWidth * scale, FixedCollisionHeight * scale);

  public override void Draw(Shader shader)
  {
    if (shader is null) throw new ArgumentNullException(nameof(shader));

    // Calculate rendering offset to keep sprite bottom aligned with collision box bottom
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
    
    // Apply damage flash color tint
    if (_damageFlashTimer > 0)
    {
      shader.SetVector3("uColorTint", new Vector3(2.0f, 0.3f, 0.3f)); // Red tint (boost red, reduce green/blue)
    }
    else
    {
      shader.SetVector3("uColorTint", new Vector3(1.0f, 1.0f, 1.0f)); // Normal (white/no tint)
    }
    
    SpriteRenderer.Draw();
  }

  public void UpdateAI(double deltaSeconds)
  {
    if (IsDead || _targetPlayer is null) return;

    // Update damage flash timer
    if (_damageFlashTimer > 0)
    {
      _damageFlashTimer -= deltaSeconds;
    }

    // Update attack cooldown
    if (_attackCooldown > 0)
    {
      _attackCooldown -= deltaSeconds;
    }

    // If attacking, stay stationary and play attack animation 3 times
    if (IsAttacking)
    {
      Velocity = Vector2.Zero; // Stationary during all 3 attacks
      
      // Fire projectile once at the start of attack sequence
      if (!_projectileFiredThisAttack && _targetPlayer is not null)
      {
        FireProjectile();
        _projectileFiredThisAttack = true;
      }
      
      if (SpriteRenderer.IsAnimationFinished())
      {
        _attackRepeatCount++;
        
        // Play attack 3 times before stopping
        if (_attackRepeatCount >= 3)
        {
          IsAttacking = false;
          _attackRepeatCount = 0;
          _attackCooldown = 1.0; // 1 second cooldown between attacks
          _projectileFiredThisAttack = false; // Reset for next attack
        }
        else
        {
          // Restart the attack animation for next repeat
          PlayAnimation("Attack");
        }
      }
      else
      {
        PlayAnimation("Attack");
      }
      return; // Don't process any other AI logic while attacking
    }

    // Calculate distance to player
    var toPlayer = _targetPlayer.Position - Position;
    var distance = toPlayer.Length;

    // Idle if player is too far
    if (distance > IdleDistance)
    {
      Velocity = Vector2.Zero;
      PlayAnimation("Idle");
      return;
    }

    // Attack if in range and cooldown ready
    if (distance <= AttackRange && _attackCooldown <= 0)
    {
      IsAttacking = true;
      _attackRepeatCount = 0;
      _projectileFiredThisAttack = false;
      Velocity = Vector2.Zero;
      PlayAnimation("Attack");
      
      // Face the player
      FacingDirection = toPlayer.X > 0 ? Direction.Right : Direction.Left;
      return;
    }

    // Move to maintain optimal distance
    if (distance > OptimalDistance + 20f)
    {
      // Too far - move towards player
      var direction = Vector2.Normalize(toPlayer);
      Velocity = new Vector2(direction.X * MoveSpeed, Velocity.Y);
      FacingDirection = direction.X > 0 ? Direction.Right : Direction.Left;
      PlayAnimation("Walk");
    }
    else if (distance < OptimalDistance - 20f)
    {
      // Too close - move away from player
      var direction = Vector2.Normalize(toPlayer);
      Velocity = new Vector2(-direction.X * MoveSpeed, Velocity.Y);
      FacingDirection = direction.X < 0 ? Direction.Right : Direction.Left;
      PlayAnimation("Walk");
    }
    else
    {
      // At optimal distance - stay idle
      Velocity = Vector2.Zero;
      PlayAnimation("Idle");
    }
  }

  private void FireProjectile()
  {
    if (_targetPlayer is null) return;

    // Calculate projectile spawn position at upper corner of enemy
    var enemyHalfWidth = (FixedCollisionWidth * scale) / 2f;
    var enemyHeight = FixedCollisionHeight * scale;
    
    // Spawn from left or right upper corner based on facing direction
    var spawnOffsetX = enemyHalfWidth * (int)FacingDirection;
    var spawnOffsetY = enemyHeight * 0.4f; // Upper portion of enemy
    var spawnPosition = Position + new Vector2(spawnOffsetX, spawnOffsetY);

    // Calculate direction toward player's current position
    var toPlayer = _targetPlayer.Position - spawnPosition;
    var direction = Vector2.Normalize(toPlayer);

    // Create and fire projectile
    var projectile = new Projectile(spawnPosition, direction, _contentRoot, FacingDirection, scale);
    OnProjectileFired?.Invoke(projectile);
  }

  private static SpriteRenderer CreateEnemyRenderer(string contentRoot)
  {
    var renderer = new SpriteRenderer();

    // Load animation sprite sheets from Animation/Enemy/ folder
    var idleSpritePath = AssetHelper.RequireAsset(
      AssetHelper.GetAnimationPath(contentRoot, System.IO.Path.Combine("Enemy", "Idle.png")),
      "Enemy Idle sprite sheet is missing.");
    var walkSpritePath = AssetHelper.RequireAsset(
      AssetHelper.GetAnimationPath(contentRoot, System.IO.Path.Combine("Enemy", "Walk.png")),
      "Enemy Walk sprite sheet is missing.");
    var attackSpritePath = AssetHelper.RequireAsset(
      AssetHelper.GetAnimationPath(contentRoot, System.IO.Path.Combine("Enemy", "Attack.png")),
      "Enemy Attack sprite sheet is missing.");
    var deadSpritePath = AssetHelper.RequireAsset(
      AssetHelper.GetAnimationPath(contentRoot, System.IO.Path.Combine("Enemy", "Dead.png")),
      "Enemy Dead sprite sheet is missing.");

    // Idle animation - 7 frames with irregular positions
    // Frame height = 90, character starts 38px from top of 128px sheet
    var idleFrameOrigins = new List<Vector2i>
    {
      new Vector2i(28, 0),   // Frame 1
      new Vector2i(156, 0),  // Frame 2
      new Vector2i(283, 0),  // Frame 3
      new Vector2i(412, 0),  // Frame 4
      new Vector2i(538, 0),  // Frame 5
      new Vector2i(668, 0),  // Frame 6
      new Vector2i(796, 0)   // Frame 7
    };

    // Walk animation - 12 frames with irregular positions
    var walkFrameOrigins = new List<Vector2i>
    {
      new Vector2i(30, 0),    // Frame 1
      new Vector2i(140, 0),   // Frame 2
      new Vector2i(275, 0),   // Frame 3
      new Vector2i(407, 0),   // Frame 4
      new Vector2i(537, 0),   // Frame 5
      new Vector2i(672, 0),   // Frame 6
      new Vector2i(778, 0),   // Frame 7
      new Vector2i(930, 0),   // Frame 8
      new Vector2i(1044, 0),  // Frame 9
      new Vector2i(1173, 0),  // Frame 10
      new Vector2i(1310, 0),  // Frame 11
      new Vector2i(1437, 0)   // Frame 12
    };

    // Attack animation - 8 frames with irregular positions
    var attackFrameOrigins = new List<Vector2i>
    {
      new Vector2i(33, 0),   // Frame 1
      new Vector2i(160, 0),  // Frame 2
      new Vector2i(288, 0),  // Frame 3
      new Vector2i(416, 0),  // Frame 4
      new Vector2i(542, 0),  // Frame 5
      new Vector2i(672, 0),  // Frame 6
      new Vector2i(801, 0),  // Frame 7
      new Vector2i(928, 0)   // Frame 8
    };

    // Dead animation - 4 frames with irregular positions (character at 40px from top, 128px height)
    var deadFrameOrigins = new List<Vector2i>
    {
      new Vector2i(38, 0),   // Frame 1
      new Vector2i(150, 0),  // Frame 2
      new Vector2i(259, 0),  // Frame 3
      new Vector2i(375, 0)   // Frame 4
    };

    // Load all animations with frame width, height, and origins
    renderer.LoadAnimation("Idle", idleSpritePath, frameWidth: 53, frameHeight: 90, idleFrameOrigins, frameDurationSeconds: 0.12, loop: true);
    renderer.LoadAnimation("Walk", walkSpritePath, frameWidth: 80, frameHeight: 90, walkFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    renderer.LoadAnimation("Attack", attackSpritePath, frameWidth: 67, frameHeight: 90, attackFrameOrigins, frameDurationSeconds: 0.1, loop: false);
    renderer.LoadAnimation("Dead", deadSpritePath, frameWidth: 94, frameHeight: 90, deadFrameOrigins, frameDurationSeconds: 0.15, loop: false);

    return renderer;
  }
}