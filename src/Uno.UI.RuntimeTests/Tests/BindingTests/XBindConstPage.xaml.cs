using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindConstPage : Page
{
	public const double MyWidth = 200;
	public const double MyHeight = 200;

	public XBindConstPage()
	{
		this.InitializeComponent();
	}

	public Border XBoundBorder => BoundBorder;
}
