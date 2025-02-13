using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyout
{
	/// <summary>
	/// Gets the collection used to generate the content of the menu.
	/// </summary>
	public IList<MenuFlyoutItemBase> Items
	{
		get => (IList<MenuFlyoutItemBase>)this.GetValue(ItemsProperty);
		private set => this.SetValue(ItemsProperty, value);
	}

	/// <summary>
	/// Identifies the Items dependency property.
	/// </summary>
	internal static DependencyProperty ItemsProperty { get; } =
		DependencyProperty.Register(
			nameof(Items),
			typeof(IList<MenuFlyoutItemBase>),
			typeof(MenuFlyout),
			new FrameworkPropertyMetadata(defaultValue: null));

	/// <summary>
	/// Gets or sets the style that is used when rendering the MenuFlyout.
	/// </summary>
	public Style MenuFlyoutPresenterStyle
	{
		get => (Style)this.GetValue(MenuFlyoutPresenterStyleProperty);
		set => SetValue(MenuFlyoutPresenterStyleProperty, value);
	}

	/// <summary>
	/// Identifies the MenuFlyoutPresenterStyle dependency property.
	/// </summary>
	public static DependencyProperty MenuFlyoutPresenterStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(MenuFlyoutPresenterStyle),
			typeof(Style),
			typeof(MenuFlyout),
			new FrameworkPropertyMetadata(
				null,
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));
}
