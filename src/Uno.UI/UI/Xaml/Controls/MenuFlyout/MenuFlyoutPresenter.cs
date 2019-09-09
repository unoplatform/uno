using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public  partial class MenuFlyoutPresenter : global::Windows.UI.Xaml.Controls.ItemsControl
	{
		public global::Windows.UI.Xaml.Controls.Primitives.MenuFlyoutPresenterTemplateSettings TemplateSettings { get; } = new MenuFlyoutPresenterTemplateSettings();

		public MenuFlyoutPresenter() : base()
		{
		}
	}
}
