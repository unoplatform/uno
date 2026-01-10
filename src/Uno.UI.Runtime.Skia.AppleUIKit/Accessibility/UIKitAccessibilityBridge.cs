#nullable enable

using System;
using System.Collections.Generic;
using Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using UIKit;
using Uno.Foundation.Logging;
using Uno.Helpers;

namespace Uno.UI.Runtime.Skia.AppleUIKit.Accessibility;

/// <summary>
/// Bridge between Uno's AutomationPeer system and iOS UIAccessibility.
/// Implements IAutomationPeerListener to receive automation events and
/// IUnoAccessibility to provide screen reader announcements.
/// </summary>
internal sealed class UIKitAccessibilityBridge : IAutomationPeerListener, IUnoAccessibility
{
	private static readonly Lazy<UIKitAccessibilityBridge> _instance = new(() => new UIKitAccessibilityBridge());
	private readonly AccessibilityElementRegistry _registry = new();
	private readonly HashSet<UIElement> _trackedElements = new();
	private NSObject? _container;
	private bool _isInitialized;

	internal static UIKitAccessibilityBridge Instance => _instance.Value;

	private UIKitAccessibilityBridge()
	{
	}

	/// <summary>
	/// Gets or sets the container object for accessibility elements.
	/// </summary>
	public NSObject? Container
	{
		get => _container;
		set
		{
			if (_container != value)
			{
				_container = value;
				if (_isInitialized && IsAccessibilityEnabled)
				{
					RebuildAccessibilityTree();
				}
			}
		}
	}

	/// <inheritdoc />
	public bool IsAccessibilityEnabled => UIAccessibility.IsVoiceOverRunning;

