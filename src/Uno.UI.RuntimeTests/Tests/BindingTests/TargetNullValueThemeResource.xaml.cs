using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class TargetNullValueThemeResource : Page
{
	public TargetNullValueThemeResource()
	{
		this.InitializeComponent();
		myBtn.DataContext = myBtn;
	}
}
