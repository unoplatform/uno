using Uno.UI.Samples.Controls;
using Uno.UI.Samples.Presentation.SamplePages;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Samples.Content.UITests.ButtonTestsControl
{
	[Sample("Buttons", Name = nameof(AppBarButtonTest), ViewModelType = typeof(ButtonTestsViewModel))]
	public sealed partial class AppBarButtonTest : UserControl
	{
		public AppBarButtonTest()
		{
			this.InitializeComponent();
		}
	}
}
