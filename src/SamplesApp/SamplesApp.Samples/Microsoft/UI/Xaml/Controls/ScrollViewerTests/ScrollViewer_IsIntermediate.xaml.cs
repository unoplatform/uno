using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling", IsManualTest = true)]
	public sealed partial class ScrollViewer_IsIntermediate : Page
	{
		public ScrollViewer_IsIntermediate()
		{
			this.InitializeComponent();
		}

		private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			statusTb.Text = $"Last ScrollViewerViewChangedEventArgs.IsIntermediate = {e.IsIntermediate}";
		}
	}
}
