using System;
using System.IO;
using OpenTK.Mathematics;
using SixLabors.ImageSharp;
using ImageSharpImage = SixLabors.ImageSharp.Image;

namespace knight.Core;

/// <summary>
/// Utility class for loading and validating game assets.
/// </summary>
public static class AssetHelper
{
  /// <summary>
  /// Validates that an asset file exists and returns its path.
  /// </summary>
  /// <param name="path">The path to the asset file.</param>
  /// <param name="errorMessage">Error message if file doesn't exist.</param>
  /// <returns>The validated file path.</returns>
  /// <exception cref="FileNotFoundException">Thrown when the file doesn't exist.</exception>
  public static string RequireAsset(string path, string errorMessage)
  {
    if (File.Exists(path))
    {
      return path;
    }

    throw new FileNotFoundException(errorMessage, path);
  }

  /// <summary>
  /// Gets the dimensions of an image file.
  /// </summary>
  /// <param name="filePath">Path to the image file.</param>
  /// <returns>The width and height of the image.</returns>
  /// <exception cref="InvalidOperationException">Thrown when unable to identify image dimensions.</exception>
  public static Vector2i GetTextureSize(string filePath)
  {
    var info = ImageSharpImage.Identify(filePath);
    if (info is null)
    {
      throw new InvalidOperationException($"Unable to identify texture dimensions for '{filePath}'.");
    }

    return new Vector2i(info.Width, info.Height);
  }

  /// <summary>
  /// Builds the full path to an animation sprite sheet.
  /// </summary>
  /// <param name="contentRoot">The root content directory.</param>
  /// <param name="fileName">The animation file name (e.g., "_Idle.png").</param>
  /// <returns>Full path to the animation file.</returns>
  public static string GetAnimationPath(string contentRoot, string fileName)
  {
    return Path.Combine(contentRoot, "Animation", fileName);
  }

  /// <summary>
  /// Builds the full path to an asset file.
  /// </summary>
  /// <param name="contentRoot">The root content directory.</param>
  /// <param name="fileName">The asset file name.</param>
  /// <returns>Full path to the asset file.</returns>
  public static string GetAssetPath(string contentRoot, string fileName)
  {
    return Path.Combine(contentRoot, "Assets", fileName);
  }
}
