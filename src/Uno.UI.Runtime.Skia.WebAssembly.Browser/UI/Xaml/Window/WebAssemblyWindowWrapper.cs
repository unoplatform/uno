#nullable enable

using System;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Windows.UI.Text;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Controls;
using FontFamilyHelper = Microsoft.UI.Xaml.FontFamilyHelper;
using Windows.Graphics;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace Uno.UI.Runtime.Skia;

internal partial class WebAssemblyWindowWrapper : NativeWindowWrapperBase
{
	private static WebAssemblyWindowWrapper? _instance;
	private DisplayInformation? _displayInformation;

	internal static WebAssemblyWindowWrapper Instance => _instance!;

	public static async Task Initialize()
	{
		_instance = new();
		await NativeMethods.Initialize(_instance);

		typeof(WebAssemblyWindowWrapper).LogTrace()?.Trace($"Initializing {nameof(WebAssemblyWindowWrapper)}");

		_instance._displayInformation = DisplayInformation.GetForCurrentView();
		_instance.RasterizationScale = (float)_instance._displayInformation.RawPixelsPerViewPixel;
		_instance._displayInformation.DpiChanged += (_, _) => _instance.RasterizationScale = (float)_instance._displayInformation.RawPixelsPerViewPixel;
	}

	private WebAssemblyWindowWrapper()
	{
	}

	public override object? NativeWindow => null;

	public override string Title
	{
		get => NativeMethods.GetWindowTitle();
		set => NativeMethods.SetWindowTitle(value);
	}

	protected override IDisposable ApplyFullScreenPresenter()
	{
		SetFullScreenMode(true);
		return Disposable.Create(() => SetFullScreenMode(false));
	}

	public override void Move(PointInt32 position) => NativeMethods.MoveWindow(position.X, position.Y);

	public override void Resize(SizeInt32 size) => NativeMethods.ResizeWindow(size.Width, size.Height);

	private bool SetFullScreenMode(bool turnOn) => NativeMethods.SetFullScreenMode(turnOn);

	internal void RaiseNativeSizeChanged(Size newWindowSize)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			Console.WriteLine($"RaiseNativeSizeChanged({newWindowSize.Width}, {newWindowSize.Height})");
		}

		var bounds = new Rect(default, newWindowSize);
		SetBoundsAndVisibleBounds(bounds, bounds);
		var size = new Windows.Graphics.SizeInt32((int)(newWindowSize.Width * RasterizationScale), (int)(newWindowSize.Height * RasterizationScale));
		SetSizes(size, size);
	}

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	[JSExport]
	private static void OnResize([JSMarshalAs<JSType.Any>] object instance, double width, double height, float scale)
	{
		if (instance is WebAssemblyWindowWrapper windowWrapper)
		{
			windowWrapper.RasterizationScale = scale;
			windowWrapper.RaiseNativeSizeChanged(new(width, height));
		}
		else
		{
			Console.WriteLine($"RaiseNativeSizeChanged target for {instance} does not exist");
		}
	}

	[JSExport]
	private static void OnInputPaneChanged([JSMarshalAs<JSType.Any>] object instance, double x, double y, double width, double height)
	{
		if (instance is WebAssemblyWindowWrapper windowWrapper)
		{
			var inputPane = InputPane.GetForCurrentView();
			inputPane.OccludedRect = height > 0
				? new Rect(x, y, width, height)
				: new Rect(0, 0, 0, 0);
		}
	}

	protected override void ShowCore()
	{
		if (Application.Current.FontPreloadTask is { } task)
		{
			task.ContinueWith(_ =>
			{
				NativeDispatcher.Main.Enqueue(() =>
				{
					if (XamlRoot?.Content is FrameworkElement content)
					{
						content.InvalidateArrange();
						content.LayoutUpdated += OnContentLayoutUpdated;

						void OnContentLayoutUpdated(object? sender, object e)
						{
							content.LayoutUpdated -= OnContentLayoutUpdated;

							var compositionTarget = (CompositionTarget)XamlRoot?.Content?.Visual.CompositionTarget!;
							var host = (WebAssemblyBrowserHost)XamlRootMap.GetHostForRoot(XamlRoot!)!;
							compositionTarget.FrameRendered += CompositionTargetOnFrameRendered;
							((IXamlRootHost)host).InvalidateRender();
							void CompositionTargetOnFrameRendered()
							{
								compositionTarget.FrameRendered -= CompositionTargetOnFrameRendered;
								host.RemoveSplashScreen();
							}
						}
					}
				});
			});
		}
	}

	internal string CanvasId
		=> NativeMethods.GetCanvasId(this);
}
