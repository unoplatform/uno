#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.Foundation.Logging;
using Uno.Helpers;

namespace Uno.UI.Runtime.Skia.MacOS.Accessibility;

/// <summary>
/// Bridge between Uno's AutomationPeer system and macOS NSAccessibility.
/// Implements IAutomationPeerListener to receive automation events and
/// IUnoAccessibility to provide screen reader announcements.
/// </summary>
internal sealed unsafe class MacOSAccessibilityBridge : IAutomationPeerListener, IUnoAccessibility
{
	private static readonly Lazy<MacOSAccessibilityBridge> _instance = new(() => new MacOSAccessibilityBridge());
	private readonly Dictionary<int, WeakReference<AutomationPeer>> _elementIdToPeer = new();
	private readonly ConditionalWeakTable<AutomationPeer, int?> _peerToElementId = new();
	private readonly List<AutomationPeer> _orderedPeers = new();
	private readonly object _lock = new();
	private int _nextElementId = 1;
	private bool _isInitialized;

	// String buffers for native interop (to avoid repeated allocations)
	private byte[]? _labelBuffer;
	private byte[]? _hintBuffer;
	private byte[]? _valueBuffer;
	private byte[]? _roleBuffer;

	internal static MacOSAccessibilityBridge Instance => _instance.Value;

	private MacOSAccessibilityBridge()
	{
	}

	/// <inheritdoc />
	public bool IsAccessibilityEnabled => NativeUno.uno_accessibility_is_voiceover_running();

