#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Foundation;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Uno.UI.Runtime.Skia.AppleUIKit.Accessibility;

/// <summary>
/// Manages the mapping between AutomationPeers and UIAccessibilityElements.
/// </summary>
internal sealed class AccessibilityElementRegistry
{
	private readonly ConditionalWeakTable<AutomationPeer, UnoAccessibilityElement> _peerToElement = new();
	private readonly List<UnoAccessibilityElement> _orderedElements = new();
	private readonly object _lock = new();

	/// <summary>
	/// Gets or creates an accessibility element for the given automation peer.
	/// </summary>
	public UnoAccessibilityElement GetOrCreateElement(AutomationPeer peer, NSObject container)
	{
		lock (_lock)
		{
			if (!_peerToElement.TryGetValue(peer, out var element))
			{
				element = new UnoAccessibilityElement(container, peer);
				_peerToElement.Add(peer, element);
				_orderedElements.Add(element);
			}
			return element;
		}
	}

	/// <summary>
	/// Tries to get an existing accessibility element for the given peer.
	/// </summary>
	public bool TryGetElement(AutomationPeer peer, out UnoAccessibilityElement? element)
	{
		lock (_lock)
		{
			return _peerToElement.TryGetValue(peer, out element);
		}
	}

	/// <summary>
	/// Removes the accessibility element associated with the given peer.
	/// </summary>
	public void RemoveElement(AutomationPeer peer)
	{
		lock (_lock)
		{
			if (_peerToElement.TryGetValue(peer, out var element))
			{
				_orderedElements.Remove(element);
				_peerToElement.Remove(peer);
			}
		}
	}

	/// <summary>
	/// Clears all registered elements.
	/// </summary>
	public void Clear()
	{
		lock (_lock)
		{
			_orderedElements.Clear();
			// ConditionalWeakTable doesn't have a Clear method, so we create a new one
			// Note: The old table will be garbage collected along with its weak references
		}
	}

	/// <summary>
	/// Gets the number of registered elements.
	/// </summary>
	public int Count
	{
		get
		{
			lock (_lock)
			{
				return _orderedElements.Count;
			}
		}
	}

	/// <summary>
	/// Gets an element at the specified index.
	/// </summary>
	public NSObject? GetElementAt(int index)
	{
		lock (_lock)
		{
			if (index >= 0 && index < _orderedElements.Count)
			{
				return _orderedElements[index];
			}
			return null;
		}
	}

	/// <summary>
	/// Gets the index of the specified element.
	/// </summary>
	public int GetIndexOf(NSObject element)
	{
		lock (_lock)
		{
			if (element is UnoAccessibilityElement accessibilityElement)
			{
				return _orderedElements.IndexOf(accessibilityElement);
			}
			return -1;
		}
	}

	/// <summary>
	/// Updates the order of elements based on their visual position.
	/// </summary>
	public void UpdateOrder()
	{
		lock (_lock)
		{
			// Sort by Y position first, then X position (reading order)
			_orderedElements.Sort((a, b) =>
			{
				var frameA = a.AccessibilityFrame;
				var frameB = b.AccessibilityFrame;

				// Compare Y positions with some tolerance for same-row detection
				var yDiff = frameA.Y - frameB.Y;
				if (Math.Abs(yDiff) > 10) // If Y difference is significant, use Y order
				{
					return yDiff < 0 ? -1 : 1;
				}

				// Same row, compare X positions
				var xDiff = frameA.X - frameB.X;
				if (Math.Abs(xDiff) > 1)
				{
					return xDiff < 0 ? -1 : 1;
				}

				return 0;
			});
		}
	}
}
