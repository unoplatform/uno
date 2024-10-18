using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Controls.BorderTests
{
	[Sample("Border", Description = "Setting the CornerRadius even by the smallest amount will cause the contents of the border to clip, even if the border thickness is zero or the border brush is null.")]
	public sealed partial class Border_CornerRadius_Clipping2 : UserControl
	{
		public Border_CornerRadius_Clipping2()
		{
			this.InitializeComponent();
		}
	}
}
