using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class BindingToSetterValuePage : Page
{
	public BindingToSetterValuePage()
	{
		this.InitializeComponent();
		this.DataContext = this;
	}

	public string TestValue => "Hello";
}
