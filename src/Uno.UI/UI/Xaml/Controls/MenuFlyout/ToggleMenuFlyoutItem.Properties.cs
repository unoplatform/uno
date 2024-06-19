using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class ToggleMenuFlyoutItem
{
	/// <summary>
	/// Gets or sets whether the ToggleMenuFlyoutItem is checked.
	/// </summary>
	public bool IsChecked
	{
		get => (bool)GetValue(IsCheckedProperty);
		set => SetValue(IsCheckedProperty, value);
	}

	/// <summary>
	/// Identifies the IsChecked dependency property.
	/// </summary>
	public static DependencyProperty IsCheckedProperty { get; } =
		DependencyProperty.Register(
			nameof(IsChecked),
			typeof(bool),
			typeof(ToggleMenuFlyoutItem),
			new FrameworkPropertyMetadata(
				defaultValue: false,
				propertyChangedCallback: (s, e) => (s as ToggleMenuFlyoutItem)?.OnIsCheckedChanged((bool)e.OldValue, (bool)e.NewValue)));
}
