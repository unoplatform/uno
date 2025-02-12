using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Silk.NET.Core.Contexts;
using Uno.Extensions;
using Uno.Logging;
using Window = Windows.UI.Xaml.Window;

#if !UNO_UWP_BUILD
using Microsoft.UI.Dispatching;
#else
using Windows.System;
#endif

#if WINAPPSDK
using System.Runtime.InteropServices.WindowsRuntime;
#else
using Uno.Foundation.Extensibility;
using Uno.Graphics;
using Uno.UI.Dispatching;
using Buffer = Windows.Storage.Streams.Buffer;
#endif

namespace Uno.WinUI.Graphics3DGL;

/// <summary>
/// A <see cref="FrameworkElement"/> that exposes the ability to draw 3D graphics using OpenGL and Silk.NET.
/// </summary>
/// <remarks>
/// This is only available on WinUI and on skia-based targets running with hardware acceleration.
/// This is currently only available on the WPF and X11 targets (and WinUI).
/// </remarks>
public abstract partial class GLCanvasElement : Grid, INativeContext
{
	private const int BytesPerPixel = 4;
	private static readonly Dictionary<XamlRoot, INativeOpenGLWrapper?> _xamlRootToWrapper = new();

	private static readonly (int major, int minor) _minVersion = (3, 0);

	private readonly Func<Window>? _getWindowFunc;

	private bool _changingGlInitialized;

	// valid if and only if GLCanvasElement was loaded at least once and OpenGL is available on the running platform
	private INativeOpenGLWrapper? _nativeOpenGlWrapper;
	// These are valid if and only if IsLoaded and _nativeOpenGlWrapper is not null
	private GL? _gl;
	private WriteableBitmap? _backBuffer;
	private FrameBufferDetails? _details;
#if WINAPPSDK
	private IntPtr _pixels;
#endif

	/// <summary>
	/// Use this function for the initial setup, e.g. setting up VAOs, VBOs, EBOs, etc.
	/// </summary>
	/// <remarks>
	/// <see cref="Init"/> might be called multiple times. Every call to <see cref="Init"/> except the first one
	/// will be preceded by a call to <see cref="OnDestroy"/>.
	/// </remarks>
	protected abstract void Init(GL gl);
	/// <summary>
	/// Use this function for cleaning up previously allocated resources.
	/// </summary>
	/// /// <remarks>
	/// <see cref="OnDestroy"/> might be called multiple times. Every call to <see cref="OnDestroy"/> will be preceded by
	/// a call to <see cref="Init"/>.
	/// </remarks>
	protected abstract void OnDestroy(GL gl);
	/// <summary>
	/// The rendering logic goes this.
	/// </summary>
	/// <remarks>
	/// Before <see cref="RenderOverride"/> is called, the OpenGL viewport is set to the resolution that was provided to
	/// the <see cref="GLCanvasElement"/> constructor.
	/// </remarks>
	/// <remarks>
	/// Due to the fact that both <see cref="GLCanvasElement"/> and the skia rendering engine used by Uno both use OpenGL,
	/// you must make sure to restore all the OpenGL state values to their original values at the end of <see cref="RenderOverride"/>.
	/// For example, make sure to save the values for the initially-bound OpenGL VAO if you intend to bind your own VAO
	/// and bind the original VAO at the end of the method. Similarly, make sure to disable depth testing at
	/// the end if you choose to enable it.
	/// Some of this may be done for you automatically.
	/// </remarks>
	protected abstract void RenderOverride(GL gl);

	/// <param name="getWindowFunc">A function that returns the Window object that this element belongs to. This parameter is only used on WinUI. On Uno Platform, it can be set to null.</param>
#if WINAPPSDK
	protected GLCanvasElement(Func<Window> getWindowFunc)
#else
	protected GLCanvasElement(Func<Window>? getWindowFunc)
#endif
	{
		_getWindowFunc = getWindowFunc;

		Background = new ImageBrush
		{
			RelativeTransform = new ScaleTransform { ScaleX = 1, ScaleY = -1, CenterX = 0.5, CenterY = 0.5 } // because OpenGL coordinates go bottom-to-top
		};

		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		SizeChanged += (_, _) => UpdateFramebuffer();
	}

