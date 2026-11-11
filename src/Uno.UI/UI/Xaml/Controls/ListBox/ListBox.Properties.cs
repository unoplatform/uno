using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ListBox
{
	/// <summary>
	/// Gets or sets the selection behavior for the ListBox control.
	/// </summary>
	public SelectionMode SelectionMode
	{
		get => GetSelectionModeValue();
		set => SetSelectionModeValue(value);
	}

	/// <summary>
	/// Identifies the SelectionMode dependency property.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = SelectionMode.Single, ChangedCallback = true)]
	public static DependencyProperty SelectionModeProperty { get; } = CreateSelectionModeProperty();

	/// <summary>
	/// Gets or sets a value that indicates whether item selection changes when keyboard focus changes.
	/// </summary>
	public bool SingleSelectionFollowsFocus
	{
		get => GetSingleSelectionFollowsFocusValue();
		set => SetSingleSelectionFollowsFocusValue(value);
	}

	/// <summary>
	/// Identifies the SingleSelectionFollowsFocus dependency property.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = true)]
	public static DependencyProperty SingleSelectionFollowsFocusProperty { get; } = CreateSingleSelectionFollowsFocusProperty();
}
