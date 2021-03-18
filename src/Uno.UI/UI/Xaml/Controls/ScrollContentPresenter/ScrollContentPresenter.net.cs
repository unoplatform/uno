using Uno.Extensions;
using Uno.Logging;
using Uno.UI.DataBinding;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Drawing;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter, IScrollContentPresenter
	{
		public bool CanHorizontallyScroll { get; set; }
		public bool CanVerticallyScroll { get; set; }

		void IScrollContentPresenter.OnMinZoomFactorChanged(float newValue) { }

		void IScrollContentPresenter.OnMaxZoomFactorChanged(float newValue) { }

		ScrollBarVisibility IScrollContentPresenter.VerticalScrollBarVisibility { get => VerticalScrollBarVisibility; set => VerticalScrollBarVisibility = value; }
		internal ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

		ScrollBarVisibility IScrollContentPresenter.HorizontalScrollBarVisibility { get => HorizontalScrollBarVisibility; set => HorizontalScrollBarVisibility = value; }
		internal ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
	}
}
