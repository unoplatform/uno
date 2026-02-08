#nullable enable

using System;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using System.Globalization;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using UIKit;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.AppleUIKit.Accessibility;

/// <summary>
/// A UIAccessibilityElement that wraps an Uno AutomationPeer to expose it to VoiceOver.
/// </summary>
internal sealed class UnoAccessibilityElement : UIAccessibilityElement
{
	private readonly WeakReference<AutomationPeer> _peerRef;

	public UnoAccessibilityElement(NSObject container, AutomationPeer peer)
		: base(container)
	{
		_peerRef = new WeakReference<AutomationPeer>(peer);
		UpdateFromPeer();
	}

	/// <summary>
	/// Updates all accessibility properties from the associated AutomationPeer.
	/// </summary>
	public void UpdateFromPeer()
	{
		if (!_peerRef.TryGetTarget(out var peer))
		{
			return;
		}

		try
		{
			// Core properties
			AccessibilityLabel = peer.GetName() ?? string.Empty;
			AccessibilityHint = peer.GetHelpText() ?? string.Empty;
			AccessibilityIdentifier = peer.GetAutomationId() ?? string.Empty;

			// Control type -> Traits
			var traits = (ulong)MapControlTypeToTraits(peer.GetAutomationControlType());

			// State-based traits
			if (!peer.IsEnabled())
			{
				traits |= (ulong)UIAccessibilityTrait.NotEnabled;
			}

			if (peer.GetHeadingLevel() != AutomationHeadingLevel.None)
			{
				traits |= (ulong)UIAccessibilityTrait.Header;
			}

			// Selected state for toggleable/selectable items
			if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
			{
				if (toggleProvider.ToggleState == ToggleState.On)
				{
					traits |= (ulong)UIAccessibilityTrait.Selected;
				}
			}
			else if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionProvider)
			{
				if (selectionProvider.IsSelected)
				{
					traits |= (ulong)UIAccessibilityTrait.Selected;
				}
			}

			AccessibilityTraits = traits;

			// Value for range controls (sliders, progress bars)
			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				var value = rangeProvider.Value;
				var min = rangeProvider.Minimum;
				var max = rangeProvider.Maximum;

				// Format as percentage or raw value
				if (max > min)
				{
					var percentage = (value - min) / (max - min) * 100;
					AccessibilityValue = string.Format(CultureInfo.InvariantCulture, "{0:F0}%", percentage);
				}
				else
				{
					AccessibilityValue = value.ToString("F1", CultureInfo.InvariantCulture);
				}
			}
			else if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
			{
				AccessibilityValue = valueProvider.Value;
			}
			else
			{
				AccessibilityValue = null;
			}

