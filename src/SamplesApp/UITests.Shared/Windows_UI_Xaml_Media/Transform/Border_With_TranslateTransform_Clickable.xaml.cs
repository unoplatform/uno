using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Media.Transform
{
	[SampleControlInfo("Transform", "Border_With_TranslateTransform_Clickable")]
	public sealed partial class Border_With_TranslateTransform_Clickable : UserControl
	{

		public Border_With_TranslateTransform_Clickable()
		{
			this.InitializeComponent();
		}

		private void Test1_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			Test1.Content = "Changed";
		}
	}
}
