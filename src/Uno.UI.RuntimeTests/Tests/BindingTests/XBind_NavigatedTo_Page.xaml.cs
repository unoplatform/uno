using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBind_NavigatedTo_Page : Page
{
	public string BoundText { get; set; } = "initial";

	public XBind_NavigatedTo_Page()
	{
		InitializeComponent();
		DataContextChanged += (s, e) =>
		{
			BoundText = "datacontext-changed";
			Bindings.Update();
		};
	}

	protected internal override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		DataContext = new object();
	}
}
