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
using Microsoft.UI.Composition;
using System.Diagnostics;
using Microsoft.UI.Xaml.Automation;

namespace Uno.UI.Runtime.Skia;

internal partial class WebAssemblyWindowWrapper : NativeWindowWrapperBase
{
	private sealed class CompositionListener : CompositionObject
	{
		private readonly WebAssemblyWindowWrapper _owner;
		private readonly UIElement _rootElement;

		public CompositionListener(WebAssemblyWindowWrapper owner, UIElement rootElement) : base(rootElement.Visual.Compositor)
		{
			_owner = owner;
			_rootElement = rootElement;
		}

		private protected override void OnPropertyChangedCore(string? propertyName, bool isSubPropertyChange)
		{
			base.OnPropertyChangedCore(propertyName, isSubPropertyChange);
			_owner.CreateAOM(_rootElement);
		}
	}

	private static readonly Lazy<WebAssemblyWindowWrapper> _instance = new Lazy<WebAssemblyWindowWrapper>(() => new());
	private DisplayInformation _displayInformation;
	private CompositionListener? _compositionListener;

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
		if (_compositionListener is not null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("EnableA11y is called for the second time. This shouldn't happen.");
			}

			return;
		}

		if (Window?.RootElement is not { } rootElement)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("EnableA11y is called while either Window or its RootElement is null. This shouldn't happen.");
			}

			return;
		}

		AutomationProperties.OnAutomationIdChangedCallback = OnAutomationIdChanged;
		_compositionListener = new CompositionListener(this, rootElement);
		CreateAOM(rootElement);
	}

	internal void CreateAOM(UIElement rootElement)
	{
		Debug.Assert(_compositionListener is not null);

		// We build an AOM (Accessibility Object Model):
		// https://wicg.github.io/aom/explainer.html
		var rootHashCode = rootElement.Visual.GetHashCode();
		rootElement.Visual.RemoveContext(_compositionListener, null);
		rootElement.Visual.AddContext(_compositionListener, null);

		var totalOffset = rootElement.Visual.GetTotalOffset();
		NativeMethods.AddRootElementToSemanticsRoot(this, rootHashCode, rootElement.Visual.Size.X, rootElement.Visual.Size.Y, totalOffset.X, totalOffset.Y);
		foreach (var child in rootElement.GetChildren())
		{
			BuildSemanticsTreeRecursive(rootHashCode, child);
		}
	}

	internal void BuildSemanticsTreeRecursive(int parentHashCode, UIElement child)
	{
		Debug.Assert(_compositionListener is not null);

		var hashCode = child.Visual.GetHashCode();
		child.Visual.RemoveContext(_compositionListener, null);
		child.Visual.AddContext(_compositionListener, null);

		var totalOffset = child.Visual.GetTotalOffset();

		var role = AutomationProperties.FindHtmlRole(child);
		var automationId = AutomationProperties.GetAutomationId(child);

		NativeMethods.AddSemanticElement(this, parentHashCode, hashCode, child.Visual.Size.X, child.Visual.Size.Y, totalOffset.X, totalOffset.Y, role, automationId);
		foreach (var childChild in child.GetChildren())
		{
			BuildSemanticsTreeRecursive(hashCode, childChild);
		}
	}

	internal void OnAutomationIdChanged(UIElement element, string automationId)
	{
		NativeMethods.UpdateAriaLabel(this, element.Visual.GetHashCode(), automationId);
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
		internal static partial void AddSemanticElement([JSMarshalAs<JSType.Any>] object owner, int parentHashCode, int hashCode, float width, float height, float x, float y, string role, string automationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.updateAriaLabel")]
		internal static partial void UpdateAriaLabel([JSMarshalAs<JSType.Any>] object owner, int hashCode, string automationId);

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.getWindowTitle")]
		internal static partial string GetWindowTitle();

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.setWindowTitle")]
		internal static partial void SetWindowTitle(string title);
	}
}
