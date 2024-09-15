using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI;
using Windows.Foundation;

#if __ANDROID__
using Android.Views;
using View = Android.Views.View;
#elif __IOS__
using CoreGraphics;
using View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using CoreGraphics;
using View = AppKit.NSView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml;

internal static partial class MobileLayoutingHelpers
{
	public static Size MeasureElement(View view, Size availableSize)
	{
#if __CROSSRUNTIME__ || IS_UNIT_TESTS
		view.Measure(availableSize);
		return view.DesiredSize;
#else
		if (view is UIElement viewAsUIElement)
		{
			viewAsUIElement.Measure(availableSize);
			return viewAsUIElement.DesiredSize;
		}

		LayoutInformation.SetMeasureDirtyPath(view, false);
		if (view is ILayouterElement layouterElement)
		{
			var desiredSizeFromLayouterElement = layouterElement.Measure(availableSize);
			LayoutInformation.SetDesiredSize(view, desiredSizeFromLayouterElement);
			LayoutInformation.SetAvailableSize(view, availableSize);
			return desiredSizeFromLayouterElement;
		}

#if __ANDROID__
		var physical = availableSize.LogicalToPhysicalPixels();
		var widthSpec = ViewHelper.MakeMeasureSpec((int)physical.Width, Android.Views.MeasureSpecMode.AtMost);
		var heightSpec = ViewHelper.MakeMeasureSpec((int)physical.Height, Android.Views.MeasureSpecMode.AtMost);
		view.Measure(widthSpec, heightSpec);
		var desiredSize = Uno.UI.Controls.BindableView.GetNativeMeasuredDimensionsFast(view).PhysicalToLogicalPixels();
		LayoutInformation.SetDesiredSize(view, desiredSize);
		LayoutInformation.SetAvailableSize(view, availableSize);

		if (view is ViewGroup viewGroup)
		{
			var childCount = viewGroup.ChildCount;
			for (int i = 0; i < childCount; i++)
			{
				MeasureElement(viewGroup.GetChildAt(i), desiredSize);
			}
		}

		return desiredSize;
#elif __IOS__ || __MACOS__
		var physical = availableSize.LogicalToPhysicalPixels();

#if __IOS__
		var desiredSize = view.SizeThatFits(physical).PhysicalToLogicalPixels();
#else
		CGSize desiredSize = view switch
		{
			NSControl nsControl => nsControl.SizeThatFits(physical),
			IHasSizeThatFits hasSizeThatFits => hasSizeThatFits.SizeThatFits(physical),
			_ => view.FittingSize,
		};
		desiredSize = desiredSize.PhysicalToLogicalPixels();
#endif

		LayoutInformation.SetDesiredSize(view, desiredSize);
		LayoutInformation.SetAvailableSize(view, availableSize);
		foreach (var child in view.Subviews)
		{
			MeasureElement(child, desiredSize);
		}

		return desiredSize;
#else
#error Unrecognized platform
#endif
#endif
	}

	public static void ArrangeElement(View view, Rect finalRect)
	{
#if __CROSSRUNTIME__ || IS_UNIT_TESTS
		view.Arrange(finalRect);
#else
		if (view is UIElement viewAsUIElement)
		{
			viewAsUIElement.Arrange(finalRect);
			return;
		}

		LayoutInformation.SetLayoutSlot(view, finalRect);
		LayoutInformation.SetArrangeDirtyPath(view, false);
		if (view is ILayouterElement layouterElement)
		{
			layouterElement.Arrange(finalRect);
		}
		else
		{
			var physicalRect = ViewHelper.LogicalToPhysicalPixels(finalRect);
#if __ANDROID__
			view.Layout((int)physicalRect.Left, (int)physicalRect.Top, (int)physicalRect.Right, (int)physicalRect.Bottom);
#elif __IOS__ || __MACOS__
			view.Frame = physicalRect;
#endif
		}

		if (view is IFrameworkElement_EffectiveViewport evp)
		{
			evp.OnLayoutUpdated();
		}
#endif
	}
}
