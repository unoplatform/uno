using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class VisualStateSetterBindingsPage : Page
{
	public VisualStateSetterBindingsPage()
	{
		this.InitializeComponent();
		Root.DataContext = this;
	}

	public double SideOpacity => 0.5;

	public double SideWidth => 120;
}
