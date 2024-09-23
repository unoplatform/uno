using System;
using Microsoft.UI.Xaml;
using Silk.NET.OpenGL;
using Uno.Disposables;
using Uno.Graphics;

namespace Uno.WinUI.Runtime.Skia.X11;

internal class X11NativeOpenGLWrapper : INativeOpenGLWrapper
{
	private IntPtr _display;
	private IntPtr _glContext;
	private IntPtr _pBuffer;

	public unsafe void CreateContext(UIElement element)
	{
		if (element.XamlRoot is null || X11Manager.XamlRootMap.GetHostForRoot(element.XamlRoot) is not X11XamlRootHost xamlRootHost)
		{
			throw new InvalidOperationException($"The XamlRoot and its XamlRootHost must be initialized on the element before calling {nameof(CreateContext)}.");
		}

		_display = xamlRootHost.RootX11Window.Display;

		using var lockDisposable = X11Helper.XLock(_display);

		var glxAttribs = new int[]{
			GlxConsts.GLX_DRAWABLE_TYPE   , GlxConsts.GLX_PBUFFER_BIT,
			GlxConsts.GLX_RED_SIZE        , 8,
			GlxConsts.GLX_GREEN_SIZE      , 8,
			GlxConsts.GLX_BLUE_SIZE       , 8,
			GlxConsts.GLX_ALPHA_SIZE      , 8,
			GlxConsts.GLX_DEPTH_SIZE      , 8,
			GlxConsts.GLX_STENCIL_SIZE    , 8,
			(int)X11Helper.None
		};

		IntPtr bestFbc = IntPtr.Zero;
		XVisualInfo* visual = null;
		var ptr = GlxInterface.glXChooseFBConfig(_display, XLib.XDefaultScreen(_display), glxAttribs, out var count);
		if (ptr == null || *ptr == IntPtr.Zero)
		{
			throw new InvalidOperationException($"{nameof(GlxInterface.glXChooseFBConfig)} failed to retrieve GLX framebuffer configurations.");
		}
		for (var c = 0; c < count; c++)
		{
			XVisualInfo* visual_ = GlxInterface.glXGetVisualFromFBConfig(_display, ptr[c]);
			if (visual_->depth == 32) // 24bit color + 8bit stencil as requested above
			{
				bestFbc = ptr[c];
				visual = visual_;
				break;
			}
		}

		if (visual == null)
		{
			throw new InvalidOperationException("Could not create correct visual window.\n");
		}

		_glContext = GlxInterface.glXCreateNewContext(_display, bestFbc, GlxConsts.GLX_RGBA_TYPE, IntPtr.Zero, /* True */ 1);
		_pBuffer = GlxInterface.glXCreatePbuffer(_display, bestFbc, new[] { (int)X11Helper.None });
	}

	public object CreateGLSilkNETHandle() => GL.GetApi(GlxInterface.glXGetProcAddress);

	public void DestroyContext()
	{
		using var lockDisposable = X11Helper.XLock(_display);

		GlxInterface.glXDestroyPbuffer(_display, _pBuffer);
		GlxInterface.glXDestroyContext(_display, _glContext);

		_display = default;
		_glContext = default;
		_pBuffer = default;
	}

	public IDisposable MakeCurrent()
	{
		var glContext = GlxInterface.glXGetCurrentContext();
		var drawable = GlxInterface.glXGetCurrentDrawable();
		GlxInterface.glXMakeCurrent(_display, _pBuffer, _glContext);
		return Disposable.Create(() => GlxInterface.glXMakeCurrent(_display, drawable, glContext));
	}
}
