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
using Uno.Helpers;
using System.Numerics;

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

		AccessibilityAnnouncer.WindowWrapper = this;
		NativeMethods.Initialize(this);

		_displayInformation = DisplayInformation.GetForCurrentView();
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;
		_displayInformation.DpiChanged += (_, _) => RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;
	}

	public override object? NativeWindow => null;

	internal Window? Window { get; private set; }

	internal new XamlRoot? XamlRoot { get; private set; }

	public override string Title
	{
		get => NativeMethods.GetWindowTitle();
		set => NativeMethods.SetWindowTitle(value);
	}

	internal void RaiseNativeSizeChanged(Size newWindowSize)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			Console.WriteLine($"RaiseNativeSizeChanged({newWindowSize.Width}, {newWindowSize.Height})");
		}

		Bounds = new Rect(default, newWindowSize);
		VisibleBounds = new Rect(default, newWindowSize);
	}

	internal void EnableA11y()
	{
		// We build an AOM (Accessibility Object Model):
		// https://wicg.github.io/aom/explainer.html
		if (Window?.RootElement is { } rootElement)
		{
			var rootHashCode = rootElement.GetHashCode();
			NativeMethods.AddRootElementToSemanticsRoot(this, rootHashCode, rootElement.Visual.Size.X, rootElement.Visual.Size.Y, rootElement.Visual.Offset.X, rootElement.Visual.Offset.Y);
			foreach (var child in rootElement.GetChildren())
			{
				BuildSemanticsTreeRecursive(rootHashCode, child);
			}
		}
	}

	internal void BuildSemanticsTreeRecursive(int parentHashCode, UIElement child)
	{
		var hashCode = child.GetHashCode();
		NativeMethods.AddSemanticElement(this, parentHashCode, hashCode, child.Visual.Size.X, child.Visual.Size.Y, child.Visual.Offset.X, child.Visual.Offset.Y);
		foreach (var childChild in child.GetChildren())
		{
			BuildSemanticsTreeRecursive(hashCode, childChild);
		}
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

	[JSExport]
	private static void EnableA11y([JSMarshalAs<JSType.Any>] object instance)
	{
		if (instance is WebAssemblyWindowWrapper windowWrapper)
		{
			windowWrapper.EnableA11y();
		}
		else
		{
			Console.WriteLine($"EnableA11y target for {instance} does not exist");
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

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.addRootElementToSemanticsRoot")]
		internal static partial void AddRootElementToSemanticsRoot([JSMarshalAs<JSType.Any>] object owner, int rootHashCode, float width, float height, float x, float y);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.addSemanticElement")]
		internal static partial void AddSemanticElement([JSMarshalAs<JSType.Any>] object owner, int parentHashCode, int hashCode, float width, float height, float x, float y);

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.getWindowTitle")]
		internal static partial string GetWindowTitle();

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.setWindowTitle")]
		internal static partial void SetWindowTitle(string title);
	}
}
