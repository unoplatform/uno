using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

public sealed partial class PlainName_GetOnly : UserControl
{
	public PlainName_GetOnly()
	{
		InitializeComponent();
	}

#if HAS_UNO
	internal Run PlainRunElement => PlainRun;

	internal Hyperlink PlainLinkElement => PlainLink;
#endif
}
