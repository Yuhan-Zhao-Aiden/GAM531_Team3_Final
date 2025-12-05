using knight.Core;
using knight.Graphics;
using knight.Scenes;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace knight;

public sealed class Game : GameWindow
{
    private readonly string _contentRoot;

    private Shader? _shader;
    private Camera? _camera;
    private SceneManager? _sceneManager;
    private GameScene? _gameScene;

    private Matrix4 _projection;

    private const string VertexShaderSource = @"
#version 330 core
layout(location = 0) in vec2 aPosition;
layout(location = 1) in vec2 aTexCoord;

uniform mat4 uProjection;
uniform mat4 uView;
uniform mat4 uModel;

out vec2 vTexCoord;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(aPosition.xy, 0.0, 1.0);
    vTexCoord = aTexCoord;
}";

    private const string FragmentShaderSource = @"
#version 330 core
in vec2 vTexCoord;

out vec4 FragColor;

uniform sampler2D uTexture;
uniform vec3 uColorTint;

void main()
{
    vec4 texColor = texture(uTexture, vTexCoord);
    FragColor = vec4(texColor.rgb * uColorTint, texColor.a);
}";

    public Game(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, string? contentRoot = null)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _contentRoot = contentRoot ?? AppContext.BaseDirectory;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.ClearColor(0.1f, 0.12f, 0.16f, 1f);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);

        _shader = new Shader(VertexShaderSource, FragmentShaderSource);
        _shader.Use();
        _shader.SetInt("uTexture", 0);
        _shader.SetVector3("uColorTint", new Vector3(1.0f, 1.0f, 1.0f)); // Default white (no tint)

        UpdateProjection();
        _shader.SetMatrix4("uProjection", _projection);

        // Fixed camera at origin for non-moving view
        _camera = new Camera(new Vector2(0, 0), new Vector2(FramebufferSize.X, FramebufferSize.Y));

        _sceneManager = new SceneManager(_contentRoot, FramebufferSize);
        _gameScene = new GameScene();
        _sceneManager.LoadScene(_gameScene);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (_sceneManager is null || _camera is null)
        {
            return;
        }

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            _sceneManager.Unload();
            _gameScene = new GameScene();
            _sceneManager.LoadScene(_gameScene);
            return;
        }

        // Pass keyboard state to game scene
        if (_gameScene is not null)
        {
            _gameScene.SetKeyboardState(KeyboardState);
        }

        _sceneManager.Update(args.Time);

        // REMOVED: Camera follow code
        // The camera now stays fixed at its initial position
        // if (_gameScene?.Player is not null)
        // {
        //   _camera.FollowTarget(_gameScene.Player.Position, smoothing: 0.1f);
        // }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.Clear(ClearBufferMask.ColorBufferBit);

        if (_shader is null || _camera is null || _sceneManager is null)
        {
            SwapBuffers();
            return;
        }

        _sceneManager.Draw(_shader, _camera);
        
        // Draw UI on top (after game objects)
        if (_gameScene is not null)
        {
            _gameScene.DrawUI(_projection);
        }

        SwapBuffers();
    }

    protected override void OnFramebufferResize(FramebufferResizeEventArgs e)
    {
        base.OnFramebufferResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
        UpdateProjection();

        if (_shader is not null)
        {
            _shader.Use();
            _shader.SetMatrix4("uProjection", _projection);
        }

        if (_camera is not null)
        {
            _camera.ViewportSize = new Vector2(e.Width, e.Height);
            // Keep camera position at origin for fixed view
            _camera.Position = new Vector2(0, 0);
        }

        _sceneManager?.OnResize(new Vector2i(e.Width, e.Height));
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        // OnFramebufferResize handles all resize logic
    }

    protected override void OnUnload()
    {
        _sceneManager?.Unload();
        _shader?.Dispose();
        base.OnUnload();
    }

    private void UpdateProjection()
    {
        _projection = Matrix4.CreateOrthographicOffCenter(0, FramebufferSize.X, 0, FramebufferSize.Y, -1f, 1f);
    }
}