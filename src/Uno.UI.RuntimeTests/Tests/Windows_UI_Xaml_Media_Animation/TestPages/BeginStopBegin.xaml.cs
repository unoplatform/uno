using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Animation.TestPages;

public sealed partial class BeginStopBegin : Page
{
	public BeginStopBegin()
	{
		this.InitializeComponent();
	}

	private void Start_Animation(object sender, RoutedEventArgs e)
	{
		myStoryboard.Begin();
		myStoryboard.Stop();
		myStoryboard.Begin();
	}
}
