#if XAMARIN
namespace Windows.UI.Xaml.Controls
{
	public  partial class CommandBarOverflowPresenter : ItemsControl
	{
		public CommandBarOverflowPresenter() : base()
		{
			ItemsPanel = new ItemsPanelTemplate(() => new StackPanel());
		}
	}
}
#endif