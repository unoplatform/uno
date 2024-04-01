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
using Microsoft.UI.Xaml.Controls;

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
		UIElement.ExternalOnChildAdded = OnChildAdded;
		UIElement.ExternalOnChildRemoved = OnChildRemoved;
		Visual.ExternalOnVisualOffsetOrSizeChanged = OnSizeOrOffsetChanged;
		NativeMethods.Initialize(this);

		_displayInformation = DisplayInformation.GetForCurrentView();
		RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;
		_displayInformation.DpiChanged += (_, _) => RasterizationScale = (float)_displayInformation.RawPixelsPerViewPixel;
	}

	private void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (IsAccessibilityEnabled)
		{
			if (AddSemanticElement(parent.Visual.Handle, child, index))
			{
				foreach (var childChild in child._children)
				{
					OnChildAdded(child, childChild, null);
				}
			}
		}
	}

	private void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (IsAccessibilityEnabled)
		{
			if (parent.GetOrCreateAutomationPeer() is { } automationPeer)
			{
				automationPeer.OnPropertyChanged -= AutomationPeer_OnPropertyChanged;
			}

			RemoveSemanticElement(parent.Visual.Handle, child.Visual.Handle);
		}
	}

	private void OnSizeOrOffsetChanged(Visual visual)
	{
		// TODO: transformations (e.g, RenderTransform) are not yet handled :/
		if (IsAccessibilityEnabled && visual is ShapeVisual shapeVisual)
		{
			if (!visual.IsVisible)
			{
				NativeMethods.HideSemanticElement(this, shapeVisual.Handle);
			}
			else
			{
				var totalOffset = visual.GetTotalOffset();
				NativeMethods.UpdateSemanticElementPositioning(this, shapeVisual.Handle, shapeVisual.Size.X, shapeVisual.Size.Y, totalOffset.X, totalOffset.Y);
			}
		}
	}

	public override object? NativeWindow => null;

	internal Window? Window { get; private set; }

	internal new XamlRoot? XamlRoot { get; private set; }

	public override string Title
	{
		get => NativeMethods.GetWindowTitle();
		set => NativeMethods.SetWindowTitle(value);
	}

	internal bool IsAccessibilityEnabled { get; private set; }

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
		if (IsAccessibilityEnabled)
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
		IsAccessibilityEnabled = true;
		CreateAOM(rootElement);
		Control.OnIsFocusableChangedCallback = UpdateIsFocusable;
	}

	private void UpdateIsFocusable(Control control, bool isFocusable)
	{
		NativeMethods.UpdateIsFocusable(this, control.Visual.Handle, isFocusable);
	}

	internal void CreateAOM(UIElement rootElement)
	{
		Debug.Assert(IsAccessibilityEnabled);

		// We build an AOM (Accessibility Object Model):
		// https://wicg.github.io/aom/explainer.html
		var rootHandle = rootElement.Visual.Handle;

		var totalOffset = rootElement.Visual.GetTotalOffset();
		NativeMethods.AddRootElementToSemanticsRoot(this, rootHandle, rootElement.Visual.Size.X, rootElement.Visual.Size.Y, totalOffset.X, totalOffset.Y, rootElement.IsFocusable);
		foreach (var child in rootElement.GetChildren())
		{
			BuildSemanticsTreeRecursive(rootHandle, child);
		}
	}

	internal void BuildSemanticsTreeRecursive(IntPtr parentHandle, UIElement child)
	{
		Debug.Assert(IsAccessibilityEnabled);

		var handle = child.Visual.Handle;

		AddSemanticElement(parentHandle, child, null);
		foreach (var childChild in child.GetChildren())
		{
			BuildSemanticsTreeRecursive(handle, childChild);
		}
	}

	private bool AddSemanticElement(IntPtr parentHandle, UIElement child, int? index)
	{
		var totalOffset = child.Visual.GetTotalOffset();
		var role = AutomationProperties.FindHtmlRole(child);
		var automationId = AutomationProperties.GetAutomationId(child);
		var automationPeer = child.GetOrCreateAutomationPeer();

		if (automationPeer is not null)
		{
			automationPeer.OnPropertyChanged += AutomationPeer_OnPropertyChanged;

			// TODO: Verify if this is the right behavior.
			if (string.IsNullOrEmpty(automationId))
			{
				automationId = automationPeer.GetName();
			}
		}

		string? ariaChecked = null;
		if (child is CheckBox checkBox)
		{
			ariaChecked = ConvertToAriaChecked(checkBox.IsChecked);
		}
		else if (child is RadioButton radioButton)
		{
			ariaChecked = ConvertToAriaChecked(radioButton.IsChecked);
		}
		// TODO: aria-valuenow, aria-valuemin, aria-valuemax for Slider

		return NativeMethods.AddSemanticElement(this, parentHandle, child.Visual.Handle, index, child.Visual.Size.X, child.Visual.Size.Y, totalOffset.X, totalOffset.Y, role, automationId, child.IsFocusable, ariaChecked, child.Visual.IsVisible);
	}

	private void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle)
	{
		NativeMethods.RemoveSemanticElement(this, parentHandle, childHandle);
	}

	// Important to keep this static to avoid memory leaks.
	// Otherwise, proper event un-subscription will be needed.
	private static void AutomationPeer_OnPropertyChanged(UIElement element, AutomationProperty automationProperty, object value)
	{
		if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty)
		{
			var ariaChecked = ConvertToAriaChecked((ToggleState)value);
			NativeMethods.UpdateAriaChecked(Instance, element.Visual.Handle, ariaChecked);
		}
	}

	private static string? ConvertToAriaChecked(ToggleState isChecked)
	{
		return isChecked switch
		{
			ToggleState.On => "true",
			ToggleState.Off => "false",
			ToggleState.Indeterminate => "mixed",
			_ => null,
		};
	}

	private static string? ConvertToAriaChecked(bool? isChecked)
	{
		return isChecked switch
		{
			true => "true",
			false => "false",
			null => "mixed",
		};
	}

	internal void OnAutomationIdChanged(UIElement element, string automationId)
	{
		Debug.Assert(IsAccessibilityEnabled);
		NativeMethods.UpdateAriaLabel(this, element.Visual.Handle, automationId);
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
		internal static partial void AddRootElementToSemanticsRoot([JSMarshalAs<JSType.Any>] object owner, IntPtr rootHandle, float width, float height, float x, float y, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.addSemanticElement")]
		internal static partial bool AddSemanticElement([JSMarshalAs<JSType.Any>] object owner, IntPtr parentHandle, IntPtr handle, int? index, float width, float height, float x, float y, string role, string automationId, bool isFocusable, string? ariaChecked, bool isVisible);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.removeSemanticElement")]
		internal static partial void RemoveSemanticElement([JSMarshalAs<JSType.Any>] object owner, IntPtr parentHandle, IntPtr childHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.updateAriaLabel")]
		internal static partial void UpdateAriaLabel([JSMarshalAs<JSType.Any>] object owner, IntPtr handle, string automationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.updateAriaChecked")]
		internal static partial void UpdateAriaChecked([JSMarshalAs<JSType.Any>] object owner, IntPtr handle, string? ariaChecked);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.updateSemanticElementPositioning")]
		internal static partial void UpdateSemanticElementPositioning([JSMarshalAs<JSType.Any>] object owner, IntPtr handle, float width, float height, float x, float y);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.updateIsFocusable")]
		internal static partial void UpdateIsFocusable([JSMarshalAs<JSType.Any>] object owner, IntPtr handle, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.WebAssemblyWindowWrapper.hideSemanticElement")]
		internal static partial void HideSemanticElement([JSMarshalAs<JSType.Any>] object owner, IntPtr handle);

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.getWindowTitle")]
		internal static partial string GetWindowTitle();

		[JSImport("globalThis.Windows.UI.ViewManagement.ApplicationView.setWindowTitle")]
		internal static partial void SetWindowTitle(string title);
	}
}
