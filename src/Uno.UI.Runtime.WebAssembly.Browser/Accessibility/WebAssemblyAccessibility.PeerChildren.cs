#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime;

// UIA builds the automation tree from AutomationPeer.GetChildren() (MUX Reference AutomationPeer_Partial.cpp,
// GetChildrenImpl / NavigateCoreImpl), so a peer decides which elements below it are exposed. The semantic DOM is
// built from the visual tree; these helpers keep that walk to the children each peer reports.
internal partial class WebAssemblyAccessibility
{
	// Peer-bearing elements left out because the peer above them does not report them (their subtree is left out too).
	// Decided when the element is walked and dropped when it is removed. A peer whose GetChildren() result changes
	// without a tree change isn't re-evaluated.
	private readonly HashSet<IntPtr> _peerExcludedHandles = new();

	/// <summary>
	/// The automation children a peer reports, keyed by the element hosting each of them in the visual tree.
	/// </summary>
	private sealed class PeerChildrenScope
	{
		private readonly Dictionary<UIElement, AutomationPeer> _children;

		public PeerChildrenScope(Dictionary<UIElement, AutomationPeer> children) => _children = children;

		public bool TryGetChildPeer(UIElement element, out AutomationPeer? peer) => _children.TryGetValue(element, out peer);
	}

	/// <summary>
	/// Returns the scope of <paramref name="peer"/>'s children, or null when they can't all be placed in the visual
	/// tree (e.g. virtual peers), in which case the visual walk is kept as is.
	/// </summary>
	private PeerChildrenScope? CreatePeerChildrenScope(AutomationPeer peer)
	{
		IList<AutomationPeer>? children;
		try
		{
			children = peer.GetChildren();
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] GetChildren failed for {peer.GetType().Name}; exposing its visual subtree.", ex);
			}

			return null;
		}

		var map = new Dictionary<UIElement, AutomationPeer>(ReferenceEqualityComparer.Instance);
		if (children is not null)
		{
			foreach (var child in children)
			{
				var owner = child switch
				{
					FrameworkElementAutomationPeer { Owner: { } elementOwner } => elementOwner,
					ItemAutomationPeer itemPeer => itemPeer.GetContainer(),
					_ => null,
				};

				if (owner is null)
				{
					return null;
				}

				map.TryAdd(owner, child);
			}
		}

		return new PeerChildrenScope(map);
	}

	/// <summary>
	/// Applies the automation children of the peer above <paramref name="element"/> (<paramref name="scope"/>) to it.
	/// Returns false when the element has a peer that is not reported, so it and its subtree must not be exposed.
	/// Otherwise <paramref name="childScope"/> is the scope for its visual children.
	/// </summary>
	private bool TryEnterPeerChildrenScope(UIElement element, PeerChildrenScope? scope, out PeerChildrenScope? childScope)
	{
		var peer = element.GetOrCreateAutomationPeer();

		AutomationPeer? reportedPeer = null;
		if (scope is not null && !scope.TryGetChildPeer(element, out reportedPeer) && peer is not null)
		{
			childScope = null;
			return false;
		}

		// An element without a peer is crossed by FrameworkElementAutomationPeer.GetChildrenCore, so the scope
		// above it still applies, unless it is the container of a reported item peer, which owns its children.
		var childrenSource = peer ?? reportedPeer;
		if (childrenSource is null)
		{
			childScope = scope;
		}
		else
		{
			childScope = element.GetChildren().Count == 0 ? null : CreatePeerChildrenScope(childrenSource);
		}

		return true;
	}

	/// <summary>
	/// Resolves the scope for the visual children of <paramref name="parent"/>, for walks that start below the root
	/// (an element added or shown after the initial build). Returns false when <paramref name="parent"/> is part of a
	/// subtree left out by a peer.
	/// </summary>
	private bool TryResolvePeerChildrenScope(UIElement? parent, out PeerChildrenScope? scope)
	{
		scope = null;

		// From parent up to the nearest ancestor with a peer, whose children decide.
		var path = new List<UIElement>();
		var foundPeer = false;
		for (var current = parent; current is not null; current = current.GetParent() as UIElement)
		{
			if (_peerExcludedHandles.Contains(current.Visual.Handle))
			{
				return false;
			}

			if (!foundPeer)
			{
				path.Add(current);
				foundPeer = current.GetOrCreateAutomationPeer() is not null;
			}
		}

		for (var i = path.Count - 1; i >= 0; i--)
		{
			if (!TryEnterPeerChildrenScope(path[i], scope, out scope))
			{
				return false;
			}
		}

		return true;
	}
}
