#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

public sealed partial class TextBox_XBind_TwoWay_Null_ViewModel : Page
{
	public TextBox_XBind_TwoWay_Null_ViewModel()
	{
		this.InitializeComponent();
	}

	public TextBox TextBox => textBox;

	public TextBox_XBind_TwoWay_Null_ViewModel_Data? ViewModel { get; set; } = new();

	public void UpdateBindings() => Bindings.Update();
}

public class TextBox_XBind_TwoWay_Null_ViewModel_Data
{
	public string Text { get; set; } = "";
}
