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
using Microsoft.UI.Xaml;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia;

internal partial class WebAssemblyWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<WebAssemblyWindowWrapper> _instance = new Lazy<WebAssemblyWindowWrapper>(() => new());

	internal static WebAssemblyWindowWrapper Instance => _instance.Value;

	public WebAssemblyWindowWrapper()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing {nameof(WebAssemblyWindowWrapper)}");
		}

		NativeMethods.Initialize(this);
	}

	public override object? NativeWindow => null;

	internal Window? Window { get; private set; }

	internal XamlRoot? XamlRoot { get; private set; }

	internal void RaiseNativeSizeChanged(Size newWindowSize)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			Console.WriteLine($"RaiseNativeSizeChanged({newWindowSize.Width}, {newWindowSize.Height})");
		}

		Bounds = new Rect(default, newWindowSize);
		VisibleBounds = new Rect(default, newWindowSize);
	}

	internal void OnNativeVisibilityChanged(bool visible) => Visible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosed();

	internal void SetWindow(Window window, XamlRoot xamlRoot)
	{
		Window = window;
		XamlRoot = xamlRoot;
	}

	[JSExport]
	private static void OnResize([JSMarshalAs<JSType.Any>] object instance, double width, double height)
	{
		if (instance is WebAssemblyWindowWrapper windowWrapper)
		{
			windowWrapper.RaiseNativeSizeChanged(new(width, height));
		}
		else
		{
			Console.WriteLine($"RaiseNativeSizeChanged target for {instance} does not exist");
		}
	}

	internal string CanvasId
		=> NativeMethods.GetCanvasId(this);

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.initialize")]
		public static partial void Initialize([JSMarshalAs<JSType.Any>] object owner);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.getContainerId")]
		public static partial string GetContainerId([JSMarshalAs<JSType.Any>] object owner);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.getCanvasId")]
		public static partial string GetCanvasId([JSMarshalAs<JSType.Any>] object owner);
	}
}
