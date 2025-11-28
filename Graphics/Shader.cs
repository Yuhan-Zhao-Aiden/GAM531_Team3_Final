using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace knight.Graphics;

public sealed class Shader : IDisposable
{
    public int Handle { get; }

    private bool _disposed;

    public Shader(string vertexSource, string fragmentSource)
    {
        if (string.IsNullOrWhiteSpace(vertexSource))
        {
            throw new ArgumentException("Vertex shader source cannot be empty.", nameof(vertexSource));
        }

        if (string.IsNullOrWhiteSpace(fragmentSource))
        {
            throw new ArgumentException("Fragment shader source cannot be empty.", nameof(fragmentSource));
        }

        var vertexShader = CompileShader(vertexSource, ShaderType.VertexShader);
        var fragmentShader = CompileShader(fragmentSource, ShaderType.FragmentShader);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var linkStatus);
        if (linkStatus == 0)
        {
            var info = GL.GetProgramInfoLog(Handle);
            GL.DeleteProgram(Handle);
            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
            throw new InvalidOperationException($"Shader program link failed: {info}");
        }

        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }

    public void Use() => GL.UseProgram(Handle);

    public void SetMatrix4(string uniformName, Matrix4 value)
    {
        var location = GL.GetUniformLocation(Handle, uniformName);
        if (location != -1)
        {
            GL.UniformMatrix4(location, false, ref value);
        }
    }

    public void SetInt(string uniformName, int value)
    {
        var location = GL.GetUniformLocation(Handle, uniformName);
        if (location != -1)
        {
            GL.Uniform1(location, value);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (Handle != 0)
        {
            GL.DeleteProgram(Handle);
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static int CompileShader(string source, ShaderType type)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out var compileStatus);
        if (compileStatus == 0)
        {
            var info = GL.GetShaderInfoLog(shader);
            GL.DeleteShader(shader);
            throw new InvalidOperationException($"{type} compile failed: {info}");
        }

        return shader;
    }
}
