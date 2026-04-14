using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// UIA provider for a single UIElement in the accessibility tree.
/// Implements IRawElementProviderSimple and IRawElementProviderFragment.
/// The root element additionally implements IRawElementProviderFragmentRoot.
///
/// Navigation uses the automation peer tree (which flattens layout-only elements)
/// rather than the raw visual tree, matching WinUI3 behavior.
/// </summary>
[ComVisible(true)]
internal class Win32RawElementProvider :
	IRawElementProviderSimple,
	IRawElementProviderFragment,
	IRawElementProviderFragmentRoot
{
	private static int _nextRuntimeId = 1;

	private readonly UIElement _owner;
	private readonly nint _hwnd;
	private readonly int _runtimeId;
	private readonly bool _isRoot;
	private readonly Win32Accessibility _accessibility;
	private readonly WeakReference<AutomationPeer>? _representedPeer;
	private IList<AutomationPeer>? _cachedAutomationChildren;
	private const int MaxHitTestDepth = 1024;

	internal UIElement Owner => _owner;
	internal AutomationPeer? RepresentedPeer => _representedPeer is not null && _representedPeer.TryGetTarget(out var peer) ? peer : null;

	internal Win32RawElementProvider(
		UIElement owner,
		nint hwnd,
		bool isRoot,
		Win32Accessibility accessibility,
		AutomationPeer? representedPeer = null)
	{
		_owner = owner;
		_hwnd = hwnd;
		_isRoot = isRoot;
		_accessibility = accessibility;
		_representedPeer = representedPeer is not null ? new WeakReference<AutomationPeer>(representedPeer) : null;
		_runtimeId = _nextRuntimeId++;
	}

	internal bool RepresentsPeer(AutomationPeer peer)
		=> ReferenceEquals(GetAutomationPeer(), peer);

	// IRawElementProviderSimple

	public ProviderOptions ProviderOptions =>
		ProviderOptions.ServerSideProvider | ProviderOptions.UseComThreading;

	public object? GetPatternProvider(int patternId)
	{
		try
		{
			var peer = GetAutomationPeer();
			if (peer is null)
			{
				return null;
			}

			object? result = patternId switch
			{
				Win32UIAutomationInterop.UIA_InvokePatternId
					when peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke
					=> new UiaInvokeProviderWrapper(invoke),
				Win32UIAutomationInterop.UIA_TogglePatternId
					when peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle
					=> new UiaToggleProviderWrapper(toggle),
				Win32UIAutomationInterop.UIA_ValuePatternId
					when peer.GetPattern(PatternInterface.Value) is IValueProvider value
					=> new UiaValueProviderWrapper(value),
				Win32UIAutomationInterop.UIA_RangeValuePatternId
					when peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValue
					=> new UiaRangeValueProviderWrapper(rangeValue),
				Win32UIAutomationInterop.UIA_ExpandCollapsePatternId
					when peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapse
					=> new UiaExpandCollapseProviderWrapper(expandCollapse),
				Win32UIAutomationInterop.UIA_SelectionPatternId
					when peer.GetPattern(PatternInterface.Selection) is Microsoft.UI.Xaml.Automation.Provider.ISelectionProvider selection
					=> new UiaSelectionProviderWrapper(selection, _accessibility),
				Win32UIAutomationInterop.UIA_SelectionItemPatternId
					when peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItem
					=> new UiaSelectionItemProviderWrapper(selectionItem, _accessibility),
				Win32UIAutomationInterop.UIA_ScrollPatternId
					when peer.GetPattern(PatternInterface.Scroll) is IScrollProvider scroll
					=> new UiaScrollProviderWrapper(scroll),
				Win32UIAutomationInterop.UIA_ScrollItemPatternId
					when peer.GetPattern(PatternInterface.ScrollItem) is IScrollItemProvider scrollItem
					=> new UiaScrollItemProviderWrapper(scrollItem),
				Win32UIAutomationInterop.UIA_GridPatternId
					when peer.GetPattern(PatternInterface.Grid) is IGridProvider grid
					=> new UiaGridProviderWrapper(grid, _accessibility),
				Win32UIAutomationInterop.UIA_GridItemPatternId
					when peer.GetPattern(PatternInterface.GridItem) is IGridItemProvider gridItem
					=> new UiaGridItemProviderWrapper(gridItem, _accessibility),
				Win32UIAutomationInterop.UIA_TablePatternId
					when peer.GetPattern(PatternInterface.Table) is ITableProvider table
					=> new UiaTableProviderWrapper(table, _accessibility),
				_ => null,
			};

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				var patternName = GetPatternName(patternId);
				this.Log().Trace($"[UIA] GetPatternProvider({patternName}) on {DescribeElement()} → {(result is not null ? result.GetType().Name : "(null)")}");
			}

			return result;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"GetPatternProvider({patternId}) failed: {ex.Message}");
			}
			return null;
		}
	}

	public object? GetPropertyValue(int propertyId)
	{
		try
		{
			var peer = GetAutomationPeer();

			object? result = propertyId switch
			{
				// Identity & structure
				Win32UIAutomationInterop.UIA_NamePropertyId => GetName(peer),
				Win32UIAutomationInterop.UIA_AutomationIdPropertyId => GetAutomationId(peer),
				Win32UIAutomationInterop.UIA_ClassNamePropertyId => peer?.GetClassName(),
				Win32UIAutomationInterop.UIA_ControlTypePropertyId => GetControlTypeId(peer),
				Win32UIAutomationInterop.UIA_LocalizedControlTypePropertyId => peer?.GetLocalizedControlType(),
				Win32UIAutomationInterop.UIA_FrameworkIdPropertyId => "Uno",
				Win32UIAutomationInterop.UIA_ProviderDescriptionPropertyId => "Uno Platform UIA Provider",
				Win32UIAutomationInterop.UIA_ProcessIdPropertyId => GetProcessId(),
				Win32UIAutomationInterop.UIA_NativeWindowHandlePropertyId => _isRoot ? (int)_hwnd : 0,

				// State
				Win32UIAutomationInterop.UIA_IsEnabledPropertyId => peer?.IsEnabled() ?? (_owner is Control c ? c.IsEnabled : true),
				Win32UIAutomationInterop.UIA_IsKeyboardFocusablePropertyId => peer?.IsKeyboardFocusable() ?? false,
				Win32UIAutomationInterop.UIA_HasKeyboardFocusPropertyId => peer?.HasKeyboardFocus() ?? false,
				Win32UIAutomationInterop.UIA_IsOffscreenPropertyId => peer?.IsOffscreen() ?? false,
				Win32UIAutomationInterop.UIA_IsPasswordPropertyId => peer?.IsPassword() ?? false,
				Win32UIAutomationInterop.UIA_IsRequiredForFormPropertyId => peer?.IsRequiredForForm() ?? false,

				// Tree view membership
				Win32UIAutomationInterop.UIA_IsControlElementPropertyId => GetIsControlElement(peer),
				Win32UIAutomationInterop.UIA_IsContentElementPropertyId => GetIsContentElement(peer),

				// Description & labels
				Win32UIAutomationInterop.UIA_HelpTextPropertyId => peer?.GetHelpText(),
				Win32UIAutomationInterop.UIA_AcceleratorKeyPropertyId => GetNonEmpty(peer?.GetAcceleratorKey()),
				Win32UIAutomationInterop.UIA_AccessKeyPropertyId => GetNonEmpty(peer?.GetAccessKey()),
				Win32UIAutomationInterop.UIA_ItemTypePropertyId => GetNonEmpty(peer?.GetItemType()),
				Win32UIAutomationInterop.UIA_ItemStatusPropertyId => GetNonEmpty(peer?.GetItemStatus()),
				Win32UIAutomationInterop.UIA_FullDescriptionPropertyId => GetNonEmpty(AutomationProperties.GetFullDescription(_owner)),

				// Semantics
				Win32UIAutomationInterop.UIA_HeadingLevelPropertyId => MapHeadingLevel(AutomationProperties.GetHeadingLevel(_owner)),
				Win32UIAutomationInterop.UIA_LandmarkTypePropertyId => MapLandmarkType(AutomationProperties.GetLandmarkType(_owner)),
				Win32UIAutomationInterop.UIA_LocalizedLandmarkTypePropertyId => GetNonEmpty(AutomationProperties.GetLocalizedLandmarkType(_owner)),
				Win32UIAutomationInterop.UIA_LiveSettingPropertyId => (int)AutomationProperties.GetLiveSetting(_owner),
				Win32UIAutomationInterop.UIA_OrientationPropertyId => MapOrientation(peer),

				// Position in group
				Win32UIAutomationInterop.UIA_PositionInSetPropertyId => GetPositiveOrNull(peer?.GetPositionInSet() ?? AutomationProperties.GetPositionInSet(_owner)),
				Win32UIAutomationInterop.UIA_SizeOfSetPropertyId => GetPositiveOrNull(peer?.GetSizeOfSet() ?? AutomationProperties.GetSizeOfSet(_owner)),
				Win32UIAutomationInterop.UIA_LevelPropertyId => GetPositiveOrNull(peer?.GetLevel() ?? AutomationProperties.GetLevel(_owner)),

				// Form validation
				Win32UIAutomationInterop.UIA_IsDataValidForFormPropertyId => peer?.IsDataValidForForm() ?? true,

				// Dialog
				Win32UIAutomationInterop.UIA_IsDialogPropertyId => peer?.IsDialog() ?? false,

				// Relation properties
				Win32UIAutomationInterop.UIA_LabeledByPropertyId => GetLabeledByProvider(peer),
				Win32UIAutomationInterop.UIA_DescribedByPropertyId => null,
				Win32UIAutomationInterop.UIA_ControllerForPropertyId => null,
				Win32UIAutomationInterop.UIA_FlowsToPropertyId => null,
				Win32UIAutomationInterop.UIA_FlowsFromPropertyId => null,

				// ARIA properties - not applicable for non-web frameworks
				Win32UIAutomationInterop.UIA_AriaRolePropertyId => null,
				Win32UIAutomationInterop.UIA_AriaPropertiesPropertyId => null,

				// Culture / peripheral / other
				Win32UIAutomationInterop.UIA_CulturePropertyId => null,
				Win32UIAutomationInterop.UIA_IsPeripheralPropertyId => null,

				_ => null,
			};

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				var propName = GetPropertyName(propertyId);
				this.Log().Trace($"[UIA] GetPropertyValue({propName}) on {DescribeElement()} → {result ?? "(null)"}");
			}

			return result;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"GetPropertyValue({propertyId}) failed: {ex.Message}");
			}
			return null;
		}
	}

	public IRawElementProviderSimple? HostRawElementProvider
	{
		get
		{
			if (_isRoot)
			{
				var hr = Win32UIAutomationInterop.UiaHostProviderFromHwnd(_hwnd, out var hostProvider);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[UIA] HostRawElementProvider: hwnd=0x{_hwnd:X}, hr=0x{hr:X}, hostProvider={(hostProvider is not null ? "present" : "NULL")}");
				}

				return hostProvider;
			}
			return null;
		}
	}

	// IRawElementProviderFragment

	public IRawElementProviderFragment? Navigate(NavigateDirection direction)
	{
		try
		{
			var result = direction switch
			{
				NavigateDirection.Parent => FindParentProvider(),
				NavigateDirection.FirstChild => GetFirstChild(),
				NavigateDirection.LastChild => GetLastChild(),
				NavigateDirection.NextSibling => GetNextSibling(),
				NavigateDirection.PreviousSibling => GetPreviousSibling(),
				_ => null,
			};

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				var targetDesc = result is Win32RawElementProvider targetProvider
					? targetProvider.DescribeElement()
					: "(null)";
				this.Log().Debug($"[UIA] Navigate({direction}) on {DescribeElement()} → {targetDesc}");
			}

			return result;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Navigate({direction}) on {DescribeElement()} failed: {ex.Message}");
			}
			return null;
		}
	}

	public int[]? GetRuntimeId()
	{
		return [Win32UIAutomationInterop.UiaAppendRuntimeId, _runtimeId];
	}

	public UiaRect BoundingRectangle
	{
		get
		{
			try
			{
				var visual = _owner.Visual;
				var size = visual.Size;

				// Compute the full element-to-root transform, then transform the
				// local rect to get the axis-aligned bounding box in window coords.
				// This correctly handles rotation, scale, and skew transforms.
				var transform = UIElement.GetTransform(from: _owner, to: null);
				var localRect = new Windows.Foundation.Rect(0, 0, size.X, size.Y);
				var logicalRect = transform.Transform(localRect);

				// Clip to ancestor scroll/clip regions so Narrator doesn't report
				// bounds for content that is scrolled out of view.
				logicalRect = ClipToAncestors(_owner, logicalRect);

				if (logicalRect.Width <= 0 || logicalRect.Height <= 0)
				{
					return default;
				}

				// Convert logical pixels to physical screen coordinates
				float dpiScale = Win32UIAutomationInterop.GetDpiForWindow(_hwnd)
					/ (float)Win32UIAutomationInterop.USER_DEFAULT_SCREEN_DPI;
				if (dpiScale <= 0)
				{
					dpiScale = 1.0f;
				}

				var clientOrigin = new System.Drawing.Point(0, 0);
				Win32UIAutomationInterop.ClientToScreen(_hwnd, ref clientOrigin);

				return new UiaRect
				{
					Left = clientOrigin.X + logicalRect.X * dpiScale,
					Top = clientOrigin.Y + logicalRect.Y * dpiScale,
					Width = logicalRect.Width * dpiScale,
					Height = logicalRect.Height * dpiScale,
				};
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"BoundingRectangle failed: {ex.Message}");
				}
				return default;
			}
		}
	}

	public IRawElementProviderFragment[]? GetEmbeddedFragmentRoots() => null;

	public void SetFocus()
	{
		if (_owner is Control control)
		{
			control.Focus(FocusState.Programmatic);
		}
		else
		{
			// For non-Control elements, try to set focus via the automation peer
			GetAutomationPeer()?.SetFocus();
		}
	}

	public IRawElementProviderFragmentRoot? FragmentRoot
		=> _accessibility.RootProvider;

	// IRawElementProviderFragmentRoot (only meaningful on root element)

	public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y)
	{
		try
		{
			var result = FindDeepestProviderAtPoint(x, y) ?? this;

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				var desc = result is Win32RawElementProvider p ? p.DescribeElement() : "self";
				this.Log().Debug($"[UIA] ElementProviderFromPoint({x:F0}, {y:F0}) → {desc}");
			}

			return result;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"ElementProviderFromPoint({x}, {y}) failed: {ex.Message}");
			}
			return this;
		}
	}

	public IRawElementProviderFragment? GetFocus()
	{
		try
		{
			var xamlRoot = _owner.XamlRoot;
			if (xamlRoot is null)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[UIA] GetFocus: XamlRoot is null on {DescribeElement()}");
				}
				return null;
			}

			var focusedElement = FocusManager.GetFocusedElement(xamlRoot);
			if (focusedElement is UIElement uiElement)
			{
				var focusedPeer = uiElement.GetOrCreateAutomationPeer();

				// Return the provider for the focused element's effective automation peer,
				// or its nearest ancestor that has an automation peer.
				var result = focusedPeer is not null
					? _accessibility.GetProviderForPeer(focusedPeer, resolveEventsSource: true)
						?? FindNearestProviderAncestor(uiElement)
					: FindNearestProviderAncestor(uiElement);

				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					var desc = result is Win32RawElementProvider p ? p.DescribeElement() : "(null)";
					this.Log().Debug($"[UIA] GetFocus → focused={uiElement.GetType().Name}, provider={desc}");
				}

				return result;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[UIA] GetFocus → no focused UIElement (focusedElement={focusedElement?.GetType().Name ?? "null"})");
				}
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"GetFocus failed: {ex.Message}");
			}
		}
		return null;
	}

	// Navigation helpers - use automation peer tree, not visual tree

	/// <summary>
	/// Gets the automation peer children for this element. If the element has an
	/// automation peer, its <see cref="AutomationPeer.GetChildren"/> provides the
	/// filtered/flattened list. Otherwise we walk the visual tree to collect peers,
	/// mimicking <see cref="FrameworkElementAutomationPeer.GetChildrenCore"/>.
	/// </summary>
	internal void InvalidateChildrenCache() => _cachedAutomationChildren = null;

	private IList<AutomationPeer>? GetAutomationChildren()
	{
		if (_cachedAutomationChildren is not null)
		{
			return _cachedAutomationChildren;
		}

		var peer = GetAutomationPeer();
		IList<AutomationPeer>? result;

		if (peer is not null)
		{
			result = peer.GetChildren();
		}
		else
		{
			// Fallback for elements without automation peers (e.g. root if it's a raw panel):
			// Walk visual tree and collect automation peers from descendants.
			var children = new List<AutomationPeer>();
			CollectAutomationPeers(_owner, children);
			result = children.Count > 0 ? children : null;
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			var count = result?.Count ?? 0;
			var peerDesc = peer?.GetType().Name ?? "none";
			var childNames = "";
			if (result is not null && count <= 10)
			{
				var names = new List<string>();
				foreach (var child in result)
				{
					var childType = child?.GetType().Name ?? "null";
					string childName;
					try { childName = child?.GetName() ?? ""; }
					catch { childName = "<error>"; }
					names.Add($"{childType}({childName})");
				}
				childNames = $" [{string.Join(", ", names)}]";
			}
			this.Log().Debug($"[UIA] GetAutomationChildren on {DescribeElement()} (peer={peerDesc}) → {count} children{childNames}");
		}

		_cachedAutomationChildren = result;
		return result;
	}

	private static void CollectAutomationPeers(UIElement element, List<AutomationPeer> result)
	{
		foreach (var child in element.GetChildren())
		{
			if (child.Visibility == Visibility.Collapsed)
			{
				continue;
			}

			var peer = child.GetOrCreateAutomationPeer();
			if (peer is not null)
			{
				result.Add(peer);
			}
			else
			{
				// No peer on this element - flatten by recursing into its children
				CollectAutomationPeers(child, result);
			}
		}
	}

	private IRawElementProviderFragment? GetFirstChild()
	{
		var children = GetAutomationChildren();
		if (children is null || children.Count == 0)
		{
			return null;
		}

		for (var i = 0; i < children.Count; i++)
		{
			var provider = _accessibility.GetProviderForPeer(children[i], resolveEventsSource: true);
			if (provider is not null && !ReferenceEquals(provider, this))
			{
				return provider;
			}
		}
		return null;
	}

	private IRawElementProviderFragment? GetLastChild()
	{
		var children = GetAutomationChildren();
		if (children is null || children.Count == 0)
		{
			return null;
		}

		for (var i = children.Count - 1; i >= 0; i--)
		{
			var provider = _accessibility.GetProviderForPeer(children[i], resolveEventsSource: true);
			if (provider is not null && !ReferenceEquals(provider, this))
			{
				return provider;
			}
		}
		return null;
	}

	private IRawElementProviderFragment? GetNextSibling()
	{
		var parentProvider = FindParentProvider();
		if (parentProvider is null)
		{
			return null;
		}

		var siblings = parentProvider.GetAutomationChildren();
		if (siblings is null)
		{
			return null;
		}

		var myPeer = GetAutomationPeer();
		var foundSelf = false;

		for (var i = 0; i < siblings.Count; i++)
		{
			if (foundSelf)
			{
				var provider = _accessibility.GetProviderForPeer(siblings[i], resolveEventsSource: true);
				if (provider is not null && !ReferenceEquals(provider, this))
				{
					return provider;
				}
			}
			else if (IsSamePeer(siblings[i], myPeer))
			{
				foundSelf = true;
			}
		}
		return null;
	}

	private IRawElementProviderFragment? GetPreviousSibling()
	{
		var parentProvider = FindParentProvider();
		if (parentProvider is null)
		{
			return null;
		}

		var siblings = parentProvider.GetAutomationChildren();
		if (siblings is null)
		{
			return null;
		}

		var myPeer = GetAutomationPeer();
		Win32RawElementProvider? previous = null;

		for (var i = 0; i < siblings.Count; i++)
		{
			if (IsSamePeer(siblings[i], myPeer))
			{
				return previous;
			}
			var provider = _accessibility.GetProviderForPeer(siblings[i], resolveEventsSource: true);
			if (provider is not null && !ReferenceEquals(provider, this))
			{
				previous = provider;
			}
		}
		return null;
	}

	/// <summary>
	/// Finds the parent UIA provider for this element. Prefers the automation
	/// peer tree (via <see cref="AutomationPeer.GetParent"/>) since it may
	/// differ from the visual tree — e.g. <see cref="ItemAutomationPeer"/>'s
	/// parent is the <see cref="ItemsControlAutomationPeer"/>, not the visual
	/// parent. Falls back to walking the visual tree if the peer tree doesn't
	/// resolve to a provider.
	/// </summary>
	private Win32RawElementProvider? FindParentProvider()
	{
		if (_isRoot)
		{
			return null;
		}

		// First, try the automation peer tree. This is the correct path for
		// ItemAutomationPeer and other peers whose logical parent differs
		// from the visual parent.
		var myPeer = GetAutomationPeer();
		if (myPeer?.GetParent() is { } parentPeer)
		{
			var parentProvider = _accessibility.GetProviderForPeer(parentPeer);
			if (parentProvider is not null)
			{
				return parentProvider;
			}
		}

		// Fall back to the visual tree when the peer tree doesn't yield a
		// provider (e.g. the parent peer has no owner element yet).
		var current = VisualTreeHelper.GetParent(_owner) as UIElement;
		while (current is not null)
		{
			// Check if this is the root element
			if (_accessibility.RootProvider is { } root && ReferenceEquals(root.Owner, current))
			{
				return root;
			}

			// Check if this ancestor has an automation peer
			if (current.GetOrCreateAutomationPeer() is not null)
			{
				return _accessibility.GetOrCreateProvider(current);
			}

			current = VisualTreeHelper.GetParent(current) as UIElement;
		}

		// If no ancestor with a peer was found, return the root
		return _accessibility.RootProvider;
	}

	/// <summary>
	/// Finds the nearest ancestor with a UIA provider, for elements that
	/// might not have a direct provider themselves.
	/// </summary>
	private Win32RawElementProvider? FindNearestProviderAncestor(UIElement element)
	{
		var current = VisualTreeHelper.GetParent(element) as UIElement;
		while (current is not null)
		{
			// Use GetOrCreateProvider so that ancestor providers are lazily created —
			// GetProvider only looks in the already-created cache and would fall back to
			// root when no providers have been created yet (e.g. on first GetFocus call).
			var provider = _accessibility.GetOrCreateProvider(current);
			if (provider is not null)
			{
				return provider;
			}
			current = VisualTreeHelper.GetParent(current) as UIElement;
		}
		return _accessibility.RootProvider;
	}

	/// <summary>
	/// Compares two automation peers for identity. Uses reference equality first,
	/// then falls back to checking if they wrap the same UIElement.
	/// </summary>
	private static bool IsSamePeer(AutomationPeer? a, AutomationPeer? b)
	{
		if (a is null || b is null)
		{
			return false;
		}

		if (ReferenceEquals(a, b))
		{
			return true;
		}

		// Fall back to comparing owner UIElements
		if (a is FrameworkElementAutomationPeer feapA
			&& b is FrameworkElementAutomationPeer feapB)
		{
			return ReferenceEquals(feapA.Owner, feapB.Owner);
		}

		return false;
	}

	// Hit testing helper

	private Win32RawElementProvider? FindDeepestProviderAtPoint(double screenX, double screenY)
		=> FindDeepestProviderAtPoint(screenX, screenY, new HashSet<Win32RawElementProvider>(), 0);

	private Win32RawElementProvider? FindDeepestProviderAtPoint(double screenX, double screenY, HashSet<Win32RawElementProvider> visited, int depth)
	{
		if (!visited.Add(this))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] Hit-test cycle detected at {DescribeElement()}");
			}
			return null;
		}

		if (depth > MaxHitTestDepth)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] Hit-test depth limit exceeded at {DescribeElement()}");
			}
			return null;
		}

		// Use the automation children (filtered tree) for hit testing
		var children = GetAutomationChildren();
		if (children is not null)
		{
			// Search in reverse order (topmost Z-index first)
			for (var i = children.Count - 1; i >= 0; i--)
			{
				var childProvider = _accessibility.GetProviderForPeer(children[i], resolveEventsSource: true);
				if (childProvider is not null && !ReferenceEquals(childProvider, this))
				{
					var found = childProvider.FindDeepestProviderAtPoint(screenX, screenY, visited, depth + 1);
					if (found is not null)
					{
						return found;
					}
				}
			}
		}

		// Check if point is within this element's bounds
		if (ContainsPoint(screenX, screenY))
		{
			return this;
		}

		return null;
	}

	// Property helpers

	private IRawElementProviderSimple? GetLabeledByProvider(AutomationPeer? peer)
	{
		// Check AutomationProperties.LabeledBy attached property first
		var labeledByElement = AutomationProperties.GetLabeledBy(_owner);
		if (labeledByElement is UIElement labelElement)
		{
			return _accessibility.GetOrCreateProvider(labelElement);
		}

		// Fall back to peer's GetLabeledBy()
		if (peer?.GetLabeledBy() is { } labeledByPeer)
		{
			return _accessibility.GetProviderForPeer(labeledByPeer);
		}

		return null;
	}

	private string? GetName(AutomationPeer? peer)
	{
		if (_isRoot && peer is null)
		{
			// Root without a peer returns the window title
			return GetWindowTitle();
		}
		// Return null instead of empty string so UIA knows the name is unset
		// and can fall back to other name computation strategies.
		return GetNonEmpty(peer?.GetName());
	}

	private string? GetAutomationId(AutomationPeer? peer)
	{
		// Prefer the attached property, fall back to the peer
		var id = AutomationProperties.GetAutomationId(_owner);
		if (!string.IsNullOrEmpty(id))
		{
			return id;
		}
		return GetNonEmpty(peer?.GetAutomationId());
	}

	private int GetControlTypeId(AutomationPeer? peer)
	{
		if (peer is null)
		{
			return _isRoot
				? Win32UIAutomationInterop.UIA_PaneControlTypeId
				: Win32UIAutomationInterop.UIA_CustomControlTypeId;
		}

		return MapControlType(peer.GetAutomationControlType());
	}

	private bool GetIsControlElement(AutomationPeer? peer)
	{
		if (_isRoot)
		{
			return true;
		}

		// Elements without automation peers are not control elements
		return peer?.IsControlElement() ?? false;
	}

	private bool GetIsContentElement(AutomationPeer? peer)
	{
		if (_isRoot)
		{
			return true;
		}

		return peer?.IsContentElement() ?? false;
	}

	private int GetProcessId()
	{
		_ = Win32UIAutomationInterop.GetWindowThreadProcessId(_hwnd, out var processId);
		return processId;
	}

	private string? GetWindowTitle()
	{
		if (!_isRoot)
		{
			return null;
		}

		// Try to get the window title from the HWND
		try
		{
			var titleLength = GetWindowTextLength(_hwnd);
			if (titleLength > 0)
			{
				var buffer = new char[titleLength + 1];
				unsafe
				{
					fixed (char* pBuffer = buffer)
					{
						var charsRead = GetWindowText(_hwnd, pBuffer, buffer.Length);
						if (charsRead > 0)
						{
							return new string(buffer, 0, charsRead);
						}
					}
				}
			}
		}
		catch
		{
			// Fall through
		}

		return "Uno Platform";
	}

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowTextLength(nint hWnd);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern unsafe int GetWindowText(nint hWnd, char* lpString, int nMaxCount);

	private static string? GetNonEmpty(string? value)
		=> string.IsNullOrEmpty(value) ? null : value;

	private static int? GetPositiveOrNull(int value)
		=> value > 0 ? value : null;

	private static int? MapOrientation(AutomationPeer? peer)
	{
		if (peer is null)
		{
			return null;
		}
		return peer.GetOrientation() switch
		{
			AutomationOrientation.Horizontal => Win32UIAutomationInterop.OrientationType_Horizontal,
			AutomationOrientation.Vertical => Win32UIAutomationInterop.OrientationType_Vertical,
			_ => Win32UIAutomationInterop.OrientationType_None,
		};
	}

	internal static int MapHeadingLevel(AutomationHeadingLevel level) => level switch
	{
		AutomationHeadingLevel.None => Win32UIAutomationInterop.HeadingLevel_None,
		AutomationHeadingLevel.Level1 => Win32UIAutomationInterop.HeadingLevel1,
		AutomationHeadingLevel.Level2 => Win32UIAutomationInterop.HeadingLevel2,
		AutomationHeadingLevel.Level3 => Win32UIAutomationInterop.HeadingLevel3,
		AutomationHeadingLevel.Level4 => Win32UIAutomationInterop.HeadingLevel4,
		AutomationHeadingLevel.Level5 => Win32UIAutomationInterop.HeadingLevel5,
		AutomationHeadingLevel.Level6 => Win32UIAutomationInterop.HeadingLevel6,
		AutomationHeadingLevel.Level7 => Win32UIAutomationInterop.HeadingLevel7,
		AutomationHeadingLevel.Level8 => Win32UIAutomationInterop.HeadingLevel8,
		AutomationHeadingLevel.Level9 => Win32UIAutomationInterop.HeadingLevel9,
		_ => Win32UIAutomationInterop.HeadingLevel_None,
	};

	internal static int? MapLandmarkType(AutomationLandmarkType type) => type switch
	{
		AutomationLandmarkType.None => null,
		AutomationLandmarkType.Custom => Win32UIAutomationInterop.UIA_CustomLandmarkTypeId,
		AutomationLandmarkType.Form => Win32UIAutomationInterop.UIA_FormLandmarkTypeId,
		AutomationLandmarkType.Main => Win32UIAutomationInterop.UIA_MainLandmarkTypeId,
		AutomationLandmarkType.Navigation => Win32UIAutomationInterop.UIA_NavigationLandmarkTypeId,
		AutomationLandmarkType.Search => Win32UIAutomationInterop.UIA_SearchLandmarkTypeId,
		_ => null,
	};

	internal static int MapControlType(AutomationControlType controlType) => controlType switch
	{
		AutomationControlType.Button => Win32UIAutomationInterop.UIA_ButtonControlTypeId,
		AutomationControlType.Calendar => Win32UIAutomationInterop.UIA_CalendarControlTypeId,
		AutomationControlType.CheckBox => Win32UIAutomationInterop.UIA_CheckBoxControlTypeId,
		AutomationControlType.ComboBox => Win32UIAutomationInterop.UIA_ComboBoxControlTypeId,
		AutomationControlType.Edit => Win32UIAutomationInterop.UIA_EditControlTypeId,
		AutomationControlType.Hyperlink => Win32UIAutomationInterop.UIA_HyperlinkControlTypeId,
		AutomationControlType.Image => Win32UIAutomationInterop.UIA_ImageControlTypeId,
		AutomationControlType.ListItem => Win32UIAutomationInterop.UIA_ListItemControlTypeId,
		AutomationControlType.List => Win32UIAutomationInterop.UIA_ListControlTypeId,
		AutomationControlType.Menu => Win32UIAutomationInterop.UIA_MenuControlTypeId,
		AutomationControlType.MenuBar => Win32UIAutomationInterop.UIA_MenuBarControlTypeId,
		AutomationControlType.MenuItem => Win32UIAutomationInterop.UIA_MenuItemControlTypeId,
		AutomationControlType.ProgressBar => Win32UIAutomationInterop.UIA_ProgressBarControlTypeId,
		AutomationControlType.RadioButton => Win32UIAutomationInterop.UIA_RadioButtonControlTypeId,
		AutomationControlType.ScrollBar => Win32UIAutomationInterop.UIA_ScrollBarControlTypeId,
		AutomationControlType.Slider => Win32UIAutomationInterop.UIA_SliderControlTypeId,
		AutomationControlType.Spinner => Win32UIAutomationInterop.UIA_SpinnerControlTypeId,
		AutomationControlType.StatusBar => Win32UIAutomationInterop.UIA_StatusBarControlTypeId,
		AutomationControlType.Tab => Win32UIAutomationInterop.UIA_TabControlTypeId,
		AutomationControlType.TabItem => Win32UIAutomationInterop.UIA_TabItemControlTypeId,
		AutomationControlType.Text => Win32UIAutomationInterop.UIA_TextControlTypeId,
		AutomationControlType.ToolBar => Win32UIAutomationInterop.UIA_ToolBarControlTypeId,
		AutomationControlType.ToolTip => Win32UIAutomationInterop.UIA_ToolTipControlTypeId,
		AutomationControlType.Tree => Win32UIAutomationInterop.UIA_TreeControlTypeId,
		AutomationControlType.TreeItem => Win32UIAutomationInterop.UIA_TreeItemControlTypeId,
		AutomationControlType.Custom => Win32UIAutomationInterop.UIA_CustomControlTypeId,
		AutomationControlType.Group => Win32UIAutomationInterop.UIA_GroupControlTypeId,
		AutomationControlType.Thumb => Win32UIAutomationInterop.UIA_ThumbControlTypeId,
		AutomationControlType.DataGrid => Win32UIAutomationInterop.UIA_DataGridControlTypeId,
		AutomationControlType.DataItem => Win32UIAutomationInterop.UIA_DataItemControlTypeId,
		AutomationControlType.Document => Win32UIAutomationInterop.UIA_DocumentControlTypeId,
		AutomationControlType.SplitButton => Win32UIAutomationInterop.UIA_SplitButtonControlTypeId,
		AutomationControlType.Window => Win32UIAutomationInterop.UIA_WindowControlTypeId,
		AutomationControlType.Pane => Win32UIAutomationInterop.UIA_PaneControlTypeId,
		AutomationControlType.Header => Win32UIAutomationInterop.UIA_HeaderControlTypeId,
		AutomationControlType.HeaderItem => Win32UIAutomationInterop.UIA_HeaderItemControlTypeId,
		AutomationControlType.Table => Win32UIAutomationInterop.UIA_TableControlTypeId,
		AutomationControlType.Separator => Win32UIAutomationInterop.UIA_SeparatorControlTypeId,
		AutomationControlType.SemanticZoom => Win32UIAutomationInterop.UIA_SemanticZoomControlTypeId,
		AutomationControlType.AppBar => Win32UIAutomationInterop.UIA_AppBarControlTypeId,
		AutomationControlType.FlipView => Win32UIAutomationInterop.UIA_FlipViewControlTypeId,
		_ => Win32UIAutomationInterop.UIA_CustomControlTypeId,
	};

	// Diagnostic helpers

	internal string DescribeElement()
	{
		var resolvedPeer = _representedPeer is not null && _representedPeer.TryGetTarget(out var rp) ? rp : null;
		var typeName = resolvedPeer is not null && resolvedPeer is not FrameworkElementAutomationPeer
			? $"{resolvedPeer.GetType().Name}->{_owner.GetType().Name}"
			: _owner.GetType().Name;
		var automationId = AutomationProperties.GetAutomationId(_owner);
		var name = AutomationProperties.GetName(_owner);
		var details = !string.IsNullOrEmpty(automationId) ? automationId
			: !string.IsNullOrEmpty(name) ? $"\"{name}\""
			: $"rid={_runtimeId}";
		return $"{typeName}({details}){(_isRoot ? " [ROOT]" : "")}";
	}

	private static string GetPropertyName(int propertyId) => propertyId switch
	{
		Win32UIAutomationInterop.UIA_NamePropertyId => "Name",
		Win32UIAutomationInterop.UIA_AutomationIdPropertyId => "AutomationId",
		Win32UIAutomationInterop.UIA_ClassNamePropertyId => "ClassName",
		Win32UIAutomationInterop.UIA_ControlTypePropertyId => "ControlType",
		Win32UIAutomationInterop.UIA_LocalizedControlTypePropertyId => "LocalizedControlType",
		Win32UIAutomationInterop.UIA_FrameworkIdPropertyId => "FrameworkId",
		Win32UIAutomationInterop.UIA_ProviderDescriptionPropertyId => "ProviderDescription",
		Win32UIAutomationInterop.UIA_ProcessIdPropertyId => "ProcessId",
		Win32UIAutomationInterop.UIA_NativeWindowHandlePropertyId => "NativeWindowHandle",
		Win32UIAutomationInterop.UIA_IsEnabledPropertyId => "IsEnabled",
		Win32UIAutomationInterop.UIA_IsKeyboardFocusablePropertyId => "IsKeyboardFocusable",
		Win32UIAutomationInterop.UIA_HasKeyboardFocusPropertyId => "HasKeyboardFocus",
		Win32UIAutomationInterop.UIA_IsOffscreenPropertyId => "IsOffscreen",
		Win32UIAutomationInterop.UIA_IsPasswordPropertyId => "IsPassword",
		Win32UIAutomationInterop.UIA_IsRequiredForFormPropertyId => "IsRequiredForForm",
		Win32UIAutomationInterop.UIA_IsControlElementPropertyId => "IsControlElement",
		Win32UIAutomationInterop.UIA_IsContentElementPropertyId => "IsContentElement",
		Win32UIAutomationInterop.UIA_HelpTextPropertyId => "HelpText",
		Win32UIAutomationInterop.UIA_AcceleratorKeyPropertyId => "AcceleratorKey",
		Win32UIAutomationInterop.UIA_AccessKeyPropertyId => "AccessKey",
		Win32UIAutomationInterop.UIA_ItemTypePropertyId => "ItemType",
		Win32UIAutomationInterop.UIA_ItemStatusPropertyId => "ItemStatus",
		Win32UIAutomationInterop.UIA_FullDescriptionPropertyId => "FullDescription",
		Win32UIAutomationInterop.UIA_HeadingLevelPropertyId => "HeadingLevel",
		Win32UIAutomationInterop.UIA_LandmarkTypePropertyId => "LandmarkType",
		Win32UIAutomationInterop.UIA_LocalizedLandmarkTypePropertyId => "LocalizedLandmarkType",
		Win32UIAutomationInterop.UIA_LiveSettingPropertyId => "LiveSetting",
		Win32UIAutomationInterop.UIA_OrientationPropertyId => "Orientation",
		Win32UIAutomationInterop.UIA_PositionInSetPropertyId => "PositionInSet",
		Win32UIAutomationInterop.UIA_SizeOfSetPropertyId => "SizeOfSet",
		Win32UIAutomationInterop.UIA_LevelPropertyId => "Level",
		Win32UIAutomationInterop.UIA_BoundingRectanglePropertyId => "BoundingRectangle",
		Win32UIAutomationInterop.UIA_RuntimeIdPropertyId => "RuntimeId",
		Win32UIAutomationInterop.UIA_ClickablePointPropertyId => "ClickablePoint",
		Win32UIAutomationInterop.UIA_CulturePropertyId => "Culture",
		Win32UIAutomationInterop.UIA_LabeledByPropertyId => "LabeledBy",
		Win32UIAutomationInterop.UIA_DescribedByPropertyId => "DescribedBy",
		Win32UIAutomationInterop.UIA_FlowsToPropertyId => "FlowsTo",
		Win32UIAutomationInterop.UIA_FlowsFromPropertyId => "FlowsFrom",
		Win32UIAutomationInterop.UIA_AriaRolePropertyId => "AriaRole",
		Win32UIAutomationInterop.UIA_AriaPropertiesPropertyId => "AriaProperties",
		Win32UIAutomationInterop.UIA_IsDataValidForFormPropertyId => "IsDataValidForForm",
		Win32UIAutomationInterop.UIA_ControllerForPropertyId => "ControllerFor",
		Win32UIAutomationInterop.UIA_IsPeripheralPropertyId => "IsPeripheral",
		Win32UIAutomationInterop.UIA_IsDialogPropertyId => "IsDialog",
		Win32UIAutomationInterop.UIA_OptimizeForVisualContentPropertyId => "OptimizeForVisualContent",
		_ => $"Unknown({propertyId})",
	};

	private static string GetPatternName(int patternId) => patternId switch
	{
		Win32UIAutomationInterop.UIA_InvokePatternId => "Invoke",
		Win32UIAutomationInterop.UIA_TogglePatternId => "Toggle",
		Win32UIAutomationInterop.UIA_ValuePatternId => "Value",
		Win32UIAutomationInterop.UIA_RangeValuePatternId => "RangeValue",
		Win32UIAutomationInterop.UIA_ExpandCollapsePatternId => "ExpandCollapse",
		Win32UIAutomationInterop.UIA_SelectionPatternId => "Selection",
		Win32UIAutomationInterop.UIA_SelectionItemPatternId => "SelectionItem",
		Win32UIAutomationInterop.UIA_ScrollPatternId => "Scroll",
		Win32UIAutomationInterop.UIA_ScrollItemPatternId => "ScrollItem",
		Win32UIAutomationInterop.UIA_GridPatternId => "Grid",
		Win32UIAutomationInterop.UIA_GridItemPatternId => "GridItem",
		Win32UIAutomationInterop.UIA_TablePatternId => "Table",
		_ => $"Unknown({patternId})",
	};

	// IRawElementProviderAdviseEvents

	public void AdviseEventAdded(int eventId, int[]? propertyIds)
	{
		_accessibility.OnAdviseEventAdded(eventId, propertyIds);
	}

	public void AdviseEventRemoved(int eventId, int[]? propertyIds)
	{
		_accessibility.OnAdviseEventRemoved(eventId, propertyIds);
	}

	/// <summary>
	/// Clips a logical rect to ancestor elements that have Clip set (e.g., ScrollViewer).
	/// This prevents Narrator from reporting bounds for content scrolled out of view.
	/// </summary>
	private static Windows.Foundation.Rect ClipToAncestors(UIElement element, Windows.Foundation.Rect rect)
	{
		var ancestor = element.GetParent() as UIElement;
		while (ancestor is not null)
		{
			if (ancestor.Clip is RectangleGeometry clip)
			{
				var clipTransform = UIElement.GetTransform(from: ancestor, to: null);
				var clipRect = clipTransform.Transform(clip.Rect);
				rect.Intersect(clipRect);
				if (rect.IsEmpty)
				{
					return new Windows.Foundation.Rect(0, 0, 0, 0);
				}
			}
			ancestor = ancestor.GetParent() as UIElement;
		}
		return rect;
	}

	private AutomationPeer? GetAutomationPeer()
		=> (_representedPeer is not null && _representedPeer.TryGetTarget(out var peer) ? peer : null)
			?? _owner.GetOrCreateAutomationPeer();

	private bool ContainsPoint(double screenX, double screenY)
	{
		var bounds = BoundingRectangle;
		return screenX >= bounds.Left
			&& screenX < bounds.Left + bounds.Width
			&& screenY >= bounds.Top
			&& screenY < bounds.Top + bounds.Height;
	}
}
