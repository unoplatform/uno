using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindOneWayLeakControl : UserControl
{
	public XBindOneWayLeakControl(Given_BindingSourceStaleHandlers.CountingViewModel vm)
	{
		VM = vm;
		this.InitializeComponent();
	}

	public Given_BindingSourceStaleHandlers.CountingViewModel VM { get; }

	public string ShownName => NameText.Text;
}
