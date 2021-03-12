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
	public partial class ScrollContentPresenter : ContentPresenter
	{
		public bool CanHorizontallyScroll { get; set; }

		public bool CanVerticallyScroll { get; set; }

		internal ScrollBarVisibility VerticalScrollBarVisibility { get; set; }

		internal ScrollBarVisibility HorizontalScrollBarVisibility { get; set; }
	}
}
