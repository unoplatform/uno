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
using Uno.UI.Xaml.Core;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class UnoExploreByTouchHelper : ExploreByTouchHelper
{
	private readonly UIElement _rootElement;
	private ConditionalWeakTable<UIElement, object> _cwtElementToId = new();
	private int _currentId;

	public UnoExploreByTouchHelper(View host, UIElement rootElement) : base(host)
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

		if (_cwtElementToId.TryGetValue(element, out var id))
		{
			return (int)id;
		}

		id = Interlocked.Increment(ref _currentId);
		_cwtElementToId.Add(element, id);
		return (int)id;
	}

	protected override void GetVisibleVirtualViews(IList<Integer> virtualViewIds)
	{
		var focusManager = VisualTree.GetFocusManagerForElement(_rootElement);
	}

	protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle arguments)
	{
		return false;
	}

	protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat node)
	{
		return false;
	}
}
