#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.Helpers;

namespace Uno.UI.Runtime.Skia;

internal partial class WebAssemblyAccessibility : IUnoAccessibility, IAutomationPeerListener
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
		AutomationPeer.AutomationPeerListener = this;
	}

	public bool IsAccessibilityEnabled { get; private set; }

	private Vector3 GetVisualOffset(Visual visual)
	{
		return visual.GetTotalOffset();
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
				var totalOffset = GetVisualOffset(visual);
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

		@this.IsAccessibilityEnabled = true;
		@this.CreateAOM(rootElement);
		Control.OnIsFocusableChangedCallback = @this.UpdateIsFocusable;
	}

	[JSExport]
	public static void OnScroll(IntPtr handle, double horizontalOffset, double verticalOffset)
	{
		var @this = Instance;
		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
			// TODO: We shouldn't check individual scrollers.
			// Instead, we should scroll using automation peers once they are implemented correctly for SCP and ScrollPresenter
			if (owner is ScrollContentPresenter scp)
			{
				scp.Set(horizontalOffset, verticalOffset);
			}
			else if (owner is ScrollPresenter sp)
			{
				sp.ScrollTo(horizontalOffset, verticalOffset);
			}
		}
	}

	private void UpdateIsFocusable(Control control, bool isFocusable)
	{
		NativeMethods.UpdateIsFocusable(control.Visual.Handle, IsAccessibilityFocusable(control, isFocusable));
	}

	private static bool IsAccessibilityFocusable(DependencyObject dependencyObject, bool isFocusable)
	{
		// We'll consider TextBlock and RichTextBlock as accessibility focusable, even if they are not focusable.
		// Screen readers should read them.
		if (!isFocusable && dependencyObject is not (TextBlock or RichTextBlock))
		{
			return false;
		}

		var accessibilityView = AutomationProperties.GetAccessibilityView(dependencyObject);
		if (accessibilityView == AccessibilityView.Raw)
		{
			return false;
		}

		// TODO: Adjust when TextElement's automation peers are supported.
		if ((dependencyObject as UIElement)?.GetOrCreateAutomationPeer() is null)
		{
			return false;
		}

		return true;
	}

	internal void CreateAOM(UIElement rootElement)
	{
		Debug.Assert(IsAccessibilityEnabled);

		// We build an AOM (Accessibility Object Model):
		// https://wicg.github.io/aom/explainer.html
		var rootHandle = rootElement.Visual.Handle;

		var totalOffset = GetVisualOffset(rootElement.Visual);
		NativeMethods.AddRootElementToSemanticsRoot(rootHandle, rootElement.Visual.Size.X, rootElement.Visual.Size.Y, totalOffset.X, totalOffset.Y, IsAccessibilityFocusable(rootElement, rootElement.IsFocusable));
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
		var totalOffset = GetVisualOffset(child.Visual);
		var role = AutomationProperties.FindHtmlRole(child);
		var automationId = AutomationProperties.GetAutomationId(child);
		var automationPeer = child.GetOrCreateAutomationPeer();
		var horizontallyScrollable = false;
		var verticallyScrollable = false;
		if (automationPeer is not null)
		{
			// TODO: Verify if this is the right behavior.
			if (string.IsNullOrEmpty(automationId))
			{
				automationId = automationPeer.GetName();
			}
		}

		if (automationPeer is IScrollProvider scrollProvider)
		{
			//horizontallyScrollable = scrollProvider.HorizontallyScrollable;
			//verticallyScrollable = scrollProvider.VerticallyScrollable;
			horizontallyScrollable = true;
			verticallyScrollable = true;
		}
		else if (child.IsScrollPort)
		{
			// Workaround: ScrollViewerAutomationPeer isn't implemented.
			//var extentWidth = sv.ExtentWidth;
			//var viewportWidth = sv.ViewportWidth;
			//var minHorizontalOffset = sv.MinHorizontalOffset;
			//horizontallyScrollable = DoubleUtil.GreaterThan(extentWidth, viewportWidth + minHorizontalOffset);

			//var extentHeight = sv.ExtentHeight;
			//var viewportHeight = sv.ViewportHeight;
			//var minVerticalOffset = sv.MinVerticalOffset;
			//verticallyScrollable = DoubleUtil.GreaterThan(extentHeight, viewportHeight + minVerticalOffset);
			horizontallyScrollable = true;
			verticallyScrollable = true;
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

		return NativeMethods.AddSemanticElement(parentHandle, child.Visual.Handle, index, child.Visual.Size.X, child.Visual.Size.Y, totalOffset.X, totalOffset.Y, role, automationId, IsAccessibilityFocusable(child, child.IsFocusable), ariaChecked, child.Visual.IsVisible, horizontallyScrollable, verticallyScrollable, child.GetType().Name);
	}

	private void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle)
	{
		NativeMethods.RemoveSemanticElement(parentHandle, childHandle);
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

	private void OnAutomationNameChanged(UIElement element, string automationId)
	{
		Debug.Assert(IsAccessibilityEnabled);
		NativeMethods.UpdateAriaLabel(element.Visual.Handle, automationId);
	}

	public void AnnouncePolite(string text)
		=> NativeMethods.AnnouncePolite(text);

	public void AnnounceAssertive(string text)
		=> NativeMethods.AnnounceAssertive(text);

	public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty &&
			TryGetPeerOwner(peer, out var element))
		{
			var ariaChecked = ConvertToAriaChecked((ToggleState)newValue);
			NativeMethods.UpdateAriaChecked(element.Visual.Handle, ariaChecked);
		}
		else if (automationProperty == AutomationElementIdentifiers.NameProperty &&
			TryGetPeerOwner(peer, out element))
		{
			OnAutomationNameChanged(element, (string)newValue);
		}
		else if ((automationProperty == ScrollPatternIdentifiers.HorizontalScrollPercentProperty ||
			automationProperty == ScrollPatternIdentifiers.VerticalScrollPercentProperty) &&
			TryGetPeerOwner(peer, out element) && element is ScrollViewer { Presenter: { } presenter } sv)
		{
			NativeMethods.UpdateNativeScrollOffsets(presenter.Visual.Handle, sv.HorizontalOffset, sv.VerticalOffset);
		}
	}

	public bool ListenerExistsHelper(AutomationEvents eventId)
		=> IsAccessibilityEnabled;

	private static bool TryGetPeerOwner(AutomationPeer peer, [NotNullWhen(true)] out UIElement? owner)
	{
		if (peer is FrameworkElementAutomationPeer { Owner: { } element })
		{
			owner = element;
			return true;
		}

		owner = null;
		return false;
	}

	// TODO (DOTI): Added with macOS automation, maybe won't be needed for wasm
	public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => throw new NotImplementedException();
	public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) => throw new NotImplementedException();

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.addRootElementToSemanticsRoot")]
		internal static partial void AddRootElementToSemanticsRoot(IntPtr rootHandle, float width, float height, float x, float y, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.addSemanticElement")]
		internal static partial bool AddSemanticElement(IntPtr parentHandle, IntPtr handle, int? index, float width, float height, float x, float y, string role, string automationId, bool isFocusable, string? ariaChecked, bool isVisible, bool horizontallyScrollable, bool verticallyScrollable, string temporary);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.removeSemanticElement")]
		internal static partial void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLabel")]
		internal static partial void UpdateAriaLabel(IntPtr handle, string automationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaChecked")]
		internal static partial void UpdateAriaChecked(IntPtr handle, string? ariaChecked);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateNativeScrollOffsets")]
		internal static partial void UpdateNativeScrollOffsets(IntPtr handle, double horizontalOffset, double verticalOffset);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateSemanticElementPositioning")]
		internal static partial void UpdateSemanticElementPositioning(IntPtr handle, float width, float height, float x, float y);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateIsFocusable")]
		internal static partial void UpdateIsFocusable(IntPtr handle, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.hideSemanticElement")]
		internal static partial void HideSemanticElement(IntPtr handle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.announcePolite")]
		internal static partial void AnnouncePolite(string text);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.announceAssertive")]
		internal static partial void AnnounceAssertive(string text);
	}
}
