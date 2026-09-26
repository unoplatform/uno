using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

namespace Test02;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
	}

	public Color MyColor => Colors.DeepSkyBlue;
}
