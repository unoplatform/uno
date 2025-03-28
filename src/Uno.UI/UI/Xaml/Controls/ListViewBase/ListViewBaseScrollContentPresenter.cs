#if !IS_UNIT_TESTS && !UNO_REFERENCE_API && !__MACOS__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;

using Uno.Foundation.Logging;
#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
using Color = AppKit.NSColor;
using Font = AppKit.NSFont;
#endif

namespace Windows.UI.Xaml.Controls
{
	/// <summary>
	/// An Uno-only class which allows the <see cref="ScrollViewer"/> within a <see cref="ListViewBase"/> template
	/// to host a native collection view. 
	/// </summary>
	public sealed partial class ListViewBaseScrollContentPresenter : ScrollContentPresenter, INativeScrollContentPresenter
	{
		public ListViewBaseScrollContentPresenter()
		{
			Native = this;
		}

		ScrollBarVisibility INativeScrollContentPresenter.HorizontalScrollBarVisibility
		{
			get => NativePanel?.HorizontalScrollBarVisibility ?? ScrollBarVisibility.Auto;
			set
			{
				if (NativePanel != null)
				{
					NativePanel.HorizontalScrollBarVisibility = value;
				}
			}
		}

		ScrollBarVisibility INativeScrollContentPresenter.VerticalScrollBarVisibility
		{
			get => NativePanel?.VerticalScrollBarVisibility ?? ScrollBarVisibility.Auto;
			set
			{
				if (NativePanel != null)
				{
					NativePanel.VerticalScrollBarVisibility = value;
				}
			}
		}

		bool INativeScrollContentPresenter.CanHorizontallyScroll
		{
			get => NativePanel?.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled;
			set { }
		}

		bool INativeScrollContentPresenter.CanVerticallyScroll
		{
			get => NativePanel?.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled;
			set { }
		}

		private double _extentWidth;
		double INativeScrollContentPresenter.ExtentWidth
		{
			get => _extentWidth;
			set => _extentWidth = value;
		}

		private double _extentHeight;
		double INativeScrollContentPresenter.ExtentHeight
		{
			get => _extentHeight;
			set => _extentHeight = value;
		}

		internal NativeListViewBase NativePanel => (Content as ItemsPresenter)?.Panel as NativeListViewBase;

		public void OnMaxZoomFactorChanged(float newValue)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"{nameof(OnMaxZoomFactorChanged)} is not implemented for {nameof(ListViewBaseScrollContentPresenter)}");
			}
		}

		public void OnMinZoomFactorChanged(float newValue)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"{nameof(OnMinZoomFactorChanged)} is not implemented for {nameof(ListViewBaseScrollContentPresenter)}");
			}
		}
	}
}

#endif
