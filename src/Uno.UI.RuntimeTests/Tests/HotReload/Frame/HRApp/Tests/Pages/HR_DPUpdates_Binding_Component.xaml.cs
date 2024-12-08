using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

public sealed partial class HR_DPUpdates_Binding_Component : UserControl
{
	public HR_DPUpdates_Binding_Component()
	{
		this.InitializeComponent();
		tb.DataContext = this;
	}
}
