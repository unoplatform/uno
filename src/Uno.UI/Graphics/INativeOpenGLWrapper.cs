using System;
using Microsoft.UI.Xaml;

#if WINAPPSDK || WINDOWS_UWP
namespace Uno.WinUI.Graphics3DGL;
#else
namespace Uno.Graphics;
#endif

internal interface INativeOpenGLWrapper
{
	public delegate IntPtr GLGetProcAddress(string proc);

	/// <summary>
	/// Creates an OpenGL context for a native window/surface that the
	/// <param name="element"></param> belongs to. The <see cref="INativeOpenGLWrapper"/>
	/// will be associated with this element until a corresponding call to <see cref="DestroyContext"/>.
	/// </summary>
	public void CreateContext(UIElement element);

	/// <remarks>
	/// This should be cast to a Silk.NET.OpenGL.GL (even on devices with GLES, not Desktop GL).
	/// The Silk.NET.OpenGL.GL API surface is a almost a complete superset of the Silk.NET.OpenGLES.GL API
	/// surface (with the exception of BlendBarrier and PrimitiveBoundingBox which are OpenGL ES 3.2-only APIs).
	/// So, we always expose an OpenGL.GL object even on GLES devices, with the caveat that the user is only
	/// allowed to make calls that are available on this GL (or GLES) version (just like OpenGL 4-only APIs
	/// aren't allowed on OpenGL 3 implementations).
	/// </remarks>
	public object CreateGLSilkNETHandle();

	/// <summary>
	/// Destroys the context created in <see cref="CreateContext"/>. This is only called if a preceding
	/// call to <see cref="CreateContext"/> is made (after the last call to <see cref="DestroyContext"/>).
	/// </summary>
	public void DestroyContext();

	/// <summary>
	/// Makes the OpenGL context created in <see cref="CreateContext"/> the current context for the thread.
	/// </summary>
	/// <returns>A disposable that restores the OpenGL context to what it was at the time of this method call.</returns>
	public IDisposable MakeCurrent();
}
