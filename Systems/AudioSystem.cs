using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;

namespace knight.Systems;

public sealed class AudioSystem : IDisposable
{
  private readonly string _contentRoot;
  private IWavePlayer? _bgmPlayer;
  private AudioFileReader? _bgmReader;
  private readonly List<IWavePlayer> _activeSoundPlayers = new();
  private readonly Random _random = new();
  private bool _disposed;

  public float BgmVolume { get; set; } = 0.3f;
  public float SfxVolume { get; set; } = 0.5f;

  public AudioSystem(string contentRoot)
  {
    _contentRoot = contentRoot;
  }

  public void PlayBgm(string filename, bool loop = true)
  {
    try
    {
      StopBgm();

      string path = Path.Combine(_contentRoot, "Sound", filename);
      if (!File.Exists(path))
      {
        Console.WriteLine($"[AUDIO] BGM file not found: {path}");
        return;
      }

      _bgmReader = new AudioFileReader(path) { Volume = BgmVolume };

      if (loop)
      {
        var loopStream = new LoopStream(_bgmReader);
        _bgmPlayer = new WaveOutEvent();
        _bgmPlayer.Init(loopStream);
      }
      else
      {
        _bgmPlayer = new WaveOutEvent();
        _bgmPlayer.Init(_bgmReader);
      }

      _bgmPlayer.Play();
      Console.WriteLine($"[AUDIO] Playing BGM: {filename}");
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[AUDIO] Error playing BGM: {ex.Message}");
    }
  }

  public void StopBgm()
  {
    _bgmPlayer?.Stop();
    _bgmPlayer?.Dispose();
    _bgmPlayer = null;
    _bgmReader?.Dispose();
    _bgmReader = null;
  }

  public void PlaySound(string filename)
  {
    try
    {
      string path = Path.Combine(_contentRoot, "Sound", filename);
      if (!File.Exists(path))
      {
        Console.WriteLine($"[AUDIO] Sound file not found: {path}");
        return;
      }

      var reader = new AudioFileReader(path) { Volume = SfxVolume };
      var player = new WaveOutEvent();

      player.Init(reader);
      player.PlaybackStopped += (sender, args) =>
      {
        player.Dispose();
        reader.Dispose();
        _activeSoundPlayers.Remove(player);
      };

      _activeSoundPlayers.Add(player);
      player.Play();
    }
    catch (Exception ex)
    {
      Console.WriteLine($"[AUDIO] Error playing sound: {ex.Message}");
    }
  }

  public void PlayRandomWalkSound()
  {
    int randomIndex = _random.Next(1, 4); // 1, 2, or 3
    PlaySound($"walk{randomIndex}.mp3");
  }

  public void Update()
  {
    // Clean up finished sound players
    _activeSoundPlayers.RemoveAll(p => p.PlaybackState == PlaybackState.Stopped);
  }

  public void Dispose()
  {
    if (_disposed) return;

    StopBgm();

    // Create a copy to avoid collection modification during iteration
    var playersCopy = _activeSoundPlayers.ToList();
    foreach (var player in playersCopy)
    {
      player?.Stop();
      player?.Dispose();
    }
    _activeSoundPlayers.Clear();

    _disposed = true;
    GC.SuppressFinalize(this);
  }
}

// Helper class for looping audio
internal class LoopStream : WaveStream
{
  private readonly WaveStream _sourceStream;

  public LoopStream(WaveStream sourceStream)
  {
    _sourceStream = sourceStream;
  }

  public override WaveFormat WaveFormat => _sourceStream.WaveFormat;
  public override long Length => long.MaxValue;
  public override long Position
  {
    get => _sourceStream.Position;
    set => _sourceStream.Position = value;
  }

  public override int Read(byte[] buffer, int offset, int count)
  {
    int totalBytesRead = 0;

    while (totalBytesRead < count)
    {
      int bytesRead = _sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
      if (bytesRead == 0)
      {
        // Reached end of stream, loop back to beginning
        _sourceStream.Position = 0;
      }
      totalBytesRead += bytesRead;
    }

    return totalBytesRead;
  }
}
