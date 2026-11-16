using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public sealed partial class When_xBind_With_Cast_Default_Namespace : Page
{
	public ViewModel ViewModel { get; } = new();

	public When_xBind_With_Cast_Default_Namespace()
	{
		this.DataContext = ViewModel;
		this.InitializeComponent();
	}
}

public class ViewModel
{
	public ContentControl P => new ContentControl() { Content = "Hello" };
}
