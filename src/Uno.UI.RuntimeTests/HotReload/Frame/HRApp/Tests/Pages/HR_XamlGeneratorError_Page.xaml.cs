using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

public sealed partial class HR_XamlGeneratorError_Page : Page
{
	public HR_XamlGeneratorError_Page()
	{
		this.InitializeComponent();
	}

	public string Marker => "Original marker";
}
