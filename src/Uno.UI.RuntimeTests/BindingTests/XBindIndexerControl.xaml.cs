using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindIndexerControl : UserControl
{
	public XBindIndexerControl(Windows_UI_Xaml.Given_BindingSourceStaleHandlers.CountingViewModel vm)
	{
		VM = vm;
		this.InitializeComponent();
	}

	public Windows_UI_Xaml.Given_BindingSourceStaleHandlers.CountingViewModel VM { get; }
}
