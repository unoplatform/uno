using System;
using System.Numerics;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Composition
{
	[Sample("Windows.UI.Composition", Description = "The two drawings should be identical")]
	public sealed partial class MixTransformCliPropertyAndClippedByParentWithBorders_Then_RenderingIsValid : UserControl
	{
		public MixTransformCliPropertyAndClippedByParentWithBorders_Then_RenderingIsValid()
		{
			this.InitializeComponent();
		}
	}
}