	private static unsafe INativeOpenGLWrapper? GetOrCreateNativeOpenGlWrapper(XamlRoot xamlRoot, Func<Window>? getWindowFunc)
	{
		try
		{
			// This is done on the UI thread, so no concurrency concerns.
			if (!_xamlRootToWrapper.TryGetValue(xamlRoot, out var nativeOpenGlWrapper))
			{
#if WINAPPSDK
				nativeOpenGlWrapper = new WinUINativeOpenGLWrapper(xamlRoot, getWindowFunc!);
#else
				if (!ApiExtensibility.CreateInstance(xamlRoot, out nativeOpenGlWrapper))
				{
					if (typeof(GLCanvasElement).Log().IsEnabled(LogLevel.Error))
					{
						typeof(GLCanvasElement).Log().Error($"Couldn't create a {nameof(INativeOpenGLWrapper)} object. Make sure you are running on a platform with OpenGL support.");
					}

					_xamlRootToWrapper[xamlRoot] = null;
					return null;
				}
#endif

				var abort = false;
				using (nativeOpenGlWrapper.MakeCurrent())
				{
					var glGetString = (delegate* unmanaged[Cdecl]<GLEnum, byte*>)nativeOpenGlWrapper.GetProcAddress("glGetString");

					var glVersionBytePtr = glGetString(GLEnum.Version);
					var glVersionString = Marshal.PtrToStringUTF8((IntPtr)glVersionBytePtr);

					if (typeof(GLCanvasElement).Log().IsEnabled(LogLevel.Information))
					{
						typeof(GLCanvasElement).Log().Info($"{nameof(GLCanvasElement)} created an OpenGL context with a version string = '{glVersionString}'.");
					}

					if (glVersionString?.Contains("ANGLE", StringComparison.Ordinal) ?? false)
					{
						if (typeof(GLCanvasElement).Log().IsEnabled(LogLevel.Warning))
						{
							typeof(GLCanvasElement).Log().Warn($"{nameof(GLCanvasElement)} is using an ANGLE implementation, ignoring minimum version checks.");
						}
					}
					else
					{
						var glGetIntegerv = (delegate* unmanaged[Cdecl]<GLEnum, int*, void>)nativeOpenGlWrapper.GetProcAddress("glGetIntegerv");
						int major, minor;
						glGetIntegerv(GLEnum.MajorVersion, &major);
						glGetIntegerv(GLEnum.MinorVersion, &minor);

						if (major < _minVersion.major || (major == _minVersion.major && minor < _minVersion.minor))
						{
							if (typeof(GLCanvasElement).Log().IsEnabled(LogLevel.Error))
							{
								typeof(GLCanvasElement).Log().Error($"{nameof(GLCanvasElement)} requires at least {_minVersion.major}.{_minVersion.minor}, but found {major}.{minor}.");
							}

							abort = true;
						}
					}
				}

				if (abort)
				{
					nativeOpenGlWrapper.Dispose();
					nativeOpenGlWrapper = null;
				}

				_xamlRootToWrapper.Add(xamlRoot, nativeOpenGlWrapper);
			}

			return nativeOpenGlWrapper;
		}
		catch (Exception e)
		{
			if (typeof(GLCanvasElement).Log().IsEnabled(LogLevel.Error))
			{
				typeof(GLCanvasElement).Log().Error($"{nameof(INativeOpenGLWrapper)} creation failed.", e);
			}
			return null;
		}
	}

	private void OnClosed(object _, object __)
	{
		// OnUnloaded is called after OnClosed, which leads to disposing the context first and then trying to
		// delete the framebuffer, etc. and this causes exceptions.
		DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Window is closing. Destroying the {nameof(INativeOpenGLWrapper)} for this window");
			}
			if (_xamlRootToWrapper.Remove(XamlRoot!, out var wrapper))
			{
				using var makeCurrentDisposable = wrapper?.MakeCurrent();
				wrapper?.Dispose();
			}
		});
	}

	/// <summary>
	/// Invalidates the rendering, and queues a call to <see cref="RenderOverride"/>.
	/// <see cref="RenderOverride"/> will only be called once after <see cref="Invalidate"/> and the output will
	/// be saved. You need to call <see cref="Invalidate"/> everytime an update is needed. If drawing an
	/// animation, call <see cref="Invalidate"/> inside <see cref="RenderOverride"/> to continuously invalidate and update.
	/// </summary>
#if WINAPPSDK
	public void Invalidate() => DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, Render);
#else // WPF hangs if we attempt to enqueue on Low inside RenderOverride
	public void Invalidate() => NativeDispatcher.Main.Enqueue(Render, NativeDispatcherPriority.Idle);
