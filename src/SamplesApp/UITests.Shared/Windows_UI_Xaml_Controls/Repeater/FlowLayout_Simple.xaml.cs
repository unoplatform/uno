using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.Repeater
{
	[Sample("ItemsRepeater", IgnoreInSnapshotTests = true)]
	public sealed partial class FlowLayout_Simple : Page
	{
		public FlowLayout_Simple()
		{
			this.InitializeComponent();
		}

		public int[] Items { get; } = Enumerable.Range(1, 5000).ToArray();

		private void Scroll(object server, RoutedEventArgs routedEventArgs)
		{
			if (server is Button btn)
			{
				if (double.TryParse(btn.Tag as string, out var pct))
				{
					if (layout.Orientation == Orientation.Horizontal)
					{
						var offset = RepeaterScrollViewer.ScrollableHeight * pct;
						RepeaterScrollViewer.ChangeView(null, offset, null, true);
					}
					else
					{
						var offset = RepeaterScrollViewer.ScrollableWidth * pct;
						RepeaterScrollViewer.ChangeView(offset, null, null, true);
					}
				}
			}
		}

		private void Tree(object server, RoutedEventArgs routedEventArgs)
		{
#if !WINAPPSDK && (HAS_UNO || HAS_UNO_WINUI)

			var txt = this.ShowLocalVisualTree(0);
			Console.WriteLine(txt);
#endif
		}

	}
}
