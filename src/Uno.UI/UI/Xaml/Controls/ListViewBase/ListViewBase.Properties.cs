using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using NotImplementedException = System.NotImplementedException;

namespace Windows.UI.Xaml.Controls;

public partial class ListViewBase
{
	/// <summary>
	/// Gets or sets a value that indicates whether item selection changes when keyboard focus changes.
	/// </summary>
	public bool SingleSelectionFollowsFocus
	{
		get => (bool)GetValue(SingleSelectionFollowsFocusProperty);
		set => SetValue(SingleSelectionFollowsFocusProperty, value);
	}

	/// <summary>
	/// Identifies the SingleSelectionFollowsFocus dependency property.
	/// </summary>
	public static DependencyProperty SingleSelectionFollowsFocusProperty { get; } =
		DependencyProperty.Register(
			nameof(SingleSelectionFollowsFocus),
			typeof(bool),
			typeof(ListViewBase),
			new FrameworkPropertyMetadata(true));

	public bool IsMultiSelectCheckBoxEnabled
	{
		get => (bool)GetValue(IsMultiSelectCheckBoxEnabledProperty);
		set => SetValue(IsMultiSelectCheckBoxEnabledProperty, value);
	}

	public static DependencyProperty IsMultiSelectCheckBoxEnabledProperty { get; } =
		DependencyProperty.Register(
			nameof(IsMultiSelectCheckBoxEnabled), typeof(bool),
			typeof(ListViewBase),
			new FrameworkPropertyMetadata(true, (o, args) => ((ListViewBase)o).OnIsMultiSelectCheckBoxEnabledPropertyChanged(args)));

	private void OnIsMultiSelectCheckBoxEnabledPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		foreach (var item in GetItemsPanelChildren().OfType<SelectorItem>())
		{
			ApplyMultiSelectState(item);
		}
	}
}
