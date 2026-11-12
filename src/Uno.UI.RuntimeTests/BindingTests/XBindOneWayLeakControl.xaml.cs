using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindOneWayLeakControl : UserControl
{
	public XBindOneWayLeakControl(Windows_UI_Xaml.Given_BindingSourceStaleHandlers.CountingViewModel vm)
	{
		VM = vm;
		this.InitializeComponent();
	}

	public Windows_UI_Xaml.Given_BindingSourceStaleHandlers.CountingViewModel VM { get; }

	public string ShownName => NameText.Text;
}
