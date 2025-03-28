using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class BindingToSetterValuePage : Page
{
	public BindingToSetterValuePage()
	{
		this.InitializeComponent();
		this.DataContext = this;

		// Just to make sure x:Name is generated for these.
		_ = style1;
		_ = style2;
		_ = setter1;
		_ = setter2;
	}

	public string TestValue => "Hello";
}
