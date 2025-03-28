#if !__CROSSRUNTIME__
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Uno.Collections;

namespace Windows.UI.Xaml.Controls.Primitives;

partial class LayoutInformation
{
	private static readonly UnsafeWeakAttachedDictionary<object, string> _layoutProperties = new();

	#region AvailableSize

	internal static Size GetAvailableSize(object view)
		=> view is UIElement iue
			? GetAvailableSize(iue)
			: _layoutProperties.GetValue(view, "availablesize", () => default(Size));

	internal static void SetAvailableSize(object view, Size value)
	{
		if (view is UIElement iue)
		{
			iue.m_previousAvailableSize = value;
		}
		else
		{
			_layoutProperties.SetValue(view, "availablesize", value);
		}
	}
	#endregion

	#region LayoutSlot

	internal static Rect GetLayoutSlot(object view)
		=> view is UIElement iue
			? GetLayoutSlot(iue)
			: _layoutProperties.GetValue(view, "layoutslot", () => default(Rect));

	internal static void SetLayoutSlot(object view, Rect value)
	{
		if (view is UIElement iue)
		{
			iue.m_finalRect = value;
		}
		else
		{
			_layoutProperties.SetValue(view, "layoutslot", value);
		}
	}
	#endregion

	#region DesiredSize
	internal static Size GetDesiredSize(UIElement element)
		=> element.m_desiredSize;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static Size GetDesiredSize(object view)
	{
		switch (view)
		{
			case UIElement iue:
				return iue.m_desiredSize;
			default:
				return _layoutProperties.GetValue(view, "desiredSize", () => default(Size));
		}
	}

	internal static void SetDesiredSize(object view, Size desiredSize)
	{
		switch (view)
		{
			case UIElement iue:
				iue.m_desiredSize = desiredSize;
				break;
			default:
				_layoutProperties.SetValue(view, "desiredSize", desiredSize);
				break;
		}
	}
	#endregion
}
#endif
