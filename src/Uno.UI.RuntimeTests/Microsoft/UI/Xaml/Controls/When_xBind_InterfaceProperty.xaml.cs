using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public sealed partial class When_xBind_InterfaceProperty : Page
{
	public When_xBind_InterfaceProperty()
	{
		Model = new XBindInterfacePropertyModel();
		this.InitializeComponent();
	}

	public XBindInterfacePropertyModel Model { get; }
}

public class XBindInterfacePropertyModel
{
	// Backed by an array, which implements IReadOnlyList<T>.Count without declaring Count itself.
	public IReadOnlyList<string> Items { get; } = new[] { "Item1", "Item2", "Item3" };

	public IReadOnlyList<string> ListItems { get; } = new List<string> { "ListItem1", "ListItem2" };
}
