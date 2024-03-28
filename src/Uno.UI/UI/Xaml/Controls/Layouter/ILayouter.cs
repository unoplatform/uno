using Windows.Foundation;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	internal interface ILayouter
	{
		/// <summary>
		/// Measures the current layout
		/// </summary>
		/// <param name="availableSize">The available size in virtual pixels</param>
		/// <returns>The measured size in virtual pixels</returns>
		Size Measure(Size availableSize);

		/// <summary>
		/// Arranges the current layout.
		/// </summary>
		/// <param name="finalSize">The maximum size to use when arranging the layout</param>
		void Arrange(Rect finalRect);

		/// <summary>
		/// Measures the specified child.
		/// </summary>
		/// <param name="view">The view to measure</param>
		/// <param name="slotSize">The maximum size the child can use.</param>
		/// <returns>The size the view requires.</returns>
		/// <remarks>
		/// Provides the ability for external implementations to measure children.
		/// Mainly used for compatibility with existing WPF/WinRT implementations.
		/// </remarks>
		Size MeasureChild(View view, Size slotSize);

		/// <summary>
		/// Arranges the specified view.
		/// </summary>
		/// <param name="view">The view to arrange</param>
		/// <param name="frame">The frame available for the child.</param>
		/// <remarks>
		/// Provides the ability for external implementations to measure children.
		/// Mainly used for compatibility with existing WPF/WinRT implementations.
		/// </remarks>
		void ArrangeChild(View view, Rect frame);

		/// <summary>
		/// Provides the desired size of the element, from the last measure phase.
		/// </summary>
		/// <param name="element">The element to get the measured with</param>
		/// <returns>The measured size</returns>
		Size GetDesiredSize(View view);
	}
}