	/// <summary>
	/// Initializes the accessibility bridge by registering as the automation peer listener.
	/// </summary>
	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("Initializing UIKitAccessibilityBridge");
		}

		// Register as the automation peer listener
		AutomationPeer.AutomationPeerListener = this;

		// Register for accessibility announcements
		AccessibilityAnnouncer.AccessibilityImpl = this;

		// Hook into UIElement tree changes
		UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAdded;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemoved;

		// Hook into visual position/size changes
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = OnVisualOffsetOrSizeChanged;

		// Subscribe to VoiceOver status changes
		NSNotificationCenter.DefaultCenter.AddObserver(
			UIView.VoiceOverStatusDidChangeNotification,
			OnVoiceOverStatusChanged);

		_isInitialized = true;

		// If VoiceOver is already running, build the tree
		if (IsAccessibilityEnabled)
		{
			RebuildAccessibilityTree();
		}
	}

	private void OnVoiceOverStatusChanged(NSNotification notification)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"VoiceOver status changed: IsVoiceOverRunning={UIAccessibility.IsVoiceOverRunning}");
		}

		if (UIAccessibility.IsVoiceOverRunning)
		{
			RebuildAccessibilityTree();
		}
	}

	private void RebuildAccessibilityTree()
	{
		if (_container is null)
		{
			return;
		}

		var rootElement = Microsoft.UI.Xaml.Window.CurrentSafe?.RootElement;
		if (rootElement is null)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Rebuilding accessibility tree");
		}

		_registry.Clear();
		_trackedElements.Clear();
		BuildAccessibilityTreeRecursive(rootElement);
		_registry.UpdateOrder();

		// Notify VoiceOver that the screen has changed
		UIAccessibility.PostNotification(UIAccessibilityPostNotification.ScreenChanged, null);
	}

	private void BuildAccessibilityTreeRecursive(UIElement element)
	{
		if (_container is null)
		{
			return;
		}

		// Check if element should be skipped
		var accessibilityView = AutomationProperties.GetAccessibilityView(element);
		if (accessibilityView == AccessibilityView.Raw)
		{
			// Still process children
			foreach (var child in element._children)
			{
				BuildAccessibilityTreeRecursive(child);
			}
			return;
		}

		// Try to get or create an automation peer
		var peer = element.GetOrCreateAutomationPeer();
		if (peer is not null)
		{
			// Check if element has meaningful accessibility info
			var name = peer.GetName();
			var controlType = peer.GetAutomationControlType();

			// Only add elements that have a name or are interactive controls
			if (!string.IsNullOrEmpty(name) || IsInteractiveControlType(controlType))
			{
				_registry.GetOrCreateElement(peer, _container);
				_trackedElements.Add(element);

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"Added accessibility element: Name='{name}', Type={controlType}");
				}
			}
		}

		// Process children
		foreach (var child in element._children)
		{
			BuildAccessibilityTreeRecursive(child);
		}
	}

	private static bool IsInteractiveControlType(AutomationControlType type)
	{
		return type switch
		{
			AutomationControlType.Button => true,
			AutomationControlType.CheckBox => true,
			AutomationControlType.ComboBox => true,
			AutomationControlType.Edit => true,
			AutomationControlType.Hyperlink => true,
			AutomationControlType.ListItem => true,
			AutomationControlType.MenuItem => true,
			AutomationControlType.RadioButton => true,
			AutomationControlType.Slider => true,
			AutomationControlType.Tab => true,
			AutomationControlType.TabItem => true,
			AutomationControlType.TreeItem => true,
			_ => false,
		};
	}

	private void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (!IsAccessibilityEnabled || _container is null)
		{
			return;
		}

		AddElementToTree(child);
	}

	private void AddElementToTree(UIElement element)
	{
		if (_container is null)
		{
			return;
		}

		var accessibilityView = AutomationProperties.GetAccessibilityView(element);
		if (accessibilityView != AccessibilityView.Raw)
		{
			var peer = element.GetOrCreateAutomationPeer();
			if (peer is not null)
			{
				var name = peer.GetName();
				var controlType = peer.GetAutomationControlType();

				if (!string.IsNullOrEmpty(name) || IsInteractiveControlType(controlType))
				{
					_registry.GetOrCreateElement(peer, _container);
					_trackedElements.Add(element);

					// Update order and notify VoiceOver
					_registry.UpdateOrder();
					UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, null);
				}
			}
		}

		// Process children
		foreach (var child in element._children)
		{
			AddElementToTree(child);
		}
	}

	private void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		RemoveElementFromTree(child);
	}

	private void RemoveElementFromTree(UIElement element)
	{
		var peer = element.GetOrCreateAutomationPeer();
		if (peer is not null)
		{
			_registry.RemoveElement(peer);
			_trackedElements.Remove(element);
		}

		// Remove children recursively
		foreach (var child in element._children)
		{
			RemoveElementFromTree(child);
		}

		// Notify VoiceOver
		UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, null);
	}

	private void OnVisualOffsetOrSizeChanged(Visual visual)
	{
		if (!IsAccessibilityEnabled || _container is null)
		{
			return;
		}

		// Find the UIElement that owns this visual and update its accessibility frame
		if (visual is ShapeVisual shapeVisual && shapeVisual.Owner?.Target is UIElement element)
		{
			var peer = element.GetOrCreateAutomationPeer();
			if (peer is not null && _registry.TryGetElement(peer, out var accessibilityElement) && accessibilityElement is not null)
			{
				accessibilityElement.UpdateFromPeer();
			}
		}
	}

	#region IAutomationPeerListener Implementation

	/// <inheritdoc />
	public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		if (_registry.TryGetElement(peer, out var element) && element is not null)
		{
			element.UpdateFromPeer();

			// Determine the appropriate notification based on the property that changed
			if (automationProperty == AutomationElementIdentifiers.NameProperty)
			{
				// Name changed - announce the new name
				UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, element);
			}
			else if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty)
			{
				// Toggle state changed - announce the element
				UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, element.AccessibilityLabel);
			}
			else
			{
				// Generic property change
				UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, element);
			}
		}
	}

	/// <inheritdoc />
	public bool ListenerExistsHelper(AutomationEvents eventId)
	{
		// Return true if VoiceOver is running to enable automation event processing
		return IsAccessibilityEnabled;
	}

	#endregion

	#region IUnoAccessibility Implementation

	/// <inheritdoc />
	public void AnnouncePolite(string text)
	{
		if (!string.IsNullOrEmpty(text) && IsAccessibilityEnabled)
		{
			UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, new NSString(text));
		}
	}

	/// <inheritdoc />
	public void AnnounceAssertive(string text)
	{
		// iOS doesn't have a direct equivalent of assertive announcements
		// We use the same announcement but could potentially use a different approach
		if (!string.IsNullOrEmpty(text) && IsAccessibilityEnabled)
		{
			UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, new NSString(text));
		}
	}

	#endregion

	#region Accessibility Container Support

	/// <summary>
	/// Gets the number of accessibility elements.
	/// </summary>
	public nint ElementCount => _registry.Count;

	/// <summary>
	/// Gets an accessibility element at the specified index.
	/// </summary>
	public NSObject? GetElement(int index) => _registry.GetElementAt(index);

	/// <summary>
	/// Gets the index of an accessibility element.
	/// </summary>
	public nint GetIndexOf(NSObject element) => _registry.GetIndexOf(element);

	#endregion
}
