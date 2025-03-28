using Android.Views;
using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class FlyoutBase
	{
		internal virtual View NativeTarget => null;

		partial void InitializePopupPanelPartial()
		{
			_popup.PopupPanel = new FlyoutBasePopupPanel(this)
			{
				Visibility = Visibility.Collapsed,
				Background = SolidColorBrushHelper.Transparent,
			};
		}



	}
}
