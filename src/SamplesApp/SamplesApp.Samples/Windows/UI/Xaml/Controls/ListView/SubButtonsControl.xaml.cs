using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	public sealed partial class SubButtonsControl : UserControl
	{
		public SubButtonsControl()
		{
			this.InitializeComponent();
		}

		private static readonly SolidColorBrush _red = new SolidColorBrush(Colors.Red);
		private static readonly SolidColorBrush _blue = new SolidColorBrush(Colors.Blue);
		private void ChangeRectangleColor(object sender, RoutedEventArgs e)
		{
			Rectangle target;
			if ((sender as Button).Name.StartsWith("Left"))
			{
				target = LeftRectangle;
			}
			else
			{
				target = RightRectangle;
			}

			if (target.Fill?.Equals(_red) ?? false)
			{
				target.Fill = _blue;
			}
			else
			{
				target.Fill = _red;
			}
		}
	}
}
