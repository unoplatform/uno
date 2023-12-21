using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Drawing;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ScrollContentPresenter : ContentPresenter
	{
		public bool CanHorizontallyScroll { get; set; }

		public bool CanVerticallyScroll { get; set; }

		public double ExtentHeight { get; internal set; }

		public double ExtentWidth { get; internal set; }

		internal ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

		internal ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }

		private object RealContent { get; set; }
	}
}
