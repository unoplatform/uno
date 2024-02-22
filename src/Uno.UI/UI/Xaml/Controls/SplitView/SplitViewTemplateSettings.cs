using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public sealed partial class SplitViewTemplateSettings : DependencyObject
	{
		internal SplitViewTemplateSettings()
		{
		}

		public GridLength CompactPaneGridLength { get; internal set; }
		public GridLength OpenPaneGridLength { get; internal set; }
		public double NegativeOpenPaneLength { get; internal set; }
		public double NegativeOpenPaneLengthMinusCompactLength { get; internal set; }
		public double OpenPaneLengthMinusCompactLength { get; internal set; }

		public double OpenPaneLength { get; internal set; }
		public double CompactPaneLength { get; internal set; }
	}
}
