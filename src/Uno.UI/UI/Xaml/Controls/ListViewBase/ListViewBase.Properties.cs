#nullable disable

using Windows.UI.Xaml;

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
}
