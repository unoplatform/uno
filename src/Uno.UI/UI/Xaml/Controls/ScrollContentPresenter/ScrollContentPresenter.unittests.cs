using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, IScrollContentPresenter
	{
		public bool CanHorizontallyScroll { get; set; }
		public bool CanVerticallyScroll { get; set; }

		public double ExtentHeight { get; internal set; }
		public double ExtentWidth { get; internal set; }

		private object RealContent => Content;

		Size? IScrollContentPresenter.CustomContentExtent => null;

		void IScrollContentPresenter.OnMinZoomFactorChanged(float newValue) { }

		void IScrollContentPresenter.OnMaxZoomFactorChanged(float newValue) { }

		ScrollBarVisibility IScrollContentPresenter.NativeVerticalScrollBarVisibility { set => VerticalScrollBarVisibility = value; }
		internal ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

		ScrollBarVisibility IScrollContentPresenter.NativeHorizontalScrollBarVisibility { set => HorizontalScrollBarVisibility = value; }
		internal ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
	}
}
