#if !NET461 && !NETSTANDARD2_0 && !__MACOS__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions;
using Microsoft.Extensions.Logging;
using Uno.Logging;
#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
#elif XAMARIN_IOS_UNIFIED
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
	public sealed partial class ListViewBaseScrollContentPresenter : ContentPresenter, IScrollContentPresenter
	{
		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get
			{
				return NativePanel?.HorizontalScrollBarVisibility ?? ScrollBarVisibility.Auto;
			}

			set
			{
				if (NativePanel != null)
				{
					NativePanel.HorizontalScrollBarVisibility = value;
				}
			}
		}

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get
			{
				return NativePanel?.VerticalScrollBarVisibility ?? ScrollBarVisibility.Auto;
			}

			set
			{
				if (NativePanel != null)
				{
					NativePanel.VerticalScrollBarVisibility = value;
				}
			}
		}

		View IScrollContentPresenter.Content
		{
			get { return Content as View; }
			set { Content = value; }
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
