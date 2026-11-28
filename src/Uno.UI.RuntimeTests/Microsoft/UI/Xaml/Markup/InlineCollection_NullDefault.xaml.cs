#nullable enable

using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Markup;

public sealed partial class InlineCollection_NullDefault : UserControl
{
	public InlineCollection_NullDefault()
	{
		InitializeComponent();
	}

	internal StackPanel RootPanelElement => RootPanel;

	internal ContentControl ContentHostElement => ContentHost;

	internal Border TransformedBorderElement => TransformedBorder;

	internal InlineCollectionHost HostElement => Host;
}

public partial class InlineCollectionHost : Control
{
	public InlineCollectionHost()
	{
		DefaultItems = InitialDefaultItems = new List<object>();
	}

	internal List<object> InitialDefaultItems { get; }

	public List<object>? NullDefaultItems { get; set; }

	public List<object> DefaultItems { get; set; }
}
