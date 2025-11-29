using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace knight.Graphics;

public sealed class SpriteRenderer : IDisposable
{
  private static readonly Vector2[] CornerPos =
  {
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(-0.5f, 0.5f)
    };

  private readonly Dictionary<string, SpriteSheet> _animations = new(StringComparer.OrdinalIgnoreCase);
  private readonly float[] _vertexBuffer = new float[16]; // 4 vertices, pos + uv
  private readonly uint[] _indices = { 0, 1, 2, 2, 3, 0 };

  private int vao, vbo, ebo;

  private string? _activeAnimation;
  private int _activeFrameIndex;
  private double _frameTimer;
  private bool _frameDirty = true;
  private bool _disposed;

  public SpriteRenderer()
  {
    InitializeQuadBuffers();
  }

  public Vector2 CurrentFrameSize => _activeAnimation is null
      ? Vector2.One
      : _animations[_activeAnimation].FrameSize;

  public bool HasActiveAnimation => _activeAnimation is not null;

  public bool IsAnimationFinished()
  {
    if (_activeAnimation is null) return true;
    var sheet = _animations[_activeAnimation];
    return !sheet.Loops && _activeFrameIndex >= sheet.TotalFrames - 1;
  }


  public void LoadAnimation(string animationName, string filePath, int frameWidth, int frameHeight, double frameDurationSeconds, bool loop = true)
      => LoadAnimationInternal(animationName, filePath, frameWidth, frameHeight, frameDurationSeconds, loop, frameOrigins: null);


  public void LoadAnimation(string animationName, string filePath, int frameWidth, int frameHeight, IReadOnlyList<Vector2i> frameOrigins, double frameDurationSeconds, bool loop = true)
  {
    if (frameOrigins is null || frameOrigins.Count == 0)
    {
      throw new ArgumentException("At least one frame origin must be provided.", nameof(frameOrigins));
    }

    LoadAnimationInternal(animationName, filePath, frameWidth, frameHeight, frameDurationSeconds, loop, frameOrigins);
  }

  public void SetAnimation(string animationName, bool restart = false)
  {
    if (!_animations.TryGetValue(animationName, out _))
    {
      throw new KeyNotFoundException($"Animation '{animationName}' has not been loaded.");
    }

    if (_activeAnimation == animationName && !restart)
    {
      return;
    }

    _activeAnimation = animationName;
    _activeFrameIndex = 0;
    _frameTimer = 0;
    _frameDirty = true;
  }

  public void Update(double deltaSeconds)
  {
    if (_activeAnimation is null)
    {
      return;
    }

    var sheet = _animations[_activeAnimation];
    _frameTimer += deltaSeconds;

    while (_frameTimer >= sheet.FrameDurationSeconds && sheet.FrameDurationSeconds > 0)
    {
      _frameTimer -= sheet.FrameDurationSeconds;
      AdvanceFrame(sheet);
    }
  }

  public void Bind()
  {
    if (_activeAnimation is null)
    {
      return;
    }

    EnsureVertexBuffer();

    var sheet = _animations[_activeAnimation];
    GL.ActiveTexture(TextureUnit.Texture0);
    GL.BindTexture(TextureTarget.Texture2D, sheet.TextureHandle);

    GL.BindVertexArray(vao);
  }

  public void Unbind()
  {
    GL.BindVertexArray(0);
    GL.BindTexture(TextureTarget.Texture2D, 0);
  }

