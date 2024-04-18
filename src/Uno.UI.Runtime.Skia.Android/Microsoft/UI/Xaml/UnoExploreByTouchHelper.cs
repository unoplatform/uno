using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Java.Lang;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class UnoExploreByTouchHelper : ExploreByTouchHelper
{
	private readonly UIElement _rootElement;
	private ConditionalWeakTable<DependencyObject, object> _cwtElementToId = new();
	private Dictionary<int, DependencyObject?> _idToElement = new(); // TODO: This will leak.
	private int _currentId;

	public UnoExploreByTouchHelper(UnoSKCanvasView host, UIElement rootElement) : base(host)
	{
		_rootElement = rootElement;
	}

	private static bool ShouldSkipElement(DependencyObject element)
	{
		var accessibilityView = AutomationProperties.GetAccessibilityView(element);
		if (accessibilityView == AccessibilityView.Raw)
		{
			return true;
		}

		// TODO: What about non-UIElements? e.g, Hyperlinks?
		// In WinUI, `TextElement`s can have automation peers. We need to support that in Uno.
		if ((element as UIElement)?.GetOrCreateAutomationPeer() is null)
		{
			return true;
		}

		return false;
	}

	protected override int GetVirtualViewAt(float x, float y)
	{
		var (element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(x, y).PhysicalToLogicalPixels(), _rootElement.XamlRoot);
		element ??= _rootElement;
		try
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = true;
			while (!FocusProperties.IsPotentialTabStop(element) || ShouldSkipElement(element))
			{
				// Walking the tree up is not correct in the case of render transforms.
				// We could press on some coordinates and end up walking the tree up and retrieving
				// a parent that doesn't contain the pressed point.
				// TODO: Find a good way to handle this case.
				element = element.GetUIElementAdjustedParentInternal();
				if (element is null)
				{
					return ExploreByTouchHelper.HostId;
				}
			}

			return GetOrCreateVirtualId(element);
		}
		finally
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = false;
		}
	}

	private int GetOrCreateVirtualId(DependencyObject element)
	{
		if (_cwtElementToId.TryGetValue(element, out var existingId))
		{
			return (int)existingId;
		}

		var id = Interlocked.Increment(ref _currentId);
		_cwtElementToId.Add(element, id);
		_idToElement.Add(id, element);
		return id;
	}

	protected override void GetVisibleVirtualViews(IList<Integer> virtualViewIds)
	{
		var focusManager = VisualTree.GetFocusManagerForElement(_rootElement);
		if (focusManager == null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("A focus manager couldn't be found to get virtual views.");
			}

			return;
		}

		try
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = true;

			var current = focusManager.GetNextTabStop(_rootElement);
			var firstFocusable = current;
			while (current is not null)
			{
				if (!ShouldSkipElement(current))
				{
					virtualViewIds.Add(Integer.ValueOf(GetOrCreateVirtualId(current)));
				}

				current = focusManager.GetNextTabStop(current);
				if (current == firstFocusable)
				{
					break;
				}
			}
		}
		finally
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = false;
		}
	}

	protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle arguments)
	{
		// TODO: What about non-UIElements? e.g, Hyperlinks?
		// In WinUI, `TextElement`s can have automation peers. We need to support that in Uno.
		if (_idToElement.TryGetValue(virtualViewId, out var element) &&
			element is UIElement uiElement &&
			uiElement.GetOrCreateAutomationPeer() is { } peer)
		{
			if (peer.InvokeAutomationPeer())
			{
				return true;
			}
		}

		return false;
	}

	protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat node)
	{
		// TODO: What about non-UIElements? e.g, Hyperlinks?
		// In WinUI, `TextElement`s can have automation peers. We need to support that in Uno.
		if (_idToElement.TryGetValue(virtualViewId, out var element) &&
			element is UIElement uiElement)
		{
			var transform = UIElement.GetTransform(from: uiElement, to: null);
			var logicalRect = transform.Transform(new Windows.Foundation.Rect(default, new Windows.Foundation.Size(uiElement.Visual.Size.X, uiElement.Visual.Size.Y)));
			var physicalRect = logicalRect.LogicalToPhysicalPixels();
#pragma warning disable CS0618 // Type or member is obsolete
			node.SetBoundsInParent(new global::Android.Graphics.Rect((int)physicalRect.Left, (int)physicalRect.Top, (int)physicalRect.Right, (int)physicalRect.Bottom));
#pragma warning restore CS0618 // Type or member is obsolete

			var description = AutomationProperties.GetAutomationId(uiElement);
			var peer = uiElement.GetOrCreateAutomationPeer();

			if (string.IsNullOrEmpty(description))
			{
				description = peer?.GetName();
			}
			if (string.IsNullOrEmpty(description))
			{
				description = uiElement.GetType().Name;
			}

			// TODO: Scrolling?

			if (peer is not null)
			{
				if (peer is IInvokeProvider or IToggleProvider)
				{
					node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionClick);
				}
			}

			node.ContentDescription = description;
		}
	}
}
