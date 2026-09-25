using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

public sealed partial class OnApplyTemplateCounterContentControl : ContentControl
{
	public int ApplyTemplateCount { get; private set; }

	protected override void OnApplyTemplate()
	{
		ApplyTemplateCount++;
		base.OnApplyTemplate();
	}
}
