using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	[Sample("Buttons", "Button_Events")]
	public sealed partial class Button_Events : Page
	{
		private int _btnTappedCount;
		private int _btnDoubleTappedCount;
		private int _btnClickCount;
		private int _btnPointerPressedCount;
		private int _btnPointerReleasedCount;
		private int _btnPointerEnteredCount;
		private int _btnPointerExitedCount;

		public Button_Events()
		{
			this.InitializeComponent();

			btnTapped.Tapped += (snd, evt) => btnTappedCount.Text = (++_btnTappedCount).ToString();
			btnDoubleTapped.DoubleTapped += (snd, evt) => btnDoubleTappedCount.Text = (++_btnDoubleTappedCount).ToString();
			btnClick.Click += (snd, evt) => btnClickCount.Text = (++_btnClickCount).ToString();
			btnPointerPressed.PointerPressed += (snd, evt) => btnPointerPressedCount.Text = (++_btnPointerPressedCount).ToString();
			btnPointerReleased.PointerReleased += (snd, evt) => btnPointerReleasedCount.Text = (++_btnPointerReleasedCount).ToString();
			btnPointerEntered.PointerEntered += (snd, evt) => btnPointerEnteredCount.Text = (++_btnPointerEnteredCount).ToString();
			btnPointerExited.PointerExited += (snd, evt) => btnPointerExitedCount.Text = (++_btnPointerExitedCount).ToString();
		}
	}
}
