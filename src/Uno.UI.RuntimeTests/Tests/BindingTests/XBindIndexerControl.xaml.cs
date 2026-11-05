using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindIndexerControl : UserControl
{
	public XBindIndexerControl(Given_BindingSourceStaleHandlers.CountingViewModel vm)
	{
		VM = vm;
		this.InitializeComponent();
	}

	public Given_BindingSourceStaleHandlers.CountingViewModel VM { get; }
}