	/// <summary>
	/// Initializes the accessibility bridge by registering callbacks with native code.
	/// </summary>
	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace("Initializing MacOSAccessibilityBridge");
		}

		// Register native callbacks
		NativeUno.uno_set_accessibility_callbacks(
			&GetChildCount,
			&GetChildData,
			&HitTest,
			&GetFocusedElement,
			&PerformAction,
			&ElementFreed);

		// Register as the automation peer listener
		AutomationPeer.AutomationPeerListener = this;

		// Register for accessibility announcements
		AccessibilityAnnouncer.AccessibilityImpl = this;

		// Hook into UIElement tree changes
		UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAdded;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemoved;

		_isInitialized = true;

		// If VoiceOver is already running, build the tree
		if (IsAccessibilityEnabled)
		{
			RebuildAccessibilityTree();
		}
	}

	private void RebuildAccessibilityTree()
	{
		var window = Window.CurrentSafe;
		if (window?.RootElement is null)
		{
			return;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug("Rebuilding accessibility tree");
		}

		lock (_lock)
		{
			_elementIdToPeer.Clear();
			_peerToElementId.Clear();
			_orderedPeers.Clear();
			_nextElementId = 1;

			BuildAccessibilityTreeRecursive(window.RootElement);
		}
	}

	private void BuildAccessibilityTreeRecursive(UIElement element)
	{
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

		var peer = element.GetOrCreateAutomationPeer();
		if (peer is not null)
		{
			var name = peer.GetName();
			var controlType = peer.GetAutomationControlType();

			if (!string.IsNullOrEmpty(name) || IsInteractiveControlType(controlType))
			{
				RegisterPeer(peer);
			}
		}

		foreach (var child in element._children)
		{
			BuildAccessibilityTreeRecursive(child);
		}
	}

	private int RegisterPeer(AutomationPeer peer)
	{
		lock (_lock)
		{
			if (_peerToElementId.TryGetValue(peer, out var existingId) && existingId.HasValue)
			{
				return existingId.Value;
			}

			var elementId = _nextElementId++;
			_elementIdToPeer[elementId] = new WeakReference<AutomationPeer>(peer);
			_peerToElementId.AddOrUpdate(peer, elementId);
			_orderedPeers.Add(peer);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"Registered peer with ID {elementId}: Name='{peer.GetName()}', Type={peer.GetAutomationControlType()}");
			}

			return elementId;
		}
	}

	private void UnregisterPeer(AutomationPeer peer)
	{
		lock (_lock)
		{
			if (_peerToElementId.TryGetValue(peer, out var elementId) && elementId.HasValue)
			{
				_elementIdToPeer.Remove(elementId.Value);
				_peerToElementId.Remove(peer);
				_orderedPeers.Remove(peer);
			}
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
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		AddElementToTree(child);
		InvalidateNativeAccessibility();
	}

	private void AddElementToTree(UIElement element)
	{
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
					RegisterPeer(peer);
				}
			}
		}

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
		InvalidateNativeAccessibility();
	}

	private void RemoveElementFromTree(UIElement element)
	{
		var peer = element.GetOrCreateAutomationPeer();
		if (peer is not null)
		{
			UnregisterPeer(peer);
		}

		foreach (var child in element._children)
		{
			RemoveElementFromTree(child);
		}
	}

	private void InvalidateNativeAccessibility()
	{
		var handle = GetCurrentWindowHandle();
		if (handle != nint.Zero)
		{
			NativeUno.uno_accessibility_invalidate(handle);
		}
	}

	private static nint GetCurrentWindowHandle()
	{
		var window = Window.CurrentSafe;
		if (window is not null)
		{
			var wrapper = window.NativeWindowWrapper;
			if (wrapper?.NativeWindow is MacOSWindowNative nativeWindow)
			{
				return nativeWindow.Handle;
			}
		}
		return nint.Zero;
	}

	#region Native Callbacks

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int GetChildCount(nint windowHandle)
	{
		try
		{
			lock (Instance._lock)
			{
				return Instance._orderedPeers.Count;
			}
		}
		catch (Exception ex)
		{
			Instance.Log().Error($"GetChildCount failed: {ex.Message}");
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int GetChildData(nint windowHandle, int index, NativeAccessibilityElementData* data)
	{
		try
		{
			AutomationPeer? peer;
			int elementId;

			lock (Instance._lock)
			{
				if (index < 0 || index >= Instance._orderedPeers.Count)
				{
					return 0; // false
				}

				peer = Instance._orderedPeers[index];
				if (!Instance._peerToElementId.TryGetValue(peer, out var id) || !id.HasValue)
				{
					return 0;
				}
				elementId = id.Value;
			}

			// Fill in the data
			data->ElementId = elementId;
			data->ParentId = -1; // Flat list for now

			// Bounding rectangle
			var rect = peer.GetBoundingRectangle();
			data->FrameX = rect.X;
			data->FrameY = rect.Y;
			data->FrameWidth = rect.Width;
			data->FrameHeight = rect.Height;

			// Strings - we need to keep them alive until native code copies them
			var label = peer.GetName() ?? string.Empty;
			var hint = peer.GetHelpText() ?? string.Empty;
			var role = MapControlTypeToRole(peer.GetAutomationControlType());

			string? value = null;
			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				value = rangeProvider.Value.ToString("F1");
			}
			else if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
			{
				value = valueProvider.Value;
			}

			// Use instance buffers to avoid allocations
			data->Label = WriteUtf8String(label, ref Instance._labelBuffer);
			data->Hint = WriteUtf8String(hint, ref Instance._hintBuffer);
			data->Value = WriteUtf8String(value ?? string.Empty, ref Instance._valueBuffer);
			data->Role = WriteUtf8String(role, ref Instance._roleBuffer);

			// State
			data->IsEnabled = peer.IsEnabled();
			data->IsFocusable = peer.IsKeyboardFocusable();
			data->IsSelected = GetIsSelected(peer);
			data->IsExpanded = GetIsExpanded(peer);

			return 1; // true
		}
		catch (Exception ex)
		{
			Instance.Log().Error($"GetChildData failed: {ex.Message}");
			return 0;
		}
	}

	private static nint WriteUtf8String(string str, ref byte[]? buffer)
	{
		if (string.IsNullOrEmpty(str))
		{
			return nint.Zero;
		}

		var byteCount = Encoding.UTF8.GetByteCount(str) + 1; // +1 for null terminator
		if (buffer is null || buffer.Length < byteCount)
		{
			buffer = new byte[byteCount];
		}

		Encoding.UTF8.GetBytes(str, 0, str.Length, buffer, 0);
		buffer[byteCount - 1] = 0; // Null terminator

		// Pin and return pointer - native code must copy immediately
		// This is safe because the buffer is kept alive by the instance
		fixed (byte* ptr = buffer)
		{
			return (nint)ptr;
		}
	}

	private static string MapControlTypeToRole(AutomationControlType type)
	{
		return type switch
		{
			AutomationControlType.Button => "button",
			AutomationControlType.CheckBox => "checkbox",
			AutomationControlType.RadioButton => "radiobutton",
			AutomationControlType.Slider => "slider",
			AutomationControlType.ProgressBar => "progressbar",
			AutomationControlType.Text => "text",
			AutomationControlType.Edit => "textfield",
			AutomationControlType.Hyperlink => "link",
			AutomationControlType.Image => "image",
			AutomationControlType.List => "list",
			AutomationControlType.ListItem => "listitem",
			AutomationControlType.ComboBox => "combobox",
			AutomationControlType.Tab => "tablist",
			AutomationControlType.TabItem => "tab",
			AutomationControlType.Group => "group",
			_ => "group",
		};
	}

	private static bool GetIsSelected(AutomationPeer peer)
	{
		if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle)
		{
			return toggle.ToggleState == ToggleState.On;
		}
		if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selection)
		{
			return selection.IsSelected;
		}
		return false;
	}

	private static bool GetIsExpanded(AutomationPeer peer)
	{
		if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expander)
		{
			return expander.ExpandCollapseState == ExpandCollapseState.Expanded;
		}
		return false;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int HitTest(nint windowHandle, double x, double y)
	{
		try
		{
			lock (Instance._lock)
			{
				// Simple linear search - could be optimized with spatial data structure
				foreach (var peer in Instance._orderedPeers)
				{
					var rect = peer.GetBoundingRectangle();
					if (x >= rect.X && x <= rect.X + rect.Width &&
						y >= rect.Y && y <= rect.Y + rect.Height)
					{
						if (Instance._peerToElementId.TryGetValue(peer, out var id) && id.HasValue)
						{
							return id.Value;
						}
					}
				}
			}
			return -1; // Not found
		}
		catch (Exception ex)
		{
			Instance.Log().Error($"HitTest failed: {ex.Message}");
			return -1;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int GetFocusedElement(nint windowHandle)
	{
		try
		{
			// Get the currently focused element
			var window = Window.CurrentSafe;
			if (window?.RootElement is not null)
			{
				var focusedElement = Microsoft.UI.Xaml.Input.FocusManager.GetFocusedElement(window.RootElement.XamlRoot);
				if (focusedElement is UIElement uiElement)
				{
					var peer = uiElement.GetOrCreateAutomationPeer();
					if (peer is not null)
					{
						lock (Instance._lock)
						{
							if (Instance._peerToElementId.TryGetValue(peer, out var id) && id.HasValue)
							{
								return id.Value;
							}
						}
					}
				}
			}
			return -1;
		}
		catch (Exception ex)
		{
			Instance.Log().Error($"GetFocusedElement failed: {ex.Message}");
			return -1;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static int PerformAction(nint windowHandle, int elementId, sbyte* actionPtr)
	{
		try
		{
			AutomationPeer? peer = null;
			lock (Instance._lock)
			{
				if (Instance._elementIdToPeer.TryGetValue(elementId, out var weakRef))
				{
					weakRef.TryGetTarget(out peer);
				}
			}

			if (peer is null)
			{
				return 0;
			}

			var action = Marshal.PtrToStringUTF8((nint)actionPtr) ?? string.Empty;

			switch (action)
			{
				case "press":
					if (peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoker)
					{
						invoker.Invoke();
						return 1;
					}
					if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggler)
					{
						toggler.Toggle();
						return 1;
					}
					if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selector)
					{
						selector.Select();
						return 1;
					}
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
						return 1;
					}
					break;

				case "increment":
					if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider incProvider && !incProvider.IsReadOnly)
					{
						var newValue = Math.Min(incProvider.Value + incProvider.SmallChange, incProvider.Maximum);
						incProvider.SetValue(newValue);
						return 1;
					}
					break;

				case "decrement":
					if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider decProvider && !decProvider.IsReadOnly)
					{
						var newValue = Math.Max(decProvider.Value - decProvider.SmallChange, decProvider.Minimum);
						decProvider.SetValue(newValue);
						return 1;
					}
					break;
			}

			return 0;
		}
		catch (Exception ex)
		{
			Instance.Log().Error($"PerformAction failed: {ex.Message}");
			return 0;
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void ElementFreed(nint windowHandle, int elementId)
	{
		try
		{
			lock (Instance._lock)
			{
				if (Instance._elementIdToPeer.TryGetValue(elementId, out var weakRef))
				{
					if (weakRef.TryGetTarget(out var peer))
					{
						Instance._peerToElementId.Remove(peer);
						Instance._orderedPeers.Remove(peer);
					}
					Instance._elementIdToPeer.Remove(elementId);
				}
			}
		}
		catch (Exception ex)
		{
			Instance.Log().Error($"ElementFreed failed: {ex.Message}");
		}
	}

	#endregion

	#region IAutomationPeerListener Implementation

	/// <inheritdoc />
	public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		InvalidateNativeAccessibility();
	}

	/// <inheritdoc />
	public bool ListenerExistsHelper(AutomationEvents eventId)
	{
		return IsAccessibilityEnabled;
	}

	#endregion

	#region IUnoAccessibility Implementation

	/// <inheritdoc />
	public void AnnouncePolite(string text)
	{
		if (!string.IsNullOrEmpty(text) && IsAccessibilityEnabled)
		{
			// macOS doesn't have a direct equivalent, but we can post an announcement
			// through the accessibility system
			var handle = GetCurrentWindowHandle();
			if (handle != nint.Zero)
			{
				NativeUno.uno_accessibility_post_notification(handle, -1, "Announcement");
			}
		}
	}

	/// <inheritdoc />
	public void AnnounceAssertive(string text)
	{
		// Same as polite for now
		AnnouncePolite(text);
	}

	#endregion
}
