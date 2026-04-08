using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

public sealed partial class When_xBind_Null_String : UserControl
{
	public string NullStringProperty { get; set; }

	public When_xBind_Null_String()
	{
		this.InitializeComponent();
	}
}

/// <summary>
/// A simple FrameworkElement with a CLR string property to receive x:Bind values.
/// The property defaults to a sentinel value so we can distinguish between
/// "not set" and "set to null" and "set to empty string".
/// </summary>
public class StringReceiverControl : FrameworkElement
{
	public const string Sentinel = "__NOT_SET__";
	public string StringValue { get; set; } = Sentinel;
}
