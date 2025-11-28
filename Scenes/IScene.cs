using knight.Core;
using knight.Graphics;
using OpenTK.Mathematics;

namespace knight.Scenes;

public interface IScene
{
  void Initialize(string contentRoot, Vector2i viewportSize);
  void Update(double deltaSeconds);
  void Draw(Shader shader, Camera camera);
  void OnResize(Vector2i newSize);
  void Unload();
}
