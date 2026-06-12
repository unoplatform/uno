using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindConstPage : Page
{
	private const double MyWidth = 200;
	private const double MyHeight = 200;

	public XBindConstPage()
	{
		this.InitializeComponent();
	}

	public Border XBoundBorder => BoundBorder;
}
