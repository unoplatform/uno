using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests;

public sealed partial class XBindConstControl : Control
{
	private const double MyWidth = 200;
	private const double MyHeight = 200;

	public XBindConstControl()
	{
		DefaultStyleKey = typeof(XBindConstControl);

		this.InitializeComponent();
	}

	public Border XBoundBorder { get; set; }

	protected override void OnApplyTemplate()
	{
		base.OnApplyTemplate();

		XBoundBorder = GetTemplateChild("BoundBorder") as Border;


	}
}
