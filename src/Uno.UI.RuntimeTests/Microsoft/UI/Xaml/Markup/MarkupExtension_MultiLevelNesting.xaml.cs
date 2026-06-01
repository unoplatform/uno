using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

public sealed partial class MarkupExtension_MultiLevelNesting : Page
{
	public MarkupExtension_MultiLevelNesting()
	{
		this.InitializeComponent();
	}
}

public class NestedExtension : MarkupExtension
{
	public object Value { get; set; }

	protected override object ProvideValue() => Value;
}

public class TargetValueExtension : MarkupExtension
{
	protected override object ProvideValue() => nameof(TargetValueExtension);
}
