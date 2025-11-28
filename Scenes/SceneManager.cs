using System;
using knight.Core;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Scenes;

public sealed class SceneManager
{
  private IScene? _currentScene;
  private IScene? _nextScene;
  private readonly string _contentRoot;
  private Vector2i _viewportSize;

  public SceneManager(string contentRoot, Vector2i viewportSize)
  {
    _contentRoot = contentRoot ?? throw new ArgumentNullException(nameof(contentRoot));
    _viewportSize = viewportSize;
  }

  public IScene? CurrentScene => _currentScene;

  public void LoadScene(IScene scene)
  {
    if (scene is null)
    {
      throw new ArgumentNullException(nameof(scene));
    }

    _nextScene = scene;
  }

  public void Update(double deltaSeconds)
  {
    if (_nextScene is not null)
    {
      _currentScene?.Unload();
      _currentScene = _nextScene;
      _currentScene.Initialize(_contentRoot, _viewportSize);
      _nextScene = null;
    }

    _currentScene?.Update(deltaSeconds);
  }

  public void Draw(Shader shader, Camera camera)
  {
    _currentScene?.Draw(shader, camera);
  }

  public void OnResize(Vector2i newSize)
  {
    _viewportSize = newSize;
    _currentScene?.OnResize(newSize);
  }

  public void Unload()
  {
    _currentScene?.Unload();
    _currentScene = null;
    _nextScene = null;
  }
}
