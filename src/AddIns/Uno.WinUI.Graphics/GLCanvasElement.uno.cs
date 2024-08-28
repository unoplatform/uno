#if !WINAPPSDK

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Silk.NET.Core.Contexts;
using Silk.NET.OpenGL;
using Uno.Foundation.Extensibility;

namespace Uno.WinUI.Graphics;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw 3D graphics using OpenGL and Silk.NET.
/// </summary>
/// <remarks>
/// This is only available on skia-based targets and when running with hardware acceleration.
/// This is currently only available on the WPF and X11 targets.
/// </remarks>
public abstract partial class GLCanvasElement
{
	private readonly GLVisual _glVisual;

	private bool _renderDirty = true;

	/// <param name="width">The width of the backing framebuffer.</param>
	/// <param name="height">The height of the backing framebuffer.</param>
	protected GLCanvasElement(uint width, uint height)
	{
		_width = width;
		_height = height;

		_glVisual = new GLVisual(this, Visual.Compositor);
		Visual.Children.InsertAtTop(_glVisual);
	}

	public partial void Invalidate()
	{
		_renderDirty = true;
		_glVisual.Compositor.InvalidateRender(_glVisual);
	}

	private protected override unsafe void OnLoaded()
	{
		base.OnLoaded();

		if (ApiExtensibility.CreateInstance<INativeContext>(this, out var nativeContext))
		{
			_gl = GL.GetApi(nativeContext);
		}
		else if (ApiExtensibility.CreateInstance<Uno.Graphics.GLGetProcAddress>(this, out var getProcAddress))
		{
			_gl = GL.GetApi(getProcAddress.Invoke);
		}
		else
		{
			throw new InvalidOperationException($"Couldn't create a {nameof(GL)} object for {nameof(GLCanvasElement)}. Make sure you are running on a platform with {nameof(GLCanvasElement)} support.");
		}

		OnLoadedShared();
	}

	private protected override void OnUnloaded() => OnUnloadedShared();
}
#endif