			// Bounding rectangle - convert to screen coordinates
			UpdateAccessibilityFrame(peer);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Failed to update accessibility element from peer: {ex.Message}");
			}
		}
	}

	private void UpdateAccessibilityFrame(AutomationPeer peer)
	{
		var rect = peer.GetBoundingRectangle();
		if (!rect.IsEmpty)
		{
			// The bounding rectangle from GetBoundingRectangle is already in screen coordinates
			// but may need scaling for the device's screen scale
			var scale = UIScreen.MainScreen.Scale;
			AccessibilityFrame = new CGRect(
				rect.X * scale,
				rect.Y * scale,
				rect.Width * scale,
				rect.Height * scale
			);
		}
	}

	/// <summary>
	/// Called when VoiceOver user double-taps to activate the element.
	/// </summary>
	[Export("accessibilityActivate")]
	public bool AccessibilityActivate()
	{
		if (!_peerRef.TryGetTarget(out var peer))
		{
			return false;
		}

		try
		{
			// Try invoke pattern first (buttons, menu items)
			if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoker)
			{
				invoker.Invoke();
				return true;
			}

			// Try toggle pattern (checkboxes, toggle buttons)
			if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggler)
			{
				toggler.Toggle();
				UpdateFromPeer(); // Update traits to reflect new state
				return true;
			}

			// Try selection item pattern (list items)
			if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selector)
			{
				selector.Select();
				UpdateFromPeer();
				return true;
			}

			// Try expand/collapse pattern
			if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expander)
			{
				if (expander.ExpandCollapseState == ExpandCollapseState.Collapsed)
				{
					expander.Expand();
				}
				else
				{
					expander.Collapse();
				}
				return true;
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Failed to activate accessibility element: {ex.Message}");
			}
		}

		return false;
	}

	/// <summary>
	/// Called when VoiceOver user swipes up on an adjustable element.
	/// </summary>
	[Export("accessibilityIncrement")]
	public new void AccessibilityIncrement()
	{
		if (!_peerRef.TryGetTarget(out var peer))
		{
			return;
		}

		try
		{
			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider provider && !provider.IsReadOnly)
			{
				var newValue = Math.Min(provider.Value + provider.SmallChange, provider.Maximum);
				provider.SetValue(newValue);
				UpdateFromPeer();
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Failed to increment accessibility element: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Called when VoiceOver user swipes down on an adjustable element.
	/// </summary>
	[Export("accessibilityDecrement")]
	public new void AccessibilityDecrement()
	{
		if (!_peerRef.TryGetTarget(out var peer))
		{
			return;
		}

		try
		{
			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider provider && !provider.IsReadOnly)
			{
				var newValue = Math.Max(provider.Value - provider.SmallChange, provider.Minimum);
				provider.SetValue(newValue);
				UpdateFromPeer();
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Failed to decrement accessibility element: {ex.Message}");
			}
		}
	}

	/// <summary>
	/// Called when VoiceOver user performs escape gesture (two-finger Z).
	/// </summary>
	[Export("accessibilityPerformEscape")]
	public new bool AccessibilityPerformEscape()
	{
		// This could be used to dismiss popups or go back
		// For now, let the system handle it
		return false;
	}

	/// <summary>
	/// Called when VoiceOver user performs scroll gesture.
	/// </summary>
	[Export("accessibilityScroll:")]
	public new bool AccessibilityScroll(UIAccessibilityScrollDirection direction)
	{
		if (!_peerRef.TryGetTarget(out var peer))
		{
			return false;
		}

		try
		{
			if (peer.GetPattern(PatternInterface.Scroll) is IScrollProvider scrollProvider)
			{
				var horizontalAmount = ScrollAmount.NoAmount;
				var verticalAmount = ScrollAmount.NoAmount;

				switch (direction)
				{
					case UIAccessibilityScrollDirection.Up:
						verticalAmount = ScrollAmount.SmallDecrement;
						break;
					case UIAccessibilityScrollDirection.Down:
						verticalAmount = ScrollAmount.SmallIncrement;
						break;
					case UIAccessibilityScrollDirection.Left:
						horizontalAmount = ScrollAmount.SmallDecrement;
						break;
					case UIAccessibilityScrollDirection.Right:
						horizontalAmount = ScrollAmount.SmallIncrement;
						break;
				}

				if (horizontalAmount != ScrollAmount.NoAmount || verticalAmount != ScrollAmount.NoAmount)
				{
					scrollProvider.Scroll(horizontalAmount, verticalAmount);
					return true;
				}
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Failed to scroll accessibility element: {ex.Message}");
			}
		}

		return false;
	}

	private static UIAccessibilityTrait MapControlTypeToTraits(AutomationControlType type)
	{
		return type switch
		{
			AutomationControlType.Button => UIAccessibilityTrait.Button,
			AutomationControlType.CheckBox => UIAccessibilityTrait.Button,
			AutomationControlType.RadioButton => UIAccessibilityTrait.Button,
			AutomationControlType.ComboBox => UIAccessibilityTrait.Button,
			AutomationControlType.Image => UIAccessibilityTrait.Image,
			AutomationControlType.Text => UIAccessibilityTrait.StaticText,
			AutomationControlType.Hyperlink => UIAccessibilityTrait.Link,
			AutomationControlType.Header => UIAccessibilityTrait.Header,
			AutomationControlType.Slider => UIAccessibilityTrait.Adjustable,
			AutomationControlType.ProgressBar => UIAccessibilityTrait.UpdatesFrequently,
			AutomationControlType.TabItem => UIAccessibilityTrait.Button,
			AutomationControlType.MenuItem => UIAccessibilityTrait.Button,
			AutomationControlType.ListItem => UIAccessibilityTrait.None, // Will get Selected trait if selected
			AutomationControlType.TreeItem => UIAccessibilityTrait.None,
			_ => UIAccessibilityTrait.None,
		};
	}
}
