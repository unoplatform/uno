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
		// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE
		//
		// This interface is obsolete and should not used anymore.
		// The logic is being move into the SCP which then re-routes to the INativeScrollContentPresenter
		// 
		// NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE NOTE 

		ScrollBarVisibility NativeHorizontalScrollBarVisibility { set; }
		ScrollBarVisibility NativeVerticalScrollBarVisibility { set; }

		bool CanHorizontallyScroll { get; set; }
		bool CanVerticallyScroll { get; set; }

		/// <summary>
		/// The absolute dimensions of the content (or an estimate), for presenters where this is not simply equal to the Content element's size.
		/// </summary>
		Size? CustomContentExtent { get; }

		object Content { get; set; }

		void OnMinZoomFactorChanged(float newValue);
		void OnMaxZoomFactorChanged(float newValue);
	}
}
#endif
