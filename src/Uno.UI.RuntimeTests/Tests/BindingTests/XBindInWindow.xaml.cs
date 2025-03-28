using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindInWindow : Window
{
	public int ClickCount { get; private set; }

	public XBindInWindow()
	{
		this.InitializeComponent();
	}

	public void Click(object sender, RoutedEventArgs args)
	{
		ClickCount++;
	}
}
