using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Toolkit
{
	[SampleControlInfo("Toolkit")]
	public sealed partial class ElevatedViewTests : Page
	{
		public ElevatedViewTests()
		{
			this.InitializeComponent();
		}

		private void SetElevationClick(object sender, RoutedEventArgs e)
		{
			if (sender is Button btn && btn.Tag != null)
			{
				elevation1.Value = Convert.ToDouble(btn.Tag);
			}
		}
	}
}
