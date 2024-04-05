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

internal partial class WebAssemblyAccessibility : IUnoAccessibility
{
	private static readonly Lazy<WebAssemblyAccessibility> _instance = new Lazy<WebAssemblyAccessibility>(() => new());

	internal static WebAssemblyAccessibility Instance => _instance.Value;

	public WebAssemblyAccessibility()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing {nameof(WebAssemblyAccessibility)}");
		}

		AccessibilityAnnouncer.AccessibilityImpl = this;
		UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAdded;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemoved;
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = OnSizeOrOffsetChanged;
	}

	public bool IsAccessibilityEnabled { get; private set; }

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
				NativeMethods.HideSemanticElement(shapeVisual.Handle);
			}
			else
			{
				var totalOffset = visual.GetTotalOffset();
				NativeMethods.UpdateSemanticElementPositioning(shapeVisual.Handle, shapeVisual.Size.X, shapeVisual.Size.Y, totalOffset.X, totalOffset.Y);
			}
		}
	}

	[JSExport]
	public static void EnableAccessibility()
	{
		var @this = Instance;
		if (@this.IsAccessibilityEnabled)
		{
			if (@this.Log().IsEnabled(LogLevel.Warning))
			{
				@this.Log().LogWarning("EnableA11y is called for the second time. This shouldn't happen.");
			}

			return;
		}

		if (WebAssemblyWindowWrapper.Instance.Window?.RootElement is not { } rootElement)
		{
			if (@this.Log().IsEnabled(LogLevel.Warning))
			{
				@this.Log().LogWarning("EnableA11y is called while either Window or its RootElement is null. This shouldn't happen.");
			}

			return;
		}

		AutomationProperties.OnAutomationIdChangedCallback = @this.OnAutomationIdChanged;
		@this.IsAccessibilityEnabled = true;
		@this.CreateAOM(rootElement);
		Control.OnIsFocusableChangedCallback = @this.UpdateIsFocusable;
	}

	private void UpdateIsFocusable(Control control, bool isFocusable)
	{
		NativeMethods.UpdateIsFocusable(control.Visual.Handle, isFocusable);
	}

	internal void CreateAOM(UIElement rootElement)
	{
		Debug.Assert(IsAccessibilityEnabled);

		// We build an AOM (Accessibility Object Model):
		// https://wicg.github.io/aom/explainer.html
		var rootHandle = rootElement.Visual.Handle;

		var totalOffset = rootElement.Visual.GetTotalOffset();
		NativeMethods.AddRootElementToSemanticsRoot(rootHandle, rootElement.Visual.Size.X, rootElement.Visual.Size.Y, totalOffset.X, totalOffset.Y, rootElement.IsFocusable);
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

		return NativeMethods.AddSemanticElement(parentHandle, child.Visual.Handle, index, child.Visual.Size.X, child.Visual.Size.Y, totalOffset.X, totalOffset.Y, role, automationId, child.IsFocusable, ariaChecked, child.Visual.IsVisible);
	}

	private void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle)
	{
		NativeMethods.RemoveSemanticElement(parentHandle, childHandle);
	}

	// Important to keep this static to avoid memory leaks.
	// Otherwise, proper event un-subscription will be needed.
	private static void AutomationPeer_OnPropertyChanged(UIElement element, AutomationProperty automationProperty, object value)
	{
		if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty)
		{
			var ariaChecked = ConvertToAriaChecked((ToggleState)value);
			NativeMethods.UpdateAriaChecked(element.Visual.Handle, ariaChecked);
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
		NativeMethods.UpdateAriaLabel(element.Visual.Handle, automationId);
	}

	public void AnnouncePolite(string text)
		=> NativeMethods.AnnouncePolite(text);

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.addRootElementToSemanticsRoot")]
		internal static partial void AddRootElementToSemanticsRoot(IntPtr rootHandle, float width, float height, float x, float y, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.addSemanticElement")]
		internal static partial bool AddSemanticElement(IntPtr parentHandle, IntPtr handle, int? index, float width, float height, float x, float y, string role, string automationId, bool isFocusable, string? ariaChecked, bool isVisible);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.removeSemanticElement")]
		internal static partial void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLabel")]
		internal static partial void UpdateAriaLabel(IntPtr handle, string automationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaChecked")]
		internal static partial void UpdateAriaChecked(IntPtr handle, string? ariaChecked);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateSemanticElementPositioning")]
		internal static partial void UpdateSemanticElementPositioning(IntPtr handle, float width, float height, float x, float y);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateIsFocusable")]
		internal static partial void UpdateIsFocusable(IntPtr handle, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.hideSemanticElement")]
		internal static partial void HideSemanticElement(IntPtr handle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.announcePolite")]
		internal static partial void AnnouncePolite(string text);
	}
}
