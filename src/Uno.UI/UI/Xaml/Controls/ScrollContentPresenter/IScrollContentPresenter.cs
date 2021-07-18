#if !UNO_HAS_MANAGED_SCROLL_PRESENTER
using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// An interface consumed by <see cref="ScrollViewer"/>, which may contain either a <see cref="ScrollContentPresenter"/> (the
	/// normal case) or a <see cref="ListViewBaseScrollContentPresenter"/> (special case to handle usage within the template of 
	/// <see cref="ListViewBase"/>)
	/// </summary>
	internal partial interface IScrollContentPresenter
	{
		ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
		ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

		bool CanHorizontallyScroll { get; set; }
		bool CanVerticallyScroll { get; set; }

		/// <summary>
		/// The absolute dimensions of the content (or an estimate), for presenters where this is not simply equal to the Content element's size.
		/// </summary>
		Size? CustomContentExtent { get; }

		object Content { get; set; }

		void OnMinZoomFactorChanged(float newValue);
		void OnMaxZoomFactorChanged(float newValue);

		Rect MakeVisible(UIElement visual, Rect rectangle);
	}
}
#endif
