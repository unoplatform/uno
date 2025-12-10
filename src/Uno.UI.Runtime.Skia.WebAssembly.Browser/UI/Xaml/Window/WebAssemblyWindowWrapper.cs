#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Uno.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Input;
using static Windows.UI.Input.PointerUpdateKind;
using System.Runtime.CompilerServices;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;
using FontFamilyHelper = Microsoft.UI.Xaml.FontFamilyHelper;
using Windows.Graphics;
using Uno.Disposables;

namespace Uno.UI.Runtime.Skia;

internal partial class WebAssemblyWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<WebAssemblyWindowWrapper> _instance = new Lazy<WebAssemblyWindowWrapper>(() => new());
	private DisplayInformation _displayInformation;

	internal static WebAssemblyWindowWrapper Instance => _instance.Value;

	public WebAssemblyWindowWrapper()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing {nameof(WebAssemblyWindowWrapper)}");
		}

		NativeMethods.Initialize(this);

		_displayInformation = DisplayInformation.GetForCurrentView();
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;
		_displayInformation.DpiChanged += (_, _) => RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;
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
	private static async Task PrefetchFonts()
	{
		var symbolsFontSuccess = await FontFamilyHelper.PreloadAsync(new FontFamily(FeatureConfiguration.Font.SymbolsFont), FontWeights.Normal, FontStretch.Normal, FontStyle.Normal);
		if (symbolsFontSuccess)
		{
			typeof(WebAssemblyWindowWrapper).Log().Info("The default symbols font was preloaded successfully.");
		}

		if (Uri.TryCreate(FeatureConfiguration.Font.DefaultTextFontFamily, UriKind.RelativeOrAbsolute, out var uri))
		{
			var textFontSuccess = await FontFamilyHelper.PreloadAllFontsInManifest(uri);
			if (textFontSuccess)
			{
				typeof(WebAssemblyWindowWrapper).Log().Info("The default text font was preloaded successfully.");
			}
		}
		else
		{
			await FontFamilyHelper.PreloadAsync(
				FeatureConfiguration.Font.DefaultTextFontFamily,
				FontWeights.Normal,
				FontStretch.Normal,
				FontStyle.Normal);
		}
	}

	internal string CanvasId
		=> NativeMethods.GetCanvasId(this);
}
