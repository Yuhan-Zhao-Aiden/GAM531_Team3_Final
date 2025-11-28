using knight;
using OpenTK.Windowing.Desktop;

var gameWindowSettings = GameWindowSettings.Default;
var nativeWindowSettings = new NativeWindowSettings
{
    Title = "GAM531 Team 3 Final",
    ClientSize = (1080, 720)
};

using var game = new Game(gameWindowSettings, nativeWindowSettings);
game.Run();
