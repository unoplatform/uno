using Uno.UI.Samples.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UITests.Windows_UI_Xaml_Controls.BorderTests;

[Sample("Border")]
public sealed partial class PanelWithNullBrushAndNonZeroThickness : Page
{
	private bool _stateBrush;
	private bool _stateThickness;

	public PanelWithNullBrushAndNonZeroThickness()
	{
		this.InitializeComponent();
	}

	private void Button_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
	{
		if (_stateBrush)
		{
			border1.BorderBrush = null;
			border2.BorderBrush = null;
			border3.BorderBrush = new SolidColorBrush(Colors.Red);
			border4.BorderBrush = new SolidColorBrush(Colors.Red);
		}
		else
		{
			border1.BorderBrush = new SolidColorBrush(Colors.Red);
			border2.BorderBrush = new SolidColorBrush(Colors.Red);
			border3.BorderBrush = null;
			border4.BorderBrush = null;
		}

		_stateBrush = !_stateBrush;
	}

	private void Button2_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
	{
		if (_stateThickness)
		{
			border1.BorderThickness = new Thickness(10);
			border2.BorderThickness = new Thickness(30);
			border3.BorderThickness = new Thickness(10);
			border4.BorderThickness = new Thickness(30);
		}
		else
		{
			border1.BorderThickness = new Thickness(30);
			border2.BorderThickness = new Thickness(10);
			border3.BorderThickness = new Thickness(30);
			border4.BorderThickness = new Thickness(10);
		}

		_stateThickness = !_stateThickness;
	}
}
