using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public sealed partial class When_XBind_TargetDisposed : Page
{
	public When_XBind_TargetDisposed_VM ViewModel { get; }

	public When_XBind_TargetDisposed()
	{
		ViewModel = new();
		this.InitializeComponent();
	}
	//~When_XBind_TargetDisposed()
	//{
	//}

	//protected override void Dispose(bool disposing)
	//{
	//	base.Dispose(disposing);
	//}
}

public class When_XBind_TargetDisposed_VM
{
	public ObservableCollection<string> Items { get; }

	public When_XBind_TargetDisposed_VM()
	{
		Items = new(Enumerable.Range(0, 10).Select(x => $"Item {x}"));
	}
}
