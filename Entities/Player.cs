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

    // Load all animations
    renderer.LoadAnimation("Idle", idleSpritePath, frameWidth: 25, frameHeight: 40, idleFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    renderer.LoadAnimation("Jump", jumpSpritePath, frameWidth: 30, frameHeight: 40, jumpFrameOrigins, frameDurationSeconds: 0.1, loop: false);
    renderer.LoadAnimation("Fall", fallSpritePath, frameWidth: 30, frameHeight: 40, fallFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    renderer.LoadAnimation("Run", runSpritePath, frameWidth: 25, frameHeight: 40, runFrameOrigins, frameDurationSeconds: 0.1, loop: true);
    renderer.LoadAnimation("Attack", attackSpritePath, frameWidth: 66, frameHeight: 80, attackFrameOrigins, frameDurationSeconds: 0.08, loop: false);
    renderer.LoadAnimation("Attack2", attack2SpritePath, frameWidth: 78, frameHeight: 80, attack2FrameOrigins, frameDurationSeconds: 0.08, loop: false);

    return renderer;
  }
}
