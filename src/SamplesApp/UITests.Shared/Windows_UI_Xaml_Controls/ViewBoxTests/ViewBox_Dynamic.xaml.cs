using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Samples.Content.UITests.ViewBoxTests
{
	[Sample("Viewbox")]
	public sealed partial class ViewBox_Dynamic : UserControl
	{
		public ViewBox_Dynamic()
		{
			this.InitializeComponent();
		}

		private void StretchDirectionButton_Checked(object sender, RoutedEventArgs e)
		{
			RadioButton rb = sender as RadioButton;

			if (rb != null && viewBox1 != null)
			{
				string direction = rb.Tag.ToString();
				switch (direction)
				{
					case "UpOnly":
						viewBox1.StretchDirection = StretchDirection.UpOnly;
						break;
					case "DownOnly":
						viewBox1.StretchDirection = StretchDirection.DownOnly;
						break;
					case "Both":
						viewBox1.StretchDirection = StretchDirection.Both;
						break;
				}
			}
		}

		private void StretchButton_Checked(object sender, RoutedEventArgs e)
		{
			RadioButton rb = sender as RadioButton;

			if (rb != null && viewBox1 != null)
			{
				string stretch = rb.Tag.ToString();
				switch (stretch)
				{
					case "None":
						viewBox1.Stretch = Stretch.None;
						break;
					case "Fill":
						viewBox1.Stretch = Stretch.Fill;
						break;
					case "Uniform":
						viewBox1.Stretch = Stretch.Uniform;
						break;
					case "UniformToFill":
						viewBox1.Stretch = Stretch.UniformToFill;
						break;
				}
			}
		}
	}
}
