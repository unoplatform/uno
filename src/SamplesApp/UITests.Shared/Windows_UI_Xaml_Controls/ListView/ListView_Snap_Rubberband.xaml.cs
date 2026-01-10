using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Tests;
using Uno.UI.Samples.UITests.Helpers;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", nameof(ListView_Snap_Rubberband), IsManualTest = true, Description = SampleDescription)]
	public sealed partial class ListView_Snap_Rubberband : UserControl
	{
		private const string SampleDescription =
			"[ManualTest]: While the LV is snapped to the 1st item, wait at least 1second. " +
			"Using flipping gesture, over-scroll to left, and then quickly (within 250ms) scroll to the right. " +
			"The LV should snap to the 2nd item, and not rubber banding back to the first.";

		public ListView_Snap_Rubberband()
		{
			this.InitializeComponent();
			this.DataContext = new[] { 0, 1, 2 };
		}
	}
}
