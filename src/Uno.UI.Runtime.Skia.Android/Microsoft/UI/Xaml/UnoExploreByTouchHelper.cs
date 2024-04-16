using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Android.OS;
using Android.Views;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Java.Lang;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using SkiaSharp.Views.Android;
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

	public UnoExploreByTouchHelper(SKCanvasView host, UIElement rootElement) : base(host)
	{
		_rootElement = rootElement;
	}

	protected override int GetVirtualViewAt(float x, float y)
	{
		var (element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(x, y).PhysicalToLogicalPixels(), _rootElement.XamlRoot);
		element ??= _rootElement;
		while (!element.IsFocusable)
		{
			element = element.GetUIElementAdjustedParentInternal();
			if (element is null)
			{
				return ExploreByTouchHelper.HostId;
			}
		}

		return GetOrCreateVirtualId(element);
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

		var elements = XYFocusTreeWalker.FindElements(startRoot: _rootElement, currentElement: null, activeScroller: null, ignoreClipping: false, shouldConsiderXYFocusKeyboardNavigation: false);
		foreach (var element in elements)
		{
			if (element.Element is not null)
			{
				virtualViewIds.Add(Integer.ValueOf(GetOrCreateVirtualId(element.Element)));
			}
		}
	}

	protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle arguments)
	{
		return false;
	}

	protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat node)
	{
		if (_idToElement.TryGetValue(virtualViewId, out var element) &&
			element is UIElement uiElement)
		{
			var slot = uiElement.LayoutSlot.LogicalToPhysicalPixels();
#pragma warning disable CS0618 // Type or member is obsolete
			node.SetBoundsInParent(new global::Android.Graphics.Rect((int)slot.Left, (int)slot.Top, (int)slot.Right, (int)slot.Bottom));
#pragma warning restore CS0618 // Type or member is obsolete
			node.ContentDescription = uiElement.GetOrCreateAutomationPeer()?.GetName() ?? uiElement.GetType().Name;
		}
		else
		{
			node.ContentDescription = "Unknown";
		}
	}
}
