using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling", Name = nameof(ScrollViewer_InertiaDirectionChange), IsManualTest = true, IgnoreInSnapshotTests = true)]
	public sealed partial class ScrollViewer_InertiaDirectionChange : Page
	{
		public ScrollViewer_InertiaDirectionChange()
		{
			this.InitializeComponent();

			var panel = (StackPanel)SV.Content;
			var colors = new[] { Colors.LightBlue, Colors.LightCoral, Colors.LightGreen, Colors.LightGoldenrodYellow, Colors.LightPink, Colors.LightSalmon };
			for (var i = 0; i < 120; i++)
			{
				panel.Children.Add(new Border
				{
					Background = new SolidColorBrush(colors[i % colors.Length]),
					Child = new TextBlock { Text = $"Item {i}", VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center, Margin = new Microsoft.UI.Xaml.Thickness(12, 0, 0, 0) }
				});
			}
		}
	}
}
