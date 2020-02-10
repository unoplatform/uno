using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UIKit;
using CoreGraphics;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class DatePicker
	{

		/// <summary>
		/// iOS-specific property that allows apps to specify any flyout placement 
		/// (especially FlyoutPlacementMode.Full, which is commonly used on iPhone)
		/// </summary>
		public FlyoutPlacementMode FlyoutPlacement { get; set; }
	}
}
