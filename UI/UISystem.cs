using System;
using OpenTK.Mathematics;
using knight.Graphics;
using knight.Entities;

namespace knight.UI;

public sealed class UISystem : IDisposable
{
    private HealthBar? _playerHealthBar;
    private HealthBar? _enemyHealthBar;
    private Shader? _uiShader;
    private bool _disposed;

    private const string UIVertexShaderSource = @"
#version 330 core
layout(location = 0) in vec2 aPosition;

uniform mat4 uProjection;
uniform mat4 uView;
uniform mat4 uModel;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition.xy, 0.0, 1.0);
}";

    private const string UIFragmentShaderSource = @"
#version 330 core
out vec4 FragColor;

uniform vec3 uColor;

void main()
{
    FragColor = vec4(uColor, 1.0);
}";

    public void Initialize(Vector2i viewportSize)
    {
        _uiShader = new Shader(UIVertexShaderSource, UIFragmentShaderSource);
        
        // Player health bar - bottom left corner, white
        var playerBarWidth = 200f;
        var playerBarHeight = 20f;
        var playerBarX = 20f + playerBarWidth / 2f; // 20px padding from left
        var playerBarY = 20f + playerBarHeight / 2f; // 20px padding from bottom
        _playerHealthBar = new HealthBar(
            new Vector2(playerBarX, playerBarY),
            new Vector2(playerBarWidth, playerBarHeight),
            100,
            new Vector3(1.0f, 1.0f, 1.0f) // White
        );
        
        // Enemy health bar - top center, 80% width, red
        var enemyBarWidth = viewportSize.X * 0.8f;
        var enemyBarHeight = 30f;
        var enemyBarX = viewportSize.X / 2f;
        var enemyBarY = viewportSize.Y - 40f; // 40px from top
        _enemyHealthBar = new HealthBar(
            new Vector2(enemyBarX, enemyBarY),
            new Vector2(enemyBarWidth, enemyBarHeight),
            100,
            new Vector3(1.0f, 0.2f, 0.2f) // Red
        );
    }

    public void UpdateHealthBars(Player? player, Enemy? enemy)
    {
        if (_playerHealthBar != null && player != null)
        {
            _playerHealthBar.CurrentHealth = player.CurrentHealth;
            _playerHealthBar.MaxHealth = player.MaxHealth;
        }
        
        if (_enemyHealthBar != null && enemy != null)
        {
            _enemyHealthBar.CurrentHealth = enemy.CurrentHealth;
            _enemyHealthBar.MaxHealth = enemy.MaxHealth;
        }
    }

    public void Draw(Matrix4 projection)
    {
        if (_uiShader == null) return;
        
        // Draw UI with identity view matrix (no camera movement)
        var identityView = Matrix4.Identity;
        
        _playerHealthBar?.Draw(_uiShader, projection, identityView);
        _enemyHealthBar?.Draw(_uiShader, projection, identityView);
    }

    public void OnResize(Vector2i viewportSize)
    {
        // Update enemy health bar position to stay centered and 80% width
        if (_enemyHealthBar != null)
        {
            var enemyBarWidth = viewportSize.X * 0.8f;
            var enemyBarX = viewportSize.X / 2f;
            var enemyBarY = viewportSize.Y - 40f;
            _enemyHealthBar.Position = new Vector2(enemyBarX, enemyBarY);
            _enemyHealthBar.Size = new Vector2(enemyBarWidth, _enemyHealthBar.Size.Y);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _playerHealthBar?.Dispose();
        _enemyHealthBar?.Dispose();
        _uiShader?.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
