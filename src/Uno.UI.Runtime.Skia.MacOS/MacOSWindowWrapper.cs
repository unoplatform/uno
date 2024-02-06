#nullable enable

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;

using SkiaSharp;

using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

using Window = Microsoft.UI.Xaml.Window;

using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.MacOS;

internal class MacOSWindowWrapper : NativeWindowWrapperBase, IXamlRootHost
{
	private readonly Window _window;
	private readonly bool _mainWindow;
	private readonly nint _handle;
	private readonly ApplicationView _applicationView;

	private readonly GRContext? _context;


	internal static XamlRootMap<IXamlRootHost> XamlRootMap { get; } = new();

	public MacOSWindowWrapper(Window winUIWindow, XamlRoot xamlRoot)
	{
		_window = winUIWindow ?? throw new ArgumentNullException(nameof(winUIWindow));

		if (MacSkiaHost.Current is not null)
		{
			_mainWindow = true;
			_handle = NativeUno.uno_app_get_main_window();
			MacSkiaHost.Current.InitialWindow ??= this;
		}
		else
		{
			_mainWindow = false;
			// _handle = NativeUno.uno_window_new();
		}

		var ctx = NativeUno.uno_window_get_metal(_handle);
		// Sadly only the `net6.0-[mac][ios]` version of SkiaSharp supports Metal and depends on Microsoft.[macOS|iOS].dll
		// IOW neither `net6.0` or `netstandard2.0` have the required API to create a Metal context for Skia
		// This force us to initialize things manually... so we reflect to create a metal-based GRContext
		// FIXME: contribute some extra API (e.g. using `nint` or `IntPtr`) to SkiaSharp to avoid reflection
		// net8+ alternative -> https://steven-giesel.com/blogPost/05ecdd16-8dc4-490f-b1cf-780c994346a4
		var get = typeof(GRContext).GetMethod("GetObject", BindingFlags.Static | BindingFlags.NonPublic)!;
		_context = (GRContext?) get?.Invoke(null, new object [] { ctx, true });
		if (_context is null)
		{
			// Macs since 2012 have Metal 2 support and macOS 10.14 Mojave (2018) requires Metal
			// List of Mac supporting Metal https://support.apple.com/en-us/HT205073
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to initialize Metal.");
			}
		}
		MacOSMetalRenderer.Register(this);

		Size preferredWindowSize = ApplicationView.PreferredLaunchViewSize;
		if (preferredWindowSize != Size.Empty)
		{
			NativeUno.uno_window_resize(_handle, preferredWindowSize.Width, preferredWindowSize.Height);
		}
		else
		{
			NativeUno.uno_window_resize(_handle, 1024, 800);
		}

		XamlRootMap.Register(xamlRoot, this);

		_applicationView = ApplicationView.GetForWindowId(winUIWindow.AppWindow.Id);
		_applicationView.PropertyChanged += OnApplicationViewPropertyChanged;

		// TODO: closed/destroyed
		// TODO: shown

		UpdateWindowPropertiesFromPackage();
		UpdateWindowPropertiesFromApplicationView();
	}

	// TODO: unregister window on close

	public bool IsMainWindow => _mainWindow;

	public nint Handle => _handle;

	public override object NativeWindow => _handle;

	private void UpdateWindowPropertiesFromPackage()
	{
		if (Package.Current.Logo is { } uri)
		{
			var basePath = uri.OriginalString.Replace('\\', Path.DirectorySeparatorChar);
			var iconPath = Path.Combine(Package.Current.InstalledPath, basePath);

			if (File.Exists(iconPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{iconPath}] from Package.appxmanifest file");
				}

				NativeUno.uno_application_set_icon(iconPath);
			}
			else if (BitmapImage.GetScaledPath(basePath) is { } scaledPath && File.Exists(scaledPath))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"Loading icon file [{scaledPath}] scaled logo from Package.appxmanifest file");
				}

				NativeUno.uno_application_set_icon(iconPath);
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"Unable to find icon file [{iconPath}] specified in the Package.appxmanifest file.");
				}
			}
		}

		if (string.IsNullOrEmpty(_applicationView.Title))
		{
			_applicationView.Title = Package.Current.DisplayName;
		}
	}

	private void OnApplicationViewPropertyChanged(object? sender, PropertyChangedEventArgs e) => UpdateWindowPropertiesFromApplicationView();

	private void UpdateWindowPropertiesFromApplicationView()
	{
		NativeUno.uno_window_set_title(_handle, _applicationView.Title);
		var minSize = _applicationView.PreferredMinSize;
		NativeUno.uno_window_set_min_size(_handle, minSize.Width, minSize.Height);
	}

	private static readonly ConstructorInfo? _rt = typeof(GRBackendRenderTarget).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(nint), typeof(bool)], null);

	internal unsafe void Draw(double nativeWidth, double nativeHeight, nint texture)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Window {_handle} drawing {nativeWidth}x{nativeHeight} texture: {texture} FullScreen: {NativeUno.uno_application_is_full_screen()}");
		}

		// note: size is doubled for retina displays
		var info = new GRMtlTextureInfoNative() { Texture = texture };
		var nt = NativeSkia.gr_backendrendertarget_new_metal((int)nativeWidth, (int)nativeHeight, 1, &info);
		if (nt == IntPtr.Zero)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to initialize Skia with Metal backend.");
			}
		}
		// FIXME: contribute some extra API (e.g. using `nint` or `IntPtr`) to SkiaSharp to avoid reflection
       	using var target = (GRBackendRenderTarget)_rt?.Invoke(new object[] { nt, true })!;
		using var surface = SKSurface.Create(_context, target, GRSurfaceOrigin.TopLeft, SKColorType.Bgra8888);
		using var canvas = surface.Canvas;

		using (new SKAutoCanvasRestore(canvas, true))
		{
			canvas.Clear(SKColors.White);
			// canvas.Clear(BackgroundColor);

			if (RootElement?.Visual is { } rootVisual)
			{
				RootElement.XamlRoot?.Compositor.RenderRootVisual(surface, rootVisual);
			}
		}

		canvas.Flush();
		surface.Flush();
		_context?.Flush();
	}

	internal void UpdateWindowSize(double nativeWidth, double nativeHeight)
	{
		// SizeChanged?.Invoke(this, new Size(nativeWidth, nativeHeight));
		Console.WriteLine();
	}

	public UIElement? RootElement => _window.RootElement;

	public void InvalidateRender()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Window {_handle} invalidated.");
		}

		RootElement?.XamlRoot?.InvalidateOverlays();
		NativeUno.uno_window_invalidate(_handle);
	}
}
