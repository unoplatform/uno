#nullable enable

using System;

namespace Uno.UI.Composition.Drawing;

/// <summary>The OpenGL API a <see cref="IGLRenderTarget"/> exposes — desktop OpenGL, OpenGL ES, or WebGL. A backend
/// resolves its entry points via <see cref="IGLRenderTarget.GetProcAddress"/> for whichever flavor this is.</summary>
public enum GLFlavor
{
	OpenGL,
	OpenGLES,
	WebGL,
}

/// <summary>
/// An OpenGL color target: the framebuffer the backend renders into (the host swapchain's default framebuffer),
/// plus the sample/stencil counts its visual was created with. The host makes its GL context <em>current</em>
/// before handing this over; the backend resolves the GL entry points it needs through
/// <see cref="GetProcAddress"/> (never assuming it can find them on its own), then builds its rendering state
/// against the current context. The swap/present is the <see cref="IGraphicsContext"/>'s job. Mirrors
/// <see cref="ISoftwareRenderTarget"/> for the CPU-framebuffer case — the host stays free of any GPU-library type.
/// </summary>
public interface IGLRenderTarget : IRenderTarget
{
	uint FramebufferId { get; }

	int SampleCount { get; }

	int StencilBits { get; }

	/// <summary>The GL flavor this target exposes (desktop GL / GLES / WebGL) — picks how a backend assembles its interface.</summary>
	GLFlavor Flavor { get; }

	/// <summary>
	/// GL proc-address loader (the host's <c>glXGetProcAddress</c> / <c>wglGetProcAddress</c>+<c>opengl32</c> /
	/// <c>eglGetProcAddress</c> / WebGL equivalent). Required on every target so any backend can resolve GL entry
	/// points itself — it must never rely on a GPU library's built-in resolver. Neutral: a plain <c>Func</c>, no
	/// GPU-library type crosses the seam.
	/// </summary>
	Func<string, nint> GetProcAddress { get; }
}
