#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>The OpenGL API a GL context exposes — desktop OpenGL, OpenGL ES, or WebGL. A backend resolves its
/// entry points via <see cref="IGLDeviceContext.GetProcAddress"/> for whichever flavor this is.</summary>
public enum GLFlavor
{
	OpenGL,
	OpenGLES,
	WebGL,
}

/// <summary>
/// The device face of an OpenGL graphics context: the stable, per-context device details a backend needs to
/// build its GL rendering state — the GL flavor and the proc-address loader. A backend reads these from the
/// context at <see cref="IGraphicsProvider{TContext}.CreateGraphics"/> (the context <em>is</em> the device); the
/// per-frame color surface (framebuffer id + size) is a separate <see cref="IGLRenderTarget"/> concern. Neutral:
/// only an <c>enum</c> and a plain <c>Func</c> cross the seam, never a GPU-library type.
/// </summary>
public interface IGLDeviceContext : IGraphicsContext
{
	/// <summary>The GL flavor this context exposes (desktop GL / GLES / WebGL) — picks how a backend assembles its interface.</summary>
	GLFlavor Flavor { get; }

	/// <summary>
	/// GL proc-address loader (the host's <c>glXGetProcAddress</c> / <c>wglGetProcAddress</c>+<c>opengl32</c> /
	/// <c>eglGetProcAddress</c> / WebGL equivalent). Present on every GL context so any backend can resolve GL
	/// entry points itself — never relying on a GPU library's built-in resolver.
	/// </summary>
	Func<string, nint> GetProcAddress { get; }
}