  public void Draw()
  {
    if (_activeAnimation is null)
    {
      return;
    }

    Bind();
    GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);
    Unbind();
  }

  public void Dispose()
  {
    if (_disposed) return;

    foreach (var sheet in _animations.Values)
    {
      sheet.Dispose();
    }

    GL.DeleteBuffer(vbo);
    GL.DeleteBuffer(ebo);
    GL.DeleteVertexArray(vao);
    _disposed = true;
  }

  private void InitializeQuadBuffers()
  {
    vao = GL.GenVertexArray();
    vbo = GL.GenBuffer();
    ebo = GL.GenBuffer();

    GL.BindVertexArray(vao);

    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
    GL.BufferData(BufferTarget.ArrayBuffer, sizeof(float) * _vertexBuffer.Length, IntPtr.Zero, BufferUsageHint.DynamicDraw);

    GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
    GL.EnableVertexAttribArray(0);

    GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
    GL.EnableVertexAttribArray(1);

    GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
    GL.BufferData(BufferTarget.ElementArrayBuffer, sizeof(uint) * _indices.Length, _indices, BufferUsageHint.StaticDraw);

    GL.BindVertexArray(0);
  }

  private void EnsureVertexBuffer()
  {
    if (!_frameDirty || _activeAnimation is null)
    {
      return;
    }

    var sheet = _animations[_activeAnimation];
    Span<Vector2> uv = stackalloc Vector2[4];
    sheet.FillFrameUv(_activeFrameIndex, uv);

    for (var i = 0; i < 4; i++)
    {
      var position = CornerPos[i] * sheet.FrameSize;
      _vertexBuffer[i * 4 + 0] = position.X;
      _vertexBuffer[i * 4 + 1] = position.Y;
      _vertexBuffer[i * 4 + 2] = uv[i].X;
      _vertexBuffer[i * 4 + 3] = uv[i].Y;
    }

    GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
    GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, sizeof(float) * _vertexBuffer.Length, _vertexBuffer);

    _frameDirty = false;
  }

  private void AdvanceFrame(SpriteSheet sheet)
  {
    var nextFrame = _activeFrameIndex + 1;
    if (nextFrame >= sheet.TotalFrames)
    {
      _activeFrameIndex = sheet.Loops ? 0 : sheet.TotalFrames - 1;
    }
    else
    {
      _activeFrameIndex = nextFrame;
    }

    _frameDirty = true;
  }

  private void LoadAnimationInternal(string animationName, string filePath, int frameWidth, int frameHeight, double frameDurationSeconds, bool loop, IReadOnlyList<Vector2i>? frameOrigins)
  {
    if (string.IsNullOrWhiteSpace(animationName))
    {
      throw new ArgumentException("Animation name must contain characters.", nameof(animationName));
    }

    if (!File.Exists(filePath))
    {
      throw new FileNotFoundException("Could not find sprite sheet file.", filePath);
    }

    if (frameWidth <= 0 || frameHeight <= 0)
    {
      throw new ArgumentOutOfRangeException(nameof(frameWidth), "Frame dimensions must be positive.");
    }

    using var image = Image.Load<Rgba32>(filePath);
    if (frameOrigins is null)
    {
      if (image.Width % frameWidth != 0 || image.Height % frameHeight != 0)
      {
        throw new InvalidOperationException($"Sprite sheet {filePath} dimensions must divide cleanly by the frame size.");
      }
    }
    else
    {
      foreach (var origin in frameOrigins)
      {
        if (origin.X < 0 || origin.Y < 0 || origin.X + frameWidth > image.Width || origin.Y + frameHeight > image.Height)
        {
          throw new InvalidOperationException($"Frame origin {origin} with size ({frameWidth},{frameHeight}) exceeds the bounds of {filePath}.");
        }
      }
    }

    var textureHandle = GL.GenTexture();
    GL.BindTexture(TextureTarget.Texture2D, textureHandle);

    GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

    var pixelSpan = new byte[image.Width * image.Height * 4];
    image.CopyPixelDataTo(pixelSpan);
    GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixelSpan);

    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
    GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

    GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
    GL.BindTexture(TextureTarget.Texture2D, 0);

    var sheet = new SpriteSheet(animationName, textureHandle, image.Width, image.Height, frameWidth, frameHeight, frameDurationSeconds, loop, frameOrigins);
    _animations[animationName] = sheet;

    // Set the first loaded animation as active by default.
    if (_activeAnimation is null)
    {
      SetAnimation(animationName, true);
    }
  }

  private sealed class SpriteSheet : IDisposable
  {
    private readonly Vector2[] _uvMins;
    private readonly Vector2[] _uvMaxs;

    public SpriteSheet(string name, int textureHandle, int textureWidth, int textureHeight, int frameWidth, int frameHeight, double frameDurationSeconds, bool loops, IReadOnlyList<Vector2i>? frameOrigins)
    {
      Name = name;
      TextureHandle = textureHandle;
      TextureWidth = textureWidth;
      TextureHeight = textureHeight;
      FrameWidth = frameWidth;
      FrameHeight = frameHeight;
      FrameDurationSeconds = frameDurationSeconds;
      Loops = loops;

      if (frameOrigins is null)
      {
        FramesPerRow = textureWidth / frameWidth;
        FramesPerColumn = textureHeight / frameHeight;
        TotalFrames = FramesPerRow * FramesPerColumn;
        _uvMins = new Vector2[TotalFrames];
        _uvMaxs = new Vector2[TotalFrames];

        var index = 0;
        for (var row = 0; row < FramesPerColumn; row++)
        {
          var v0 = 1f - (float)(row + 1) * FrameHeight / TextureHeight;
          var v1 = 1f - (float)row * FrameHeight / TextureHeight;

          for (var column = 0; column < FramesPerRow; column++, index++)
          {
            var u0 = (float)column * FrameWidth / TextureWidth;
            var u1 = (float)(column + 1) * FrameWidth / TextureWidth;

            _uvMins[index] = new Vector2(u0, v0);
            _uvMaxs[index] = new Vector2(u1, v1);
          }
        }
      }
      else
      {
        FramesPerRow = 0;
        FramesPerColumn = 0;
        TotalFrames = frameOrigins.Count;
        _uvMins = new Vector2[TotalFrames];
        _uvMaxs = new Vector2[TotalFrames];

        for (var i = 0; i < TotalFrames; i++)
        {
          var origin = frameOrigins[i];
          var u0 = (float)origin.X / TextureWidth;
          var u1 = (float)(origin.X + FrameWidth) / TextureWidth;

          var top = (float)origin.Y / TextureHeight;
          var bottom = (float)(origin.Y + FrameHeight) / TextureHeight;

          var v1 = 1f - top;
          var v0 = 1f - bottom;

          _uvMins[i] = new Vector2(u0, v0);
          _uvMaxs[i] = new Vector2(u1, v1);
        }
      }

      FrameSize = new Vector2(FrameWidth, FrameHeight);
    }

    public string Name { get; }
    public int TextureHandle { get; }
    public int TextureWidth { get; }
    public int TextureHeight { get; }
    public int FrameWidth { get; }
    public int FrameHeight { get; }
    public int FramesPerRow { get; }
    public int FramesPerColumn { get; }
    public int TotalFrames { get; }
    public double FrameDurationSeconds { get; }
    public bool Loops { get; }
    public Vector2 FrameSize { get; }

    public void FillFrameUv(int frameIndex, Span<Vector2> destination)
    {
      if ((uint)frameIndex >= (uint)TotalFrames)
      {
        throw new ArgumentOutOfRangeException(nameof(frameIndex), $"Frame index {frameIndex} exceeds total frames {TotalFrames} for '{Name}'.");
      }

      if (destination.Length < 4)
      {
        throw new ArgumentException("Destination span must have room for four UV coordinates.", nameof(destination));
      }

      var uvMin = _uvMins[frameIndex];
      var uvMax = _uvMaxs[frameIndex];

      destination[0] = new Vector2(uvMin.X, uvMax.Y); 
      destination[1] = new Vector2(uvMax.X, uvMax.Y); 
      destination[2] = new Vector2(uvMax.X, uvMin.Y);
      destination[3] = new Vector2(uvMin.X, uvMin.Y);
    }

    public void Dispose()
    {
      if (TextureHandle != 0)
      {
        GL.DeleteTexture(TextureHandle);
      }
    }
  }
}
