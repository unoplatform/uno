using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

public sealed partial class MarkupExtension_CodegenTypeCast : Page
{
	public MarkupExtension_CodegenTypeCast()
	{
		this.InitializeComponent();
	}
}
public partial class AlreadyCheckedBox : CheckBox
{
	public AlreadyCheckedBox()
	{
		// setting a default value, so we may observe ReturnNullExtension working or not.
		this.IsChecked = true;
	}
}

public class ReturnNullExtension : MarkupExtension
{
	protected override object ProvideValue() => null;
}
