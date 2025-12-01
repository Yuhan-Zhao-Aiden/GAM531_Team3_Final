using System;
using System.Collections.Generic;
using knight.Core;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Entities;

public sealed class Player : AnimatedEntity
{
  private const float FixedCollisionHeight = 40f;
  private const float FixedCollisionWidth = 25f;


  public Player(Vector2 position, string contentRoot, float scale = 2f)
      : base(position, CreatePlayerRenderer(contentRoot))
  {
    setScale(scale);
    PlayAnimation("Idle");
  }

  public bool IsGrounded { get; set; }
  public bool IsAttacking { get; set; }
  public int AttackComboCount { get; set; }
  public bool Attack2Ready { get; set; }
  public bool IsRolling { get; set; }
  public bool IsDead { get; private set; }
  
  // Health system
  public int MaxHealth { get; } = 100;
  public int CurrentHealth { get; private set; } = 100;
  public bool IsInvulnerable => IsRolling; // Player is invulnerable during roll
  
  // Attack cooldown to prevent multi-hit during single attack
  private double _attackDamageCooldown = 0.0;
  private const double AttackDamageCooldownDuration = 0.3; // 300ms cooldown between damage applications

  public void TakeDamage(int damage)
  {
    if (IsInvulnerable || IsDead || CurrentHealth <= 0) return;
    
    CurrentHealth = Math.Max(0, CurrentHealth - damage);
    Console.WriteLine($"[PLAYER] Health: {CurrentHealth}/{MaxHealth}");
    
    if (CurrentHealth <= 0)
    {
      IsDead = true;
      IsAttacking = false;
      IsRolling = false;
      PlayAnimation("Death");
      Console.WriteLine("[PLAYER] DEFEATED!");
    }
  }

  // Check if attack can currently deal damage (prevents multi-hit during single attack)
  public bool CanDealDamage => !IsDead && IsAttacking && _attackDamageCooldown <= 0.0;
  
  // Reset attack damage cooldown when new attack starts
  public void ResetAttackCooldown()
  {
    _attackDamageCooldown = AttackDamageCooldownDuration;
  }
  
  // Update cooldown timers (call this in Update)
  public void UpdateCooldowns(double deltaSeconds)
  {
    if (_attackDamageCooldown > 0.0)
    {
      _attackDamageCooldown -= deltaSeconds;
    }
  }
  
  // Get attack hitbox for dealing damage to enemies
  // Attack sprite is 66px wide, front half (33px) is the sword
  public (Vector2 position, Vector2 size) GetAttackHitbox()
  {
    if (IsDead || !IsAttacking) return (Vector2.Zero, Vector2.Zero);

    // Attack hitbox is in front of player
    var hitboxWidth = 33f * scale; // Front half of attack sprite (sword)
    var hitboxHeight = FixedCollisionHeight * scale;
    var hitboxOffsetX = (FixedCollisionWidth * scale / 2f + hitboxWidth / 2f) * (int)FacingDirection;
    
    var hitboxPosition = Position + new Vector2(hitboxOffsetX, 0);
    var hitboxSize = new Vector2(hitboxWidth, hitboxHeight);
    
    return (hitboxPosition, hitboxSize);
  }

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

