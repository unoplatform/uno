namespace Microsoft.UI.Xaml.Controls;

partial class MenuFlyout
{
    public IList<MenuFlyoutItemBase> Items
	{
		get => (IList<MenuFlyoutItemBase>)this.GetValue(ItemsProperty);
		private set => this.SetValue(ItemsProperty, value);
	}

	public static DependencyProperty ItemsProperty { get; } =
		DependencyProperty.Register(
			nameof(Items),
			typeof(IList<MenuFlyoutItemBase>),
			typeof(MenuFlyout),
			new FrameworkPropertyMetadata(defaultValue: null));

	public Style MenuFlyoutPresenterStyle
	{
		get => (Style)this.GetValue(MenuFlyoutPresenterStyleProperty);
		set => SetValue(MenuFlyoutPresenterStyleProperty, value);
	}

	public static DependencyProperty MenuFlyoutPresenterStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(MenuFlyoutPresenterStyle),
			typeof(Style),
			typeof(MenuFlyout),
			new FrameworkPropertyMetadata(
				null, 
				FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));
}
