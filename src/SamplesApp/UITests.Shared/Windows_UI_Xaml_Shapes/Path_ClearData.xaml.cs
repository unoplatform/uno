using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes", IgnoreInSnapshotTests = true, Description = "Automated test which validates that when we clear the Path.Data, it's no longer visible. You should see only one pink D (but 3 blue containers).")]
	public sealed partial class Path_ClearData : Page
	{
		public Path_ClearData()
		{
			this.InitializeComponent();
		}

		private void ClearData(object sender, RoutedEventArgs e)
		{
			((Windows.UI.Xaml.Shapes.Path)sender).Data = null;
		}
	}
}