#endif

	public static DependencyProperty IsGLInitializedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsGLInitialized),
			typeof(bool?),
			typeof(GLCanvasElement),
			new PropertyMetadata(null, (PropertyChangedCallback)((dO, _) =>
			{
				var @this = (GLCanvasElement)dO;
				if (!@this._changingGlInitialized)
				{
					throw new InvalidOperationException($"{nameof(GLCanvasElement)}.{nameof(IsGLInitializedProperty)} is read-only.");
				}

				// We should have arrived here from set_IsGLInitialized, so we could put this line at the end of the
				// setter. Instead, we set it to false here to prevent users from calling SetValue.IsGLInitializedProperty
				// _inside_ a call to GLCanvasElement.set_IsGLInitialized. This way, if a user intercepts this
				// change (e.g. with SubscribeToPropertyChanged) and attempts to make a nested SetValue call, we still
				// explode in their face.
				@this._changingGlInitialized = false;
			})));

	/// <summary>
	/// Indicates whether this element was loaded successfully or not, including the OpenGL context creation and setup.
	/// This property is only valid when the element is loaded. When the element is not loaded in the visual tree, the value will be null.
	/// </summary>
	public bool? IsGLInitialized
	{
		get => (bool?)GetValue(IsGLInitializedProperty);
		private set
		{
			_changingGlInitialized = true;
			SetValue(IsGLInitializedProperty, value);
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
	{
		_nativeOpenGlWrapper = GetOrCreateNativeOpenGlWrapper(XamlRoot!, _getWindowFunc);

		if (_nativeOpenGlWrapper is null)
		{
			IsGLInitialized = false;
			return;
		}

		_gl = GL.GetApi(this);

		using (_nativeOpenGlWrapper.MakeCurrent())
		{
			UpdateFramebuffer();
			Init(_gl);
		}

		var window =
#if WINAPPSDK
			_getWindowFunc!();
#else
			XamlRoot!.HostWindow;
#endif
		if (window is not null)
		{
			window.Closed += OnClosed;
		}
		else if (XamlRoot.Content is FrameworkElement fe) // for Uno Islands
		{
			fe.Unloaded += OnClosed;
		}

		IsGLInitialized = true;
	}

	private void OnUnloaded(object sender, RoutedEventArgs routedEventArgs)
	{
		IsGLInitialized = null;
		if (_nativeOpenGlWrapper is null)
		{
			return;
		}

		global::System.Diagnostics.Debug.Assert(_gl is not null); // because OnLoaded creates _gl

#if WINAPPSDK
		Marshal.FreeHGlobal(_pixels);
#endif

		using (_nativeOpenGlWrapper!.MakeCurrent())
		{
#if WINAPPSDK
			if (WindowsRenderingNativeMethods.wglGetCurrentContext() == 0)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug("Skipping the disposing step because the window is closing. If it's not closing, then this is unexpected.");
				}
				return;
			}
#endif
			OnDestroy(_gl);
			_details?.Dispose();
			_gl.Dispose();
		}

		_gl = default;
		_details = default;
#if WINAPPSDK
		_pixels = default;
#endif

		var window =
#if WINAPPSDK
			_getWindowFunc!();
#else
			XamlRoot!.HostWindow;
#endif
		if (window is not null)
		{
			window.Closed -= OnClosed;
		}
		else if (XamlRoot.Content is FrameworkElement fe) // for Uno Islands
		{
			fe.Unloaded -= OnClosed;
		}
	}

	private void UpdateFramebuffer()
	{
		if (!IsLoaded || _nativeOpenGlWrapper is null)
		{
			return;
		}

		global::System.Diagnostics.Debug.Assert(_gl is not null);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Updating backing framebuffer with size={RenderSize}");
		}

		using (_nativeOpenGlWrapper!.MakeCurrent())
		{
			_details?.Dispose();
			_details = new FrameBufferDetails(_gl, RenderSize);
		}

#if WINAPPSDK
		if (_pixels != IntPtr.Zero)
		{
			Marshal.FreeHGlobal(_pixels);
		}
		_pixels = Marshal.AllocHGlobal(((int)RenderSize.Width * (int)RenderSize.Height * BytesPerPixel));
#endif

		_backBuffer = new WriteableBitmap((int)RenderSize.Width, (int)RenderSize.Height);
		((ImageBrush)Background).ImageSource = _backBuffer;

		Invalidate();
	}

	private unsafe void Render()
	{
		if (!IsLoaded || _nativeOpenGlWrapper is null)
		{
			return;
		}

		global::System.Diagnostics.Debug.Assert(_gl is not null && _details is not null && _backBuffer is not null);

		using var _ = _nativeOpenGlWrapper!.MakeCurrent();

		_gl!.BindFramebuffer(GLEnum.Framebuffer, _details.Framebuffer);
		{
			_gl.Viewport(new System.Drawing.Size((int)RenderSize.Width, (int)RenderSize.Height));

			RenderOverride(_gl);

			_gl.ReadBuffer(GLEnum.ColorAttachment0);

#if WINAPPSDK
			_gl.ReadPixels(0, 0, (uint)RenderSize.Width, (uint)RenderSize.Height, GLEnum.Bgra, GLEnum.UnsignedByte, (void*)_pixels);
			using (var stream = _backBuffer.PixelBuffer.AsStream())
			{
				stream.Write(new ReadOnlySpan<byte>((void*)_pixels, (int)RenderSize.Width * (int)RenderSize.Height * BytesPerPixel));
			}
#else
			Buffer.Cast(_backBuffer.PixelBuffer).ApplyActionOnRawBufferPtr(ptr =>
			{
				_gl.ReadPixels(0, 0, (uint)RenderSize.Width, (uint)RenderSize.Height, GLEnum.Bgra, GLEnum.UnsignedByte, (void*)ptr);
			});
			_backBuffer.PixelBuffer.Length = (uint)RenderSize.Width * (uint)RenderSize.Height * BytesPerPixel;
#endif
			_backBuffer.Invalidate();
		}
	}

	IntPtr INativeContext.GetProcAddress(string proc, int? slot) => _nativeOpenGlWrapper!.GetProcAddress(proc);
	bool INativeContext.TryGetProcAddress(string proc, [UnscopedRef] out IntPtr addr, int? slot) => _nativeOpenGlWrapper!.TryGetProcAddress(proc, out addr);
	void IDisposable.Dispose() { /* Keep this empty. This is only for INativeContext and will be called by Silk.NET, not us. */ }
}
