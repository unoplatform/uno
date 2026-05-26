using System.Collections.Generic;
using System.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls;

public sealed partial class XBind_InterfaceMember : Page
{
	public InterfaceMemberViewModel VM { get; } = new();

	public XBind_InterfaceMember()
	{
		this.InitializeComponent();
	}
}

public class InterfaceMemberViewModel : INotifyPropertyChanged
{
	public event PropertyChangedEventHandler PropertyChanged;

	// Declared as the interface type, but backed by an array. The array's Count is an explicitly
	// implemented IReadOnlyCollection<T>.Count (the concrete type only exposes Length), which is
	// exactly the scenario from issue #22223.
	private IReadOnlyList<int> _items = new[] { 1, 2, 3 };

	public IReadOnlyList<int> Items
	{
		get => _items;
		set
		{
			_items = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
		}
	}

	public NestedItemsModel Child { get; set; } = new();
}

public class NestedItemsModel
{
	// Same interface-typed-backed-by-array scenario, one level deeper, to exercise a multi-segment
	// compiled getter chain ending on an explicitly-implemented interface member.
	public IReadOnlyList<int> Items { get; set; } = new[] { 7, 8 };
}
