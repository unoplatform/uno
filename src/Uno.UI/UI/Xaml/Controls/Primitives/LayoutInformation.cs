using System;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives;

partial class LayoutInformation
{
	public static Size GetAvailableSize(UIElement element)
	{
		ArgumentNullException.ThrowIfNull(element);

		if (element.HasLayoutStorage)
		{
			return element.m_previousAvailableSize;
		}

		return default;
	}

	public static Rect GetLayoutSlot(FrameworkElement element)
	{
		ArgumentNullException.ThrowIfNull(element);

		if (element.HasLayoutStorage)
		{
			return element.m_finalRect;
		}

		return default;
	}

	internal static Rect GetLayoutSlot(UIElement element)
	{
		ArgumentNullException.ThrowIfNull(element);

		if (element.HasLayoutStorage)
		{
			return element.m_finalRect;
		}

		return default;
	}
}
