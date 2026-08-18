#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>The OpenGL API a GL context exposes — desktop OpenGL, OpenGL ES, or WebGL.</summary>
public enum GLFlavor
{
	OpenGL,
	OpenGLES,
	WebGL,
}

/// <summary>
/// The device face of an OpenGL graphics context: the GL flavor and proc-address loader a backend needs to build
/// its GL rendering state. The per-frame framebuffer is a separate <see cref="IGLRenderTarget"/> concern; only
/// neutral types cross the seam.
/// </summary>
public interface IGLDeviceContext : IGraphicsContext
{
	/// <summary>The GL flavor this context exposes (desktop GL / GLES / WebGL).</summary>
	GLFlavor Flavor { get; }

	/// <summary>Host GL proc-address loader so any backend can resolve GL entry points itself.</summary>
	Func<string, nint> GetProcAddress { get; }
}