  private static SpriteRenderer CreatePlayerRenderer(string contentRoot)
  {
    var renderer = new SpriteRenderer();

    // Load animation sprite sheets
    var idleSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAnimationPath(contentRoot, "_Idle.png"), "Idle sprite sheet is missing.");
    var jumpSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAnimationPath(contentRoot, "_Jump.png"), "Jump sprite sheet is missing.");
    var fallSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAnimationPath(contentRoot, "_Fall.png"), "Fall sprite sheet is missing.");
    var runSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAnimationPath(contentRoot, "_Run.png"), "Run sprite sheet is missing.");
    var attackSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAnimationPath(contentRoot, "_Attack.png"), "Attack sprite sheet is missing.");
    var attack2SpritePath = AssetHelper.RequireAsset(AssetHelper.GetAnimationPath(contentRoot, "_Attack2.png"), "Attack2 sprite sheet is missing.");
    var rollSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAnimationPath(contentRoot, "_Roll.png"), "Roll sprite sheet is missing.");
    var deathSpritePath = AssetHelper.RequireAsset(AssetHelper.GetAnimationPath(contentRoot, "_Death.png"), "Death sprite sheet is missing.");

    // Build frame origins for regular animations (10 frames, evenly spaced)
    var idleFrameOrigins = new List<Vector2i>(10);
    var runFrameOrigins = new List<Vector2i>(10);
    for (var i = 0; i < 10; i++)
    {
      idleFrameOrigins.Add(new Vector2i(40 + i * 120, 0));
      runFrameOrigins.Add(new Vector2i(40 + i * 120, 0));
    }

    // Build frame origins for jump/fall animations (3 frames, evenly spaced)
    var jumpFrameOrigins = new List<Vector2i>(3);
    var fallFrameOrigins = new List<Vector2i>(3);
    for (var i = 0; i < 3; i++)
    {
      var x = 40 + i * 120;
      jumpFrameOrigins.Add(new Vector2i(x, 0));
      fallFrameOrigins.Add(new Vector2i(x, 0));
    }

    // Attack animation - irregular frame positions
    var attackFrameOrigins = new List<Vector2i>
    {
      new Vector2i(35, 0),   // Frame 1
      new Vector2i(175, 0),  // Frame 2
      new Vector2i(292, 0),  // Frame 3
      new Vector2i(412, 0)   // Frame 4
    };

    // Attack2 animation - irregular frame positions
    var attack2FrameOrigins = new List<Vector2i>
    {
      new Vector2i(40, 0),   // Frame 1
      new Vector2i(157, 0),  // Frame 2
      new Vector2i(272, 0),  // Frame 3
      new Vector2i(390, 0),  // Frame 4
      new Vector2i(510, 0),  // Frame 5
      new Vector2i(630, 0)   // Frame 6
    };

    // Roll animation - irregular frame positions (character starts at y=0, extends 40px down)
    var rollFrameOrigins = new List<Vector2i>
    {
      new Vector2i(47, 0),    // Frame 1
      new Vector2i(161, 0),   // Frame 2
      new Vector2i(282, 0),   // Frame 3
      new Vector2i(400, 0),   // Frame 4
      new Vector2i(520, 0),   // Frame 5
      new Vector2i(640, 0),   // Frame 6
      new Vector2i(765, 0),   // Frame 7
      new Vector2i(892, 0),   // Frame 8
      new Vector2i(1013, 0),  // Frame 9
      new Vector2i(1127, 0),  // Frame 10
      new Vector2i(1248, 0),  // Frame 11
      new Vector2i(1364, 0)   // Frame 12
    };

    // Death animation - 10 frames with irregular positions (character at 40px from top, 80px height)
    var deathFrameOrigins = new List<Vector2i>
    {
      new Vector2i(35, 0),    // Frame 1
      new Vector2i(155, 0),   // Frame 2
      new Vector2i(276, 0),   // Frame 3
      new Vector2i(392, 0),   // Frame 4
      new Vector2i(508, 0),   // Frame 5
      new Vector2i(628, 0),   // Frame 6
      new Vector2i(749, 0),   // Frame 7
      new Vector2i(869, 0),   // Frame 8
      new Vector2i(989, 0),   // Frame 9
      new Vector2i(1108, 0)   // Frame 10
    };

    // Load all animations
    renderer.LoadAnimation("Idle", idleSpritePath, frameWidth: 25, frameHeight: 40, idleFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    renderer.LoadAnimation("Jump", jumpSpritePath, frameWidth: 30, frameHeight: 40, jumpFrameOrigins, frameDurationSeconds: 0.1, loop: false);
    renderer.LoadAnimation("Fall", fallSpritePath, frameWidth: 30, frameHeight: 40, fallFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    renderer.LoadAnimation("Run", runSpritePath, frameWidth: 25, frameHeight: 40, runFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    renderer.LoadAnimation("Attack", attackSpritePath, frameWidth: 66, frameHeight: 80, attackFrameOrigins, frameDurationSeconds: 0.08, loop: false);
    renderer.LoadAnimation("Attack2", attack2SpritePath, frameWidth: 78, frameHeight: 80, attack2FrameOrigins, frameDurationSeconds: 0.08, loop: false);
    renderer.LoadAnimation("Roll", rollSpritePath, frameWidth: 45, frameHeight: 40, rollFrameOrigins, frameDurationSeconds: 0.06, loop: false);
    renderer.LoadAnimation("Death", deathSpritePath, frameWidth: 66, frameHeight: 80, deathFrameOrigins, frameDurationSeconds: 0.1, loop: false);

    return renderer;
  }
}
