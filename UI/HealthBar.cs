using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using knight.Graphics;

namespace knight.UI;

public sealed class HealthBar : IDisposable
{
    private int _vao;
    private int _vbo;
    private bool _disposed;

    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector3 BackgroundColor { get; set; } = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 ForegroundColor { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);
    public int MaxHealth { get; set; }
    public int CurrentHealth { get; set; }

    public HealthBar(Vector2 position, Vector2 size, int maxHealth, Vector3 foregroundColor)
    {
        Position = position;
        Size = size;
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        ForegroundColor = foregroundColor;
        
        InitializeBuffers();
    }

    private void InitializeBuffers()
    {
        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        
        // Position attribute
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void Draw(Shader shader, Matrix4 projection, Matrix4 view)
    {
        shader.Use();
        shader.SetMatrix4("uProjection", projection);
        shader.SetMatrix4("uView", view);
        
        // Draw background (dark gray)
        DrawRectangle(shader, Position, Size, BackgroundColor);
        
        // Draw foreground (health bar with color)
        var healthPercentage = Math.Clamp((float)CurrentHealth / MaxHealth, 0f, 1f);
        var foregroundSize = new Vector2(Size.X * healthPercentage, Size.Y);
        DrawRectangle(shader, Position, foregroundSize, ForegroundColor);
    }

    private void DrawRectangle(Shader shader, Vector2 position, Vector2 size, Vector3 color)
    {
        if (size.X <= 0 || size.Y <= 0) return;

        var halfSize = size * 0.5f;
        var left = position.X - halfSize.X;
        var right = position.X + halfSize.X;
        var bottom = position.Y - halfSize.Y;
        var top = position.Y + halfSize.Y;

        float[] vertices = {
            left, bottom,   // Bottom-left
            right, bottom,  // Bottom-right
            right, top,     // Top-right
            left, top       // Top-left
        };

        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.DynamicDraw);

        var model = Matrix4.Identity;
        shader.SetMatrix4("uModel", model);
        
        // Set color uniform
        var colorLocation = GL.GetUniformLocation(shader.Handle, "uColor");
        if (colorLocation != -1)
        {
            GL.Uniform3(colorLocation, color);
        }

        GL.DrawArrays(PrimitiveType.TriangleFan, 0, 4);
        
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        GL.BindVertexArray(0);
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_vao != 0)
        {
            GL.DeleteVertexArray(_vao);
            _vao = 0;
        }

        if (_vbo != 0)
        {
            GL.DeleteBuffer(_vbo);
            _vbo = 0;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
