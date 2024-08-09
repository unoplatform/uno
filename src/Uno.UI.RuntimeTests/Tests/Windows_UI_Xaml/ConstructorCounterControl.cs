using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[Bindable]
public sealed partial class ConstructorCounterControl : Control
{
	public static int ConstructorCount { get; private set; }
	public static int ApplyTemplateCount { get; private set; }

	public ConstructorCounterControl()
	{
		ConstructorCount++;
	}

	protected override void OnApplyTemplate()
	{
		ApplyTemplateCount++;
	}

	public static void Reset()
	{
		ConstructorCount = 0;
		ApplyTemplateCount = 0;
	}
}
