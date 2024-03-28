using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.HotReload.Frame.Pages;

public sealed partial class HR_Frame_Pages_Scroll : Page
{
	public HR_Frame_Pages_Scroll()
	{
		this.InitializeComponent();
	}

	public ScrollViewer ScrollViewer => sv;
	public StackPanel StackPanel => sp;
}
